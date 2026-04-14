using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CapShop.CatalogService.Application.Interfaces;
using CapShop.CatalogService.Configuration;
using CapShop.CatalogService.DTOs.Assistant;
using CapShop.CatalogService.DTOs.Catalog;
using CapShop.Shared.Exceptions;
using Microsoft.Extensions.Options;

namespace CapShop.CatalogService.Application.Services
{
    public class InventoryAssistantService : IInventoryAssistantService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        private readonly IProductAppService _productAppService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OllamaOptions _ollamaOptions;
        private readonly ILogger<InventoryAssistantService> _logger;

        public InventoryAssistantService(
            IProductAppService productAppService,
            IHttpClientFactory httpClientFactory,
            IOptions<OllamaOptions> ollamaOptions,
            ILogger<InventoryAssistantService> logger)
        {
            _productAppService = productAppService;
            _httpClientFactory = httpClientFactory;
            _ollamaOptions = ollamaOptions.Value;
            _logger = logger;
        }

        public async Task<AssistantQueryResponseDto> QueryAsync(AssistantQueryRequestDto request, CancellationToken ct = default)
        {
            var message = request.Message?.Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ValidationException("Message is required.");
            }

            var pageSize = Math.Clamp(request.PageSize <= 0 ? 20 : request.PageSize, 1, 50);
            var intent = await ExtractIntentAsync(message, ct) ?? BuildHeuristicIntent(message);

            if (string.IsNullOrWhiteSpace(intent.Query))
            {
                intent.Query = message;
            }

            var search = await SearchWithRelaxationAsync(intent, pageSize, request.ReturnAllMatches, ct);

            var mappedProducts = search.Products.Select(MapProduct).ToList();
            var response = new AssistantQueryResponseDto
            {
                AppliedFilters = new AssistantAppliedFiltersDto
                {
                    Query = search.AppliedIntent.Query,
                    MinPrice = search.AppliedIntent.MinPrice,
                    MaxPrice = search.AppliedIntent.MaxPrice,
                    StockOnly = search.AppliedIntent.StockOnly,
                    SortBy = search.AppliedIntent.SortBy
                },
                Products = mappedProducts,
                TotalMatches = search.TotalCount,
                UsedRelaxedSearch = search.UsedRelaxedSearch,
                RelaxationNote = search.RelaxationNote
            };

            var aiReply = await BuildCatalogAnswerAsync(message, response, ct);
            if (string.IsNullOrWhiteSpace(aiReply))
            {
                response.Reply = BuildFallbackReply(response);
                response.UsedFallbackResponse = true;
            }
            else
            {
                response.Reply = NormalizeAssistantReply(aiReply);
            }

            return response;
        }

        private async Task<SearchOutcome> SearchWithRelaxationAsync(AssistantIntent baseIntent, int pageSize, bool returnAllMatches, CancellationToken ct)
        {
            var strict = await ExecuteSearchAsync(baseIntent, pageSize, returnAllMatches, ct);
            if (strict.Products.Count > 0)
            {
                return new SearchOutcome(strict.Products, strict.TotalCount, baseIntent, false, null);
            }

            if (baseIntent.MaxPrice.HasValue)
            {
                var relaxedMaxIntent = CloneIntent(baseIntent);
                relaxedMaxIntent.MaxPrice = null;
                var relaxedMax = await ExecuteSearchAsync(relaxedMaxIntent, pageSize, returnAllMatches, ct);
                if (relaxedMax.Products.Count > 0)
                {
                    return new SearchOutcome(
                        relaxedMax.Products,
                        relaxedMax.TotalCount,
                        relaxedMaxIntent,
                        true,
                        "No exact match under your max budget. Showing closest matches without the upper price cap.");
                }
            }

            if (baseIntent.MinPrice.HasValue || baseIntent.MaxPrice.HasValue)
            {
                var relaxedBudgetIntent = CloneIntent(baseIntent);
                relaxedBudgetIntent.MinPrice = null;
                relaxedBudgetIntent.MaxPrice = null;
                var relaxedBudget = await ExecuteSearchAsync(relaxedBudgetIntent, pageSize, returnAllMatches, ct);
                if (relaxedBudget.Products.Count > 0)
                {
                    return new SearchOutcome(
                        relaxedBudget.Products,
                        relaxedBudget.TotalCount,
                        relaxedBudgetIntent,
                        true,
                        "No exact match in the selected budget range. Showing closest matches after relaxing budget filters.");
                }
            }

            if (baseIntent.StockOnly)
            {
                var relaxedStockIntent = CloneIntent(baseIntent);
                relaxedStockIntent.StockOnly = false;
                var relaxedStock = await ExecuteSearchAsync(relaxedStockIntent, pageSize, returnAllMatches, ct);
                if (relaxedStock.Products.Count > 0)
                {
                    return new SearchOutcome(
                        relaxedStock.Products,
                        relaxedStock.TotalCount,
                        relaxedStockIntent,
                        true,
                        "No in-stock exact match found. Showing closest results including low-stock or out-of-stock items.");
                }
            }

            foreach (var broaderQuery in BuildBroaderQueries(baseIntent.Query))
            {
                var broaderIntent = CloneIntent(baseIntent);
                broaderIntent.Query = broaderQuery;
                broaderIntent.MinPrice = null;
                broaderIntent.MaxPrice = null;
                broaderIntent.StockOnly = false;

                var broader = await ExecuteSearchAsync(broaderIntent, pageSize, returnAllMatches, ct);
                if (broader.Products.Count > 0)
                {
                    return new SearchOutcome(
                        broader.Products,
                        broader.TotalCount,
                        broaderIntent,
                        true,
                        "No exact match found. Showing closest matches using broader keywords.");
                }
            }

            return new SearchOutcome(strict.Products, strict.TotalCount, baseIntent, false, null);
        }

        private async Task<(List<ProductResponseDto> Products, int TotalCount)> ExecuteSearchAsync(AssistantIntent intent, int pageSize, bool returnAllMatches, CancellationToken ct)
        {
            if (returnAllMatches)
            {
                return await ExecuteSearchAcrossPagesAsync(intent, pageSize, ct);
            }

            var (products, totalCount) = await _productAppService.SearchProductsAsync(
                query: intent.Query,
                categoryId: null,
                minPrice: intent.MinPrice,
                maxPrice: intent.MaxPrice,
                sortBy: intent.SortBy,
                page: 1,
                pageSize: pageSize,
                ct: ct);

            if (!intent.StockOnly)
            {
                return (products, totalCount);
            }

            var inStock = products.Where(x => x.Stock > 0).ToList();
            return (inStock, inStock.Count);
        }

        private async Task<(List<ProductResponseDto> Products, int TotalCount)> ExecuteSearchAcrossPagesAsync(AssistantIntent intent, int pageSize, CancellationToken ct)
        {
            var allProducts = new List<ProductResponseDto>();
            var totalCount = 0;
            var page = 1;
            const int maxPages = 20;

            while (page <= maxPages)
            {
                var (pageProducts, pageTotalCount) = await _productAppService.SearchProductsAsync(
                    query: intent.Query,
                    categoryId: null,
                    minPrice: intent.MinPrice,
                    maxPrice: intent.MaxPrice,
                    sortBy: intent.SortBy,
                    page: page,
                    pageSize: pageSize,
                    ct: ct);

                if (page == 1)
                {
                    totalCount = pageTotalCount;
                }

                if (pageProducts.Count == 0)
                {
                    break;
                }

                allProducts.AddRange(pageProducts);

                if (allProducts.Count >= totalCount)
                {
                    break;
                }

                page++;
            }

            var deduped = allProducts
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .ToList();

            if (!intent.StockOnly)
            {
                return (deduped, totalCount);
            }

            var inStock = deduped.Where(x => x.Stock > 0).ToList();
            return (inStock, inStock.Count);
        }

        private static IEnumerable<string> BuildBroaderQueries(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                yield break;
            }

            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "under", "below", "above", "between", "in", "stock", "available", "for",
                "with", "and", "the", "a", "an", "to", "of", "show", "me", "find"
            };

            var tokens = query
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim(',', '.', ':', ';', '!', '?', '"', '\''))
                .Where(x => x.Length > 2 && !stopWords.Contains(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (tokens.Count > 1)
            {
                yield return string.Join(' ', tokens.Take(2));
                yield return string.Join(' ', tokens);
            }

            foreach (var token in tokens.Take(3))
            {
                yield return token;
            }
        }

        private static AssistantIntent CloneIntent(AssistantIntent source)
        {
            return new AssistantIntent
            {
                Query = source.Query,
                MinPrice = source.MinPrice,
                MaxPrice = source.MaxPrice,
                StockOnly = source.StockOnly,
                SortBy = source.SortBy
            };
        }

        private async Task<AssistantIntent?> ExtractIntentAsync(string userMessage, CancellationToken ct)
        {
            var prompt = $$"""
You are an assistant that extracts e-commerce product search filters.

Return only valid JSON with these keys:
{
  "query": string,
  "minPrice": number|null,
  "maxPrice": number|null,
  "stockOnly": boolean,
  "sortBy": "price_asc"|"price_desc"|"newest"|null
}

Rules:
- Use concise query terms from the user request.
- If no price is given, set minPrice/maxPrice to null.
- stockOnly=true only when user asks in-stock or available.
- Do not include markdown or explanations.

User message: {{userMessage}}
""";

            try
            {
                var raw = await GenerateWithOllamaAsync(prompt, 0.1m, ct);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return null;
                }

                var json = ExtractJsonObject(raw);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                var parsed = JsonSerializer.Deserialize<AssistantIntent>(json, JsonOptions);
                if (parsed is null)
                {
                    return null;
                }

                parsed.Query = parsed.Query?.Trim() ?? string.Empty;
                return parsed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to extract assistant intent using Ollama. Falling back to heuristic parser.");
                return null;
            }
        }

        private async Task<string?> BuildCatalogAnswerAsync(string userMessage, AssistantQueryResponseDto result, CancellationToken ct)
        {
            var productJson = JsonSerializer.Serialize(result.Products, JsonOptions);
            var filtersJson = JsonSerializer.Serialize(result.AppliedFilters, JsonOptions);
            var relaxationText = string.IsNullOrWhiteSpace(result.RelaxationNote)
                ? "No filter relaxation used."
                : result.RelaxationNote;

            var prompt = $$"""
You are CapShop's inventory assistant.

Use only the provided product data. Do not invent products, prices, stock, or categories.
If no products are found, state that clearly and suggest refining the search.
Keep the response concise and practical.
If filter relaxation was used, mention that briefly before listing matches.
Use plain text only. Do not use markdown like ** or bullet markdown.
Use currency format as Rs. <amount> and never use $.
Do not include image URLs in the text response because the UI already shows product cards.

User message: {{userMessage}}
Applied filters: {{filtersJson}}
Total matches: {{result.TotalMatches}}
Relaxation note: {{relaxationText}}
Products: {{productJson}}
""";

            try
            {
                return await GenerateWithOllamaAsync(prompt, 0.3m, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to generate assistant answer from Ollama. Falling back to deterministic response.");
                return null;
            }
        }

        private async Task<string> GenerateWithOllamaAsync(string prompt, decimal temperature, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("ollama");
            var payload = new OllamaGenerateRequest
            {
                Model = string.IsNullOrWhiteSpace(_ollamaOptions.Model) ? "gemma2:2b" : _ollamaOptions.Model,
                Prompt = prompt,
                Stream = false,
                Options = new OllamaOptionsPayload
                {
                    Temperature = temperature,
                    NumPredict = 320
                }
            };

            using var response = await client.PostAsJsonAsync("api/generate", payload, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(JsonOptions, ct);
            return result?.Response?.Trim() ?? string.Empty;
        }

        private static AssistantIntent BuildHeuristicIntent(string message)
        {
            var lower = message.ToLowerInvariant();
            var intent = new AssistantIntent
            {
                Query = message,
                StockOnly = lower.Contains("in stock") || lower.Contains("available")
            };

            var underMatch = System.Text.RegularExpressions.Regex.Match(lower, @"under\s+(\d+)");
            if (underMatch.Success && decimal.TryParse(underMatch.Groups[1].Value, out var maxPrice))
            {
                intent.MaxPrice = maxPrice;
            }

            var aboveMatch = System.Text.RegularExpressions.Regex.Match(lower, @"above\s+(\d+)");
            if (aboveMatch.Success && decimal.TryParse(aboveMatch.Groups[1].Value, out var minPrice))
            {
                intent.MinPrice = minPrice;
            }

            if (lower.Contains("cheapest") || lower.Contains("low price"))
            {
                intent.SortBy = "price_asc";
            }
            else if (lower.Contains("expensive") || lower.Contains("high price"))
            {
                intent.SortBy = "price_desc";
            }

            return intent;
        }

        private static string? ExtractJsonObject(string raw)
        {
            var start = raw.IndexOf('{');
            var end = raw.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                return null;
            }

            return raw[start..(end + 1)];
        }

        private static AssistantProductMatchDto MapProduct(ProductResponseDto product)
        {
            return new AssistantProductMatchDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                CategoryName = product.Category?.Name ?? "Unknown",
                ImageUrl = product.ImageUrl
            };
        }

        private static string BuildFallbackReply(AssistantQueryResponseDto response)
        {
            if (response.Products.Count == 0)
            {
                return "I could not find matching products in the catalog. Try adding a category, budget range, or in-stock requirement.";
            }

            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(response.RelaxationNote))
            {
                builder.AppendLine(response.RelaxationNote);
            }

            builder.AppendLine($"I found {response.TotalMatches} matching product(s). Top results:");
            foreach (var product in response.Products.Take(3))
            {
                builder.AppendLine($"- {product.Name} | Rs. {product.Price:F2} | Stock: {product.Stock} | Category: {product.CategoryName}");
            }

            return builder.ToString().Trim();
        }

        private static string NormalizeAssistantReply(string aiReply)
        {
            var text = aiReply.Trim();

            // Remove common markdown markers so chat text reads cleanly in plain UI.
            text = text.Replace("**", string.Empty)
                .Replace("__", string.Empty)
                .Replace("`", string.Empty);

            // Normalize markdown list markers to plain lines.
            text = Regex.Replace(text, @"(?m)^\s*[-*]\s+", string.Empty);

            // Normalize currency from dollar to rupee text used across this app.
            text = Regex.Replace(text, @"\$\s*(\d+(?:\.\d{1,2})?)", "Rs. $1");

            // Collapse excessive blank lines.
            text = Regex.Replace(text, @"(\r?\n){3,}", Environment.NewLine + Environment.NewLine);

            return text.Trim();
        }

        private sealed class AssistantIntent
        {
            public string Query { get; set; } = string.Empty;
            public decimal? MinPrice { get; set; }
            public decimal? MaxPrice { get; set; }
            public bool StockOnly { get; set; }
            public string? SortBy { get; set; }
        }

        private sealed class OllamaGenerateRequest
        {
            public string Model { get; set; } = string.Empty;
            public string Prompt { get; set; } = string.Empty;
            public bool Stream { get; set; }
            public OllamaOptionsPayload Options { get; set; } = new();
        }

        private sealed class OllamaOptionsPayload
        {
            [JsonPropertyName("temperature")]
            public decimal Temperature { get; set; }

            [JsonPropertyName("num_predict")]
            public int NumPredict { get; set; }
        }

        private sealed class OllamaGenerateResponse
        {
            [JsonPropertyName("response")]
            public string? Response { get; set; }
        }

        private sealed record SearchOutcome(
            List<ProductResponseDto> Products,
            int TotalCount,
            AssistantIntent AppliedIntent,
            bool UsedRelaxedSearch,
            string? RelaxationNote);
    }
}
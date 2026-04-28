using System.Collections.Concurrent;
using System.Text.Json;
using CapShop.AssistantService.Application.Interfaces;
using CapShop.AssistantService.DTOs.Catalog;
using Microsoft.AspNetCore.Hosting;

namespace CapShop.AssistantService.Application.Services
{
    public class AssistantKnowledgeService : IAssistantKnowledgeService
    {
        private readonly ConcurrentDictionary<int, ProductResponseDto> _items = new();
        private readonly string _storePath;

        public AssistantKnowledgeService(IWebHostEnvironment env)
        {
            var dir = Path.Combine(env.ContentRootPath, "uploads");
            Directory.CreateDirectory(dir);
            _storePath = Path.Combine(dir, "catalog.json");

            if (File.Exists(_storePath))
            {
                try
                {
                    var json = File.ReadAllText(_storePath);
                    var arr = JsonSerializer.Deserialize<List<ProductResponseDto>>(json) ?? new List<ProductResponseDto>();
                    foreach (var it in arr)
                    {
                        _items[it.Id] = it;
                    }
                }
                catch { /* ignore malformed file */ }
            }
        }

        public async Task<int> LoadFromStreamAsync(Stream stream, string fileName)
        {
            var list = new List<ProductResponseDto>();

            if (!string.IsNullOrWhiteSpace(fileName) && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var arr = await JsonSerializer.DeserializeAsync<List<ProductResponseDto>>(stream);
                    if (arr != null) list.AddRange(arr);
                }
                catch { /* ignore parse errors */ }
            }
            else
            {
                using var sr = new StreamReader(stream);
                while (!sr.EndOfStream)
                {
                    var line = await sr.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(new[] { ',', '|', '\t' }, StringSplitOptions.None);
                    if (parts.Length < 2) continue;
                    var id = 0;
                    if (!int.TryParse(parts[0], out id)) id = Math.Abs(parts[0].GetHashCode());
                    var name = parts.ElementAtOrDefault(1) ?? string.Empty;
                    var description = parts.ElementAtOrDefault(2) ?? string.Empty;
                    decimal price = 0;
                    decimal.TryParse(parts.ElementAtOrDefault(3) ?? "0", out price);
                    int stock = 0;
                    int.TryParse(parts.ElementAtOrDefault(4) ?? "0", out stock);
                    var image = parts.ElementAtOrDefault(5) ?? string.Empty;

                    list.Add(new ProductResponseDto
                    {
                        Id = id,
                        Name = name,
                        Description = description,
                        Price = price,
                        Stock = stock,
                        ImageUrl = image
                    });
                }
            }

            foreach (var it in list)
            {
                _items[it.Id] = it;
            }

            await PersistAsync();
            return list.Count;
        }

        public Task<CatalogSearchResponseDto> SearchAsync(string query, decimal? minPrice, decimal? maxPrice, bool stockOnly, int page = 1, int pageSize = 20)
        {
            var q = query?.Trim() ?? string.Empty;
            var items = _items.Values.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                items = items.Where(i => (i.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) || (i.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (minPrice.HasValue)
                items = items.Where(i => i.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                items = items.Where(i => i.Price <= maxPrice.Value);
            if (stockOnly)
                items = items.Where(i => i.Stock > 0);

            var total = items.Count();
            var pageItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var resp = new CatalogSearchResponseDto
            {
                Products = pageItems,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };

            return Task.FromResult(resp);
        }

        public Task PersistAsync()
        {
            var arr = _items.Values.ToList();
            var json = JsonSerializer.Serialize(arr, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_storePath, json);
            return Task.CompletedTask;
        }
    }
}

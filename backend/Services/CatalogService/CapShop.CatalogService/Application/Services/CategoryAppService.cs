using CapShop.CatalogService.Application.Interfaces;
using CapShop.CatalogService.DTOs.Catalog;
using CapShop.CatalogService.Models;
using CapShop.CatalogService.Infrastructure.Repositories;

namespace CapShop.CatalogService.Application.Services
{
    public class CategoryAppService : ICategoryAppService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryAppService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<List<CategoryResponseDto>> GetAllCategoriesAsync(CancellationToken ct = default)
        {
            var categories = await _categoryRepository.GetAllAsync(ct);
            return categories.Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            }).ToList();
        }

        public async Task<CategoryResponseDto?> GetCategoryByIdAsync(int id, CancellationToken ct = default)
        {
            var category = await _categoryRepository.GetByIdAsync(id, ct);
            if (category is null) return null;

            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };
        }

        public async Task<CategoryResponseDto> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken ct = default)
        {
            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                IsActive = true
            };

            var created = await _categoryRepository.AddAsync(category, ct);
            return new CategoryResponseDto
            {
                Id = created.Id,
                Name = created.Name,
                Description = created.Description
            };
        }

        public async Task<bool> UpdateCategoryAsync(int id, UpdateCategoryDto dto, CancellationToken ct = default)
        {
            var category = await _categoryRepository.GetByIdAsync(id, ct);
            if (category is null) return false;

            category.Name = dto.Name;
            category.Description = dto.Description;
            category.IsActive = dto.IsActive;

            await _categoryRepository.UpdateAsync(category, ct);
            return true;
        }

        public async Task<bool> DeleteCategoryAsync(int id, CancellationToken ct = default)
        {
            var category = await _categoryRepository.GetByIdAsync(id, ct);
            if (category is null) return false;

            await _categoryRepository.DeleteAsync(category, ct);
            return true;
        }
    }
}

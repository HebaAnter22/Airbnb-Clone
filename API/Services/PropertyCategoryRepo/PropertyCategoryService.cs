using API.Data;
using API.DTOs.PropertyCategoryDTOs;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.PropertyCategoryRepo
{
    public class PropertyCategoryService : IPropertyCategoryService
    {
        private readonly AppDbContext _context;

        public PropertyCategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PropertyCategory>> GetAllCategoriesAsync()
        {
            return await _context.PropertyCategories
                .Select(c => new PropertyCategory
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    Description = c.Description,
                    IconUrl = c.IconUrl
                })
                .ToListAsync();
        }

        public async Task<PropertyCategory> GetCategoryByIdAsync(int id)
        {
            var category = await _context.PropertyCategories.FindAsync(id);
            if (category == null)
            {
                throw new KeyNotFoundException($"Category with ID {id} not found.");
            }
            return category;
        }

        public async Task<PropertyCategory> AddCategoryAsync(PropertyCategory category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                throw new ArgumentException("Category name cannot be empty.");
            }

            _context.PropertyCategories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<PropertyCategory> UpdateCategoryAsync(PropertyCategory category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                throw new ArgumentException("Category name cannot be empty.");
            }

            var existingCategory = await _context.PropertyCategories.FindAsync(category.CategoryId);
            if (existingCategory == null)
            {
                throw new KeyNotFoundException($"Category with ID {category.CategoryId} not found.");
            }

            existingCategory.Name = category.Name;
            existingCategory.Description = category.Description;
            existingCategory.IconUrl = category.IconUrl;

            await _context.SaveChangesAsync();
            return existingCategory;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _context.PropertyCategories.FindAsync(id);
            if (category == null)
            {
                return false;
            }

            _context.PropertyCategories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

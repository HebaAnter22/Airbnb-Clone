using API.DTOs;
using API.Models;

namespace API.Services
{
    public interface IPropertyCategoryService
    {
        Task<IEnumerable<PropertyCategoryDto>> GetAllCategoriesAsync();
        Task<PropertyCategory> GetCategoryByIdAsync(int id);
        Task<PropertyCategory> AddCategoryAsync(PropertyCategory category);
        Task<PropertyCategory> UpdateCategoryAsync(PropertyCategory category);
        Task<bool> DeleteCategoryAsync(int id);
    }
} 
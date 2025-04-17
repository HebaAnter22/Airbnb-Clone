using API.DTOs.PropertyCategoryDTOs;
using API.Models;

namespace API.Services.PropertyCategoryRepo
{
    public interface IPropertyCategoryService
    {
        Task<IEnumerable<PropertyCategory>> GetAllCategoriesAsync();
        Task<PropertyCategory> GetCategoryByIdAsync(int id);
        Task<PropertyCategory> AddCategoryAsync(PropertyCategory category);
        Task<PropertyCategory> UpdateCategoryAsync(PropertyCategory category);
        Task<bool> DeleteCategoryAsync(int id);
    }
}

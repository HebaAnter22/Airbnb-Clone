//using API.Data;
//using API.Models;
//using Microsoft.EntityFrameworkCore;

//namespace API.Services
//{
//    public class PropertyCategoryService : IPropertyCategoryService
//    {
//        private readonly AppDbContext _context;

//        public PropertyCategoryService(AppDbContext context)
//        {
//            _context = context;
//        }

//        public async Task<IEnumerable<PropertyCategory>> GetAllCategoriesAsync()
//        {
//            return await _context.PropertyCategories.ToListAsync();
//        }

//        public async Task<PropertyCategory> GetCategoryByIdAsync(int id)
//        {
//            return await _context.PropertyCategories.FindAsync(id);
//        }

//        public async Task<PropertyCategory> AddCategoryAsync(PropertyCategory category)
//        {
//            _context.PropertyCategories.Add(category);
//            await _context.SaveChangesAsync();
//            return category;
//        }

//        public async Task<PropertyCategory> UpdateCategoryAsync(PropertyCategory category)
//        {
//            _context.Entry(category).State = EntityState.Modified;
//            await _context.SaveChangesAsync();
//            return category;
//        }

//        public async Task<bool> DeleteCategoryAsync(int id)
//        {
//            var category = await _context.PropertyCategories.FindAsync(id);
//            if (category == null)
//                return false;

//            _context.PropertyCategories.Remove(category);
//            await _context.SaveChangesAsync();
//            return true;
//        }
//    }
//} 
using API.DTOs.PropertyCategoryDTOs;
using API.Models;
using API.Services.PropertyCategoryRepo;

using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertyCategoryController : ControllerBase
    {
        private readonly IPropertyCategoryService _categoryService;

        public PropertyCategoryController(IPropertyCategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PropertyCategoryDto>>> GetCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PropertyCategoryDto>> GetCategory(int id)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                var categoryDto = new PropertyCategory
                {
                    CategoryId = category.CategoryId,
                    Name = category.Name,
                    Description = category.Description,
                    IconUrl = category.IconUrl
                };
                return Ok(categoryDto);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Category with ID {id} not found.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<PropertyCategoryDto>> CreateCategory(PropertyCategoryDto categoryDto)
        {
            try
            {
                var category = new PropertyCategory
                {
                    Name = categoryDto.Name,
                    Description = categoryDto.Description,
                    IconUrl = categoryDto.IconUrl
                };

                var createdCategory = await _categoryService.AddCategoryAsync(category);
                var responseDto = new PropertyCategory
                {
                    CategoryId = createdCategory.CategoryId,
                    Name = createdCategory.Name,
                    Description = createdCategory.Description,
                    IconUrl = createdCategory.IconUrl
                };

                return CreatedAtAction(nameof(GetCategory), new { id = createdCategory.CategoryId }, responseDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<PropertyCategoryDto>> UpdateCategory(int id, PropertyCategoryDto categoryDto)
        {
            try
            {
                var category = new PropertyCategory
                {
                    CategoryId = id,
                    Name = categoryDto.Name,
                    Description = categoryDto.Description,
                    IconUrl = categoryDto.IconUrl
                };

                var updatedCategory = await _categoryService.UpdateCategoryAsync(category);
                var responseDto = new PropertyCategory
                {
                    CategoryId = updatedCategory.CategoryId,
                    Name = updatedCategory.Name,
                    Description = updatedCategory.Description,
                    IconUrl = updatedCategory.IconUrl
                };

                return Ok(responseDto);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Category with ID {id} not found.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCategory(int id)
        {
            var result = await _categoryService.DeleteCategoryAsync(id);
            if (!result)
            {
                return NotFound($"Category with ID {id} not found.");
            }

            return NoContent();
        }
    }
} 
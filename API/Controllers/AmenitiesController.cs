using API.Models;
using API.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmenitiesController : ControllerBase
    {
        private readonly IAmenityService _amenityService;

        public AmenitiesController(IAmenityService amenityService)
        {
            _amenityService = amenityService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Amenity>>> GetAmenities()
        {
            var amenities = await _amenityService.GetAllAmenitiesAsync();
            return Ok(amenities);
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<Amenity>>> GetAmenitiesByCategory(string category)
        {
            var amenities = await _amenityService.GetAmenitiesByCategoryAsync(category);
            return Ok(amenities);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Amenity>> GetAmenity(int id)
        {
            var amenity = await _amenityService.GetAmenityByIdAsync(id);
            if (amenity == null)
            {
                return NotFound();
            }
            return Ok(amenity);
        }

        [HttpPost]
        public async Task<ActionResult<Amenity>> CreateAmenity(Amenity amenity)
        {
            var createdAmenity = await _amenityService.AddAmenityAsync(amenity);
            return CreatedAtAction(nameof(GetAmenity), new { id = createdAmenity.Id }, createdAmenity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAmenity(int id, Amenity amenity)
        {
            if (id != amenity.Id)
            {
                return BadRequest();
            }

            var updatedAmenity = await _amenityService.UpdateAmenityAsync(amenity);
            return Ok(updatedAmenity);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAmenity(int id)
        {
            var result = await _amenityService.DeleteAmenityAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
} 
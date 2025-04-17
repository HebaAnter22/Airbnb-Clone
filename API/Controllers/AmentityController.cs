using API.DTOs.Amenity;
using API.Models;
using API.Services.AmenityRepo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AmenityController : ControllerBase
    {
        private readonly IAmenityService _amenityService;

        public AmenityController(IAmenityService amenityService)
        {
            _amenityService = amenityService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AmenityDto>>> GetAmenities()
        {
            var amenities = await _amenityService.GetAllAmenitiesAsync();
            return Ok(amenities);
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<AmenityDto>>> GetAmenitiesByCategory(string category)
        {
            var amenities = await _amenityService.GetAmenitiesByCategoryAsync(category);
            var amenityDtos = amenities.Select(a => new AmenityDto
            {
                Id = a.Id,
                Name = a.Name,
                Category = a.Category,
                IconUrl = a.IconUrl
            });
            return Ok(amenityDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AmenityDto>> GetAmenity(int id)
        {
            try
            {
                var amenity = await _amenityService.GetAmenityByIdAsync(id);
                var amenityDto = new AmenityDto
                {
                    Id = amenity.Id,
                    Name = amenity.Name,
                    Category = amenity.Category,
                    IconUrl = amenity.IconUrl
                };
                return Ok(amenityDto);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Amenity with ID {id} not found.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<AmenityDto>> CreateAmenity(AmenityDto amenityDto)
        {
            try
            {
                var amenity = new Amenity
                {
                    Name = amenityDto.Name,
                    Category = amenityDto.Category,
                    IconUrl = amenityDto.IconUrl
                };

                var createdAmenity = await _amenityService.AddAmenityAsync(amenity);
                var responseDto = new AmenityDto
                {
                    Id = createdAmenity.Id,
                    Name = createdAmenity.Name,
                    Category = createdAmenity.Category,
                    IconUrl = createdAmenity.IconUrl
                };

                return CreatedAtAction(nameof(GetAmenity), new { id = createdAmenity.Id }, responseDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<AmenityDto>> UpdateAmenity(int id, AmenityDto amenityDto)
        {
            try
            {
                var amenity = new Amenity
                {
                    Id = id,
                    Name = amenityDto.Name,
                    Category = amenityDto.Category,
                    IconUrl = amenityDto.IconUrl
                };

                var updatedAmenity = await _amenityService.UpdateAmenityAsync(amenity);
                var responseDto = new AmenityDto
                {
                    Id = updatedAmenity.Id,
                    Name = updatedAmenity.Name,
                    Category = updatedAmenity.Category,
                    IconUrl = updatedAmenity.IconUrl
                };

                return Ok(responseDto);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Amenity with ID {id} not found.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAmenity(int id)
        {
            var result = await _amenityService.DeleteAmenityAsync(id);
            if (!result)
            {
                return NotFound($"Amenity with ID {id} not found.");
            }

            return NoContent();
        }
    }
}

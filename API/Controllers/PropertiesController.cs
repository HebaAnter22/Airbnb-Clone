using API.DTOs;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropertiesController : ControllerBase
    {
        private readonly IPropertyService _propertyService;

        public PropertiesController(IPropertyService propertyService)
        {
            _propertyService = propertyService;
        }

        private int GetHostId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int hostId))
            {
                throw new UnauthorizedAccessException("Invalid or missing user ID in token.");
            }
            return hostId;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddProperty([FromBody] PropertyCreateDto propertyDto)
        {
            var hostId = GetHostId();
            try
            {

            var createdProperty = await _propertyService.AddPropertyAsync(propertyDto, hostId);
            return CreatedAtAction(nameof(GetProperty), new { id = createdProperty.Id }, createdProperty);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating property: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> EditProperty(int id, [FromBody] PropertyUpdateDto propertyDto)
        {
            var hostId = GetHostId();
            var updatedProperty = await _propertyService.EditPropertyAsync(id, propertyDto, hostId);
            if (updatedProperty == null) return NotFound("Property not found or you don't have permission.");
            return Ok(updatedProperty);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProperty(int id)
        {
            var hostId = GetHostId();
            var success = await _propertyService.DeletePropertyAsync(id, hostId);
            if (!success) return NotFound("Property not found or you don't have permission.");
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProperty(int id)
        {
            var property = await _propertyService.GetPropertyByIdAsync(id);
            if (property == null) return NotFound();
            return Ok(property);
        }

        [HttpGet("my-properties")]
        [Authorize]
        public async Task<IActionResult> GetMyProperties()
        {
            var hostId = int.Parse(User.FindFirst("id")?.Value);
            var properties = await _propertyService.GetHostPropertiesAsync(hostId);
            return Ok(properties);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProperties()
        {
            var properties = await _propertyService.GetAllPropertiesAsync();
            return Ok(properties);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchProperties(
            [FromQuery] string city = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] int? maxGuests = null)
        {
            var properties = await _propertyService.SearchPropertiesAsync(city, minPrice, maxPrice, maxGuests);
            return Ok(properties);
        }
    }
}
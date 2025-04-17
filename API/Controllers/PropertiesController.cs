using API.DTOs;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Text.Json;
using System.Text;
using System.IO;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropertiesController : ControllerBase
    {
        private readonly IPropertyService _propertyService;
        private readonly IWebHostEnvironment _environment;

        public PropertiesController(IPropertyService propertyService, IWebHostEnvironment environment)
        {
            _propertyService = propertyService;
            _environment = environment;
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
                // Log the incoming property data
                Console.WriteLine($"Received property creation request from host: {hostId}");
                Console.WriteLine($"Property data summary: Title='{propertyDto?.Title}', CategoryId={propertyDto?.CategoryId}, Type='{propertyDto?.PropertyType}'");
                Console.WriteLine($"Images: {propertyDto?.Images?.Count ?? 0}");
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    Console.WriteLine($"Validation errors: {string.Join(", ", errors)}");
                    return BadRequest($"Validation errors: {string.Join(", ", errors)}");
                }

                // Basic data validation
                if (propertyDto == null)
                {
                    return BadRequest("Property data is required.");
                }
                
                if (string.IsNullOrWhiteSpace(propertyDto.Title))
                {
                    return BadRequest("Property title is required.");
                }
                
                if (propertyDto.CategoryId <= 0)
                {
                    return BadRequest("Valid category ID is required.");
                }
                
                if (string.IsNullOrWhiteSpace(propertyDto.PropertyType))
                {
                    return BadRequest("Property type is required.");
                }

                Console.WriteLine($"Attempting to create property for host: {hostId}");
                Console.WriteLine($"Property data: Title={propertyDto.Title}, Type={propertyDto.PropertyType}, CategoryId={propertyDto.CategoryId}");
                
                var createdProperty = await _propertyService.AddPropertyAsync(propertyDto, hostId);
                return CreatedAtAction(nameof(GetProperty), new { id = createdProperty.Id }, createdProperty);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Validation error: {ex.Message}");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating property: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }
                
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
            var hostId = GetHostId();
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

        [HttpPost("images/upload")]
        [Authorize]
        public async Task<IActionResult> UploadImages([FromForm] List<IFormFile> files)
        {
            try
            {
                if (files == null || !files.Any())
                {
                    return BadRequest("No files were uploaded.");
                }

                Console.WriteLine($"Received {files.Count} files for upload");
                foreach (var file in files)
                {
                    Console.WriteLine($"File: {file.FileName}, Size: {file.Length} bytes, Type: {file.ContentType}");
                }

                var imageUrls = await _propertyService.UploadImagesAsync(files);
                Console.WriteLine($"Successfully uploaded files. Generated URLs: {string.Join(", ", imageUrls)}");

                return Ok(imageUrls);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading images: {ex.Message}");
                return BadRequest($"Error uploading images: {ex.Message}");
            }
        }

        [HttpPost("{id}/images")]
        [Authorize]
        public async Task<IActionResult> AddImagesToProperty(int id, [FromBody] object imageUrlsData)
        {
            try
            {
                var hostId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                
                List<string> imageUrls;
                
                // Handle input that could be either a direct array or an object with an imageUrls property
                if (imageUrlsData is List<string> urlList)
                {
                    imageUrls = urlList;
                }
                else
                {
                    // Try to deserialize as object with imageUrls property
                    try
                    {
                        var jsonElement = JsonSerializer.Deserialize<JsonElement>(
                            JsonSerializer.Serialize(imageUrlsData));
                            
                        if (jsonElement.TryGetProperty("imageUrls", out var urlsElement) && 
                            urlsElement.ValueKind == JsonValueKind.Array)
                        {
                            imageUrls = JsonSerializer.Deserialize<List<string>>(
                                urlsElement.GetRawText());
                        }
                        else
                        {
                            // Try direct deserialization
                            imageUrls = JsonSerializer.Deserialize<List<string>>(
                                JsonSerializer.Serialize(imageUrlsData));
                        }
                    }
                    catch
                    {
                        return BadRequest("Expected either an array of URLs or an object with an 'imageUrls' property");
                    }
                }
                
                if (imageUrls == null || !imageUrls.Any())
                {
                    return BadRequest("No image URLs provided");
                }
                
                Console.WriteLine($"Adding {imageUrls.Count} images to property {id} for host {hostId}");

                await _propertyService.AddImagesToPropertyAsync(id, imageUrls, hostId);
                Console.WriteLine("Successfully added images to property");

                return Ok(new { message = "Images added to property successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding images to property: {ex.Message}");
                return BadRequest($"Error adding images to property: {ex.Message}");
            }
        }

        [HttpPost("{id}/upload-images")]
        [Authorize]
        public async Task<IActionResult> UploadImagesForProperty(int id, [FromForm] List<IFormFile> files)
        {
            try
            {
                var hostId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                
                if (files == null || !files.Any())
                {
                    return BadRequest("No files were uploaded.");
                }

                Console.WriteLine($"Received {files.Count} files for upload to property {id}");

                // First upload the files to get URLs using the existing method
                var imageUrls = await _propertyService.UploadImagesAsync(files);
                Console.WriteLine($"Successfully uploaded files. Generated URLs: {string.Join(", ", imageUrls)}");

                // Then add these URLs to the property using the existing method
                var success = await _propertyService.AddImagesToPropertyAsync(id, imageUrls, hostId);
                if (!success)
                {
                    return BadRequest("Failed to add images to property. Property not found or you don't have permission.");
                }
                
                Console.WriteLine("Successfully added images to property");

                return Ok(new { message = "Images uploaded and added to property successfully", imageUrls });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading images for property: {ex.Message}");
                return BadRequest($"Error uploading images for property: {ex.Message}");
            }
        }

        [HttpDelete("{propertyId}/images/{imageId}")]
        [Authorize]
        public async Task<IActionResult> DeletePropertyImage(int propertyId, int imageId)
        {
            try
            {
                var hostId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                
                Console.WriteLine($"Attempting to delete image {imageId} from property {propertyId} for host {hostId}");
                
                var success = await _propertyService.DeletePropertyImageAsync(propertyId, imageId, hostId);
                
                if (!success)
                {
                    return NotFound("Property image not found or you don't have permission to delete it.");
                }
                
                Console.WriteLine($"Successfully deleted image {imageId} from property {propertyId}");
                
                return Ok(new { message = "Property image deleted successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting property image: {ex.Message}");
                return BadRequest($"Error deleting property image: {ex.Message}");
            }
        }
    }
}
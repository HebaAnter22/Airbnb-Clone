using API.DTOs;
using API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Services
{
    public interface IPropertyService
    {
        Task<PropertyDto> AddPropertyAsync(PropertyCreateDto propertyDto, int hostId);
        Task<PropertyDto> EditPropertyAsync(int propertyId, PropertyUpdateDto updatedPropertyDto, int hostId);
        Task<bool> DeletePropertyAsync(int propertyId, int hostId);
        Task<PropertyDto> GetPropertyByIdAsync(int propertyId);
        Task<List<PropertyDto>> GetHostPropertiesAsync(int hostId);
        Task<List<PropertyDto>> GetAllPropertiesAsync();
        Task<List<PropertyDto>> SearchPropertiesAsync(string city = null, decimal? minPrice = null, decimal? maxPrice = null, int? maxGuests = null);
        Task<List<string>> UploadImagesAsync(List<IFormFile> files);
        Task<bool> AddImagesToPropertyAsync(int propertyId, List<string> imageUrls, int hostId);
        Task<bool> UpdatePropertyAmenitiesAsync(int propertyId, List<int> amenityIds, int hostId);
        Task<bool> DeletePropertyImageAsync(int propertyId, int imageId, int hostId);
    }
}
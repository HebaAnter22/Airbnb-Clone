using API.DTOs;
using API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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
        Task<List<string>> UploadImagesAsync(List<IFormFile> files);
        Task<bool> AddImagesToPropertyAsync(int propertyId, List<string> imageUrls, int hostId);
        Task<bool> UpdatePropertyAmenitiesAsync(int propertyId, List<int> amenityIds, int hostId);
        Task<bool> DeletePropertyImageAsync(int propertyId, int imageId, int hostId);
        Task<List<PropertyDto>> SearchPropertiesAsync(string title = null, string country = null, int? minNights = null, int? maxNights = null, DateTime? startDate = null, DateTime? endDate = null, int? maxGuests = null);
    }
}
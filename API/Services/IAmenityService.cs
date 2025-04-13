using API.Models;

namespace API.Services
{
    public interface IAmenityService
    {
        Task<IEnumerable<Amenity>> GetAllAmenitiesAsync();
        Task<IEnumerable<Amenity>> GetAmenitiesByCategoryAsync(string category);
        Task<Amenity> GetAmenityByIdAsync(int id);
        Task<Amenity> AddAmenityAsync(Amenity amenity);
        Task<Amenity> UpdateAmenityAsync(Amenity amenity);
        Task<bool> DeleteAmenityAsync(int id);
    }
} 
using API.Data;
using API.DTOs.Amenity;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.AmenityRepo
{
    public class AmenityService : IAmenityService
    {
        private readonly AppDbContext _context;

        public AmenityService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AmenityDto>> GetAllAmenitiesAsync()
        {
            return await _context.Amenities
                .Select(a => new AmenityDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Category = a.Category,
                    IconUrl = a.IconUrl
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<Amenity>> GetAmenitiesByCategoryAsync(string category)
        {
            return await _context.Amenities
                .Where(a => a.Category == category)
                .ToListAsync();
        }

        public async Task<Amenity> GetAmenityByIdAsync(int id)
        {
            var amenity = await _context.Amenities.FindAsync(id);
            if (amenity == null)
            {
                throw new KeyNotFoundException($"Amenity with ID {id} not found.");
            }
            return amenity;
        }

        public async Task<Amenity> AddAmenityAsync(Amenity amenity)
        {
            if (string.IsNullOrWhiteSpace(amenity.Name))
            {
                throw new ArgumentException("Amenity name cannot be empty.");
            }

            _context.Amenities.Add(amenity);
            await _context.SaveChangesAsync();
            return amenity;
        }

        public async Task<Amenity> UpdateAmenityAsync(Amenity amenity)
        {
            if (string.IsNullOrWhiteSpace(amenity.Name))
            {
                throw new ArgumentException("Amenity name cannot be empty.");
            }

            var existingAmenity = await _context.Amenities.FindAsync(amenity.Id);
            if (existingAmenity == null)
            {
                throw new KeyNotFoundException($"Amenity with ID {amenity.Id} not found.");
            }

            existingAmenity.Name = amenity.Name;
            existingAmenity.Category = amenity.Category;
            existingAmenity.IconUrl = amenity.IconUrl;

            await _context.SaveChangesAsync();
            return existingAmenity;
        }

        public async Task<bool> DeleteAmenityAsync(int id)
        {
            var amenity = await _context.Amenities.FindAsync(id);
            if (amenity == null)
            {
                return false;
            }

            _context.Amenities.Remove(amenity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

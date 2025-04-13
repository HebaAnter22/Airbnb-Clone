using API.Data;
using API.DTOs;
using API.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Services
{
    public class PropertyService : IPropertyService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public PropertyService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PropertyDto> AddPropertyAsync(PropertyCreateDto propertyDto, int hostId)
        {

            var hostExists =
                await _context.Users.AnyAsync(u => u.Id == hostId && u.Role == "Host");
            if (!hostExists)
            {
                throw new ArgumentException($"Host with ID {hostId} does not exist.");
            }

            var property = _mapper.Map<Property>(propertyDto);
            property.HostId = hostId;
            property.CreatedAt = DateTime.UtcNow;
            property.UpdatedAt = DateTime.UtcNow;
            property.Status = "Active";

            _context.Properties.Add(property);
            try
            {

            await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Handle the exception as needed
                Console.WriteLine($"Error saving property: {ex.Message}");
                throw new Exception("Error saving property to the database.", ex);
            }

            return _mapper.Map<PropertyDto>(property);
           
        }

        public async Task<PropertyDto> EditPropertyAsync(int propertyId, PropertyUpdateDto updatedPropertyDto, int hostId)
        {
            var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == propertyId && p.HostId == hostId);
            if (property == null) return null;

            _mapper.Map(updatedPropertyDto, property);
            property.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return _mapper.Map<PropertyDto>(property);
        }

        public async Task<bool> DeletePropertyAsync(int propertyId, int hostId)
        {
            var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == propertyId && p.HostId == hostId);
            if (property == null) return false;

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PropertyDto> GetPropertyByIdAsync(int propertyId)
        {
            var property = await _context.Properties
                .Include(p => p.PropertyImages)
                //.Include(p => p.Amenities)
                .FirstOrDefaultAsync(p => p.Id == propertyId);

            return property == null ? null : _mapper.Map<PropertyDto>(property);
        }

        public async Task<List<PropertyDto>> GetHostPropertiesAsync(int hostId)
        {
            var properties = await _context.Properties
                .Where(p => p.HostId == hostId)
                .Include(p => p.PropertyImages)
                .ToListAsync();

            return _mapper.Map<List<PropertyDto>>(properties);
        }

        public async Task<List<PropertyDto>> GetAllPropertiesAsync()
        {
            var properties = await _context.Properties
                .Where(p => p.Status == "Active")
                .Include(p => p.PropertyImages)
                .Include(p => p.Amenities)
                .Include(p => p.Host)
                    .ThenInclude(h => h.User)
                .Include(p => p.Bookings)
                    .ThenInclude(b => b.Review)
                .ToListAsync();

            return _mapper.Map<List<PropertyDto>>(properties);
        }

        public async Task<List<PropertyDto>> SearchPropertiesAsync(string city = null, decimal? minPrice = null, decimal? maxPrice = null, int? maxGuests = null)
        {
            var query = _context.Properties
                .Where(p => p.Status == "Active")
                .AsQueryable();

            if (!string.IsNullOrEmpty(city))
                query = query.Where(p => p.City.Contains(city));
            if (minPrice.HasValue)
                query = query.Where(p => p.PricePerNight >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => p.PricePerNight <= maxPrice.Value);
            if (maxGuests.HasValue)
                query = query.Where(p => p.MaxGuests >= maxGuests.Value);

            var properties = await query
                .Include(p => p.PropertyImages)
                .ToListAsync();

            return _mapper.Map<List<PropertyDto>>(properties);
        }
    }
}
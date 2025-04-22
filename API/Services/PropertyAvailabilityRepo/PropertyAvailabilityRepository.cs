using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using WebApiDotNet.Repos;

namespace API.Services.PropertyAvailabilityRepo
{
    public class PropertyAvailabilityRepository : GenericRepository<PropertyAvailability>, IPropertyAvailabilityRepository
    {
        private readonly AppDbContext _context;

        public PropertyAvailabilityRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> IsPropertyAvailableAsync(int propertyId, DateTime startDate, DateTime endDate)
        {
            // Check if all dates in the range are available
            var unavailableDates = await _context.PropertyAvailabilities
                .Where(pa => pa.PropertyId == propertyId &&
                             pa.Date >= startDate && pa.Date <= endDate &&
                             !pa.IsAvailable)
                .AnyAsync();

            return !unavailableDates;
        }

        public async Task<List<PropertyAvailability>> GetPropertyAvailabilityAsync(int propertyId)
        {
            var availability = await _context.PropertyAvailabilities
                .Where(pa => pa.PropertyId == propertyId)
                .ToListAsync();
            if (availability == null || !availability.Any())
            {
                throw new Exception("No availability found for this property.");
            }
            // Return the availability data
            return availability;
        }
        public async Task UpdateAvailabilityAsync(int propertyId, DateTime startDate, DateTime endDate, bool isAvailable)
        {
            var datesToUpdate = await _context.PropertyAvailabilities
                .Where(pa => pa.PropertyId == propertyId &&
                             pa.Date >= startDate && pa.Date <= endDate)
                .ToListAsync();

            foreach (var date in datesToUpdate)
            {
                date.IsAvailable = isAvailable;
            }

            await _context.SaveChangesAsync();
        }
    }
}

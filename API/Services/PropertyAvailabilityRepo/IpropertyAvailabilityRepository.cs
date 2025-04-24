using API.Models;
using WebApiDotNet.Repos;

namespace API.Services.PropertyAvailabilityRepo
{
    public interface IPropertyAvailabilityRepository : IGenericRepository<PropertyAvailability>
    {
        Task<bool> IsPropertyAvailableAsync(int propertyId, DateTime startDate, DateTime endDate);
        Task UpdateAvailabilityAsync(int propertyId, DateTime startDate, DateTime endDate, bool isAvailable);
    }
}

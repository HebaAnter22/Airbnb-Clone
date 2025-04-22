using API.Services.PropertyAvailabilityRepo;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AvailabilityController:ControllerBase
    {
        private readonly IPropertyAvailabilityRepository _propertyAvailability;
        public AvailabilityController( IPropertyAvailabilityRepository propertyAvailability)
        {
            _propertyAvailability = propertyAvailability;

        }
        [HttpGet("{propertyId}")]
        public async Task<IActionResult> GetPropertyAvailability([FromRoute] int propertyId)
        {

            var availability = await _propertyAvailability.GetPropertyAvailabilityAsync(propertyId);
            if (availability == null)
            {
                return NotFound();
            }
            return Ok(availability);

        }
    }
}

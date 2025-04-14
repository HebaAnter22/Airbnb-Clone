using API.Data;
using API.DTOs;
using API.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace API.Services
{
    public class PropertyService : IPropertyService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _environment;

        public PropertyService(AppDbContext context, IMapper mapper, IWebHostEnvironment environment)
        {
            _context = context;
            _mapper = mapper;
            _environment = environment;
        }

        public async Task<PropertyDto> AddPropertyAsync(PropertyCreateDto propertyDto, int hostId)
        {
            // Log input data
            Console.WriteLine($"Starting AddPropertyAsync. Host ID: {hostId}");
            Console.WriteLine($"Property details: Title='{propertyDto.Title}', CategoryId={propertyDto.CategoryId}, PropertyType='{propertyDto.PropertyType}'");
            Console.WriteLine($"Location: {propertyDto.Address}, {propertyDto.City}, {propertyDto.Country}");
            Console.WriteLine($"Images count: {propertyDto.Images?.Count ?? 0}");
            
            if (propertyDto.Images != null && propertyDto.Images.Any())
            {
                Console.WriteLine("Image URLs to process:");
                foreach (var img in propertyDto.Images)
                {
                    Console.WriteLine($" - {img.ImageUrl} (IsPrimary: {img.IsPrimary})");
                }
            }

            var hostExists = await _context.Users.AnyAsync(u => u.Id == hostId && u.Role == "Host");
            if (!hostExists)
            {
                throw new ArgumentException($"Host with ID {hostId} does not exist.");
            }

            // Check if category exists
            Console.WriteLine($"Looking for category with ID: {propertyDto.CategoryId}");
            var categoryExists = await _context.PropertyCategories.AnyAsync(c => c.CategoryId == propertyDto.CategoryId);
            if (!categoryExists)
            {
                Console.WriteLine($"Category with ID {propertyDto.CategoryId} does not exist in database");
                
                // List available categories for debugging
                var categories = await _context.PropertyCategories.Select(c => new { c.CategoryId, c.Name }).ToListAsync();
                Console.WriteLine($"Available categories: {string.Join(", ", categories.Select(c => $"{c.CategoryId}: {c.Name}"))}");
                
                throw new ArgumentException($"Category with ID {propertyDto.CategoryId} does not exist.");
            }
            
            Console.WriteLine($"Category with ID {propertyDto.CategoryId} found. Proceeding with property creation.");

            // Check if cancellation policy exists
            if (propertyDto.CancellationPolicyId.HasValue && propertyDto.CancellationPolicyId.Value > 0)
            {
                Console.WriteLine($"Looking for cancellation policy with ID: {propertyDto.CancellationPolicyId}");
                var policyExists = await _context.CancellationPolicies.AnyAsync(c => c.Id == propertyDto.CancellationPolicyId);
                if (!policyExists)
                {
                    Console.WriteLine($"Cancellation policy with ID {propertyDto.CancellationPolicyId} does not exist");
                    // Get default policy ID
                    var defaultPolicy = await _context.CancellationPolicies.FirstOrDefaultAsync();
                    if (defaultPolicy != null)
                    {
                        Console.WriteLine($"Using default cancellation policy with ID: {defaultPolicy.Id}");
                        propertyDto.CancellationPolicyId = defaultPolicy.Id;
                    }
                    else
                    {
                        Console.WriteLine("No cancellation policies found in the database");
                        throw new ArgumentException("No cancellation policies available. Please create at least one cancellation policy.");
                    }
                }
            }
            else
            {
                // Get default policy ID if none provided
                var defaultPolicy = await _context.CancellationPolicies.FirstOrDefaultAsync();
                if (defaultPolicy != null)
                {
                    Console.WriteLine($"Using default cancellation policy with ID: {defaultPolicy.Id}");
                    propertyDto.CancellationPolicyId = defaultPolicy.Id;
                }
                else
                {
                    Console.WriteLine("No cancellation policies found in the database");
                    throw new ArgumentException("No cancellation policies available. Please create at least one cancellation policy.");
                }
            }

            // Use a transaction to ensure both property and images are saved consistently
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var property = _mapper.Map<Property>(propertyDto);
                property.HostId = hostId;
                property.CreatedAt = DateTime.UtcNow;
                property.UpdatedAt = DateTime.UtcNow;
                property.Status = "Active";

                // Add property to context but don't save yet
                _context.Properties.Add(property);
                try {
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Successfully saved property with ID: {property.Id}");
                } catch (Exception ex) {
                    Console.WriteLine($"Error saving property to database: {ex.Message}");
                    if (ex.InnerException != null) {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    throw new Exception("Error saving property to the database.", ex);
                }

                // After property is saved, get the ID and process images
                if (propertyDto.Images != null && propertyDto.Images.Any())
                {
                    Console.WriteLine($"Processing {propertyDto.Images.Count} images for property {property.Id}");
                    
                    // Create property-specific directory
                    var propertyUploadPath = Path.Combine(_environment.WebRootPath, "uploads", "properties", property.Id.ToString());
                    Directory.CreateDirectory(propertyUploadPath);

                    foreach (var imageDto in propertyDto.Images)
                    {
                        try
                        {
                            // Handle both full URLs and relative paths
                            string imagePath;
                            Console.WriteLine($"Processing image URL: {imageDto.ImageUrl}");
                            
                            if (imageDto.ImageUrl.StartsWith("http"))
                            {
                                // Extract the path portion from full URL
                                Uri uri = new Uri(imageDto.ImageUrl);
                                imagePath = uri.AbsolutePath;
                                Console.WriteLine($"Extracted path from URL: {imagePath}");
                            }
                            else
                            {
                                imagePath = imageDto.ImageUrl;
                                Console.WriteLine($"Using provided path: {imagePath}");
                            }
                            
                            // Get the filename from the path
                            var fileName = Path.GetFileName(imagePath);
                            Console.WriteLine($"Extracted filename: {fileName}");
                            
                            // Look in multiple possible locations for the source file
                            string sourcePath = null;
                            var possiblePaths = new[]
                            {
                                Path.Combine(_environment.WebRootPath, "uploads", "properties", fileName),
                                Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/')),
                                Path.Combine(_environment.WebRootPath, "uploads", "temp", fileName)
                            };
                            
                            foreach (var path in possiblePaths)
                            {
                                Console.WriteLine($"Checking if file exists at: {path}");
                                if (File.Exists(path))
                                {
                                    sourcePath = path;
                                    Console.WriteLine($"Found file at: {sourcePath}");
                                    break;
                                }
                            }

                            if (sourcePath != null)
                            {
                                var newPath = Path.Combine(propertyUploadPath, fileName);
                                Console.WriteLine($"Will move file from {sourcePath} to {newPath}");
                                
                                try
                                {
                                    // Move the file from the source folder to the property-specific folder
                                    File.Copy(sourcePath, newPath, true);
                                    Console.WriteLine($"Successfully copied file to {newPath}");
                                    
                                    // Try to delete the source file, but don't fail if we can't
                                    try
                                    {
                                        File.Delete(sourcePath);
                                        Console.WriteLine($"Deleted source file at {sourcePath}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Could not delete source file: {ex.Message}");
                                    }
                                    
                                    // Create relative URL for the new location
                                    var relativePath = $"/uploads/properties/{property.Id}/{fileName}";
                                    
                                    // Create full URL with base address
                                    var baseUrl = "https://localhost:7228"; // This should come from configuration in a real app
                                    var fullImageUrl = $"{baseUrl}{relativePath}";
                                    
                                    // Create PropertyImage entity and add to database
                                    var propertyImage = new PropertyImage
                                    {
                                        PropertyId = property.Id,
                                        ImageUrl = fullImageUrl,
                                        IsPrimary = imageDto.IsPrimary,
                                        CreatedAt = DateTime.UtcNow,
                                        Category = "Additional"  // Must be one of: 'Bedroom', 'Bathroom', 'Living Area', 'Kitchen', 'Exterior', 'Additional'
                                    };
                                    
                                    try {
                                        _context.PropertyImages.Add(propertyImage);
                                        Console.WriteLine($"Added image entity for: {fullImageUrl}");
                                    }
                                    catch (Exception ex) {
                                        Console.WriteLine($"Error adding image entity to context: {ex.Message}");
                                        // Continue even if we can't add this image
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error while moving/copying file: {ex.Message}");
                                    throw;
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Warning: Source image not found for {imagePath}");
                                
                                // Just create a record with the URL as is
                                // Check if it's already a full URL
                                string fullImageUrl;
                                if (imagePath.StartsWith("http"))
                                {
                                    fullImageUrl = imagePath;
                                }
                                else
                                {
                                    // Create full URL with base address
                                    var baseUrl = "https://localhost:7228"; // This should come from configuration in a real app
                                    fullImageUrl = $"{baseUrl}{imagePath}";
                                }
                                
                                var propertyImage = new PropertyImage
                                {
                                    PropertyId = property.Id,
                                    ImageUrl = fullImageUrl,
                                    IsPrimary = imageDto.IsPrimary,
                                    CreatedAt = DateTime.UtcNow,
                                    Category = "Additional"  // Must be one of: 'Bedroom', 'Bathroom', 'Living Area', 'Kitchen', 'Exterior', 'Additional'
                                };
                                
                                try {
                                    _context.PropertyImages.Add(propertyImage);
                                    Console.WriteLine($"Added image entity with provided URL: {fullImageUrl}");
                                }
                                catch (Exception ex) {
                                    Console.WriteLine($"Error adding image entity to context: {ex.Message}");
                                    // Continue even if we can't add this image
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing image: {ex.Message}");
                            // Continue with other images even if one fails
                        }
                    }
                    
                    // Save all image entities
                    try {
                        await _context.SaveChangesAsync();
                        Console.WriteLine("Successfully saved all image entities to database");
                    } catch (Exception ex) {
                        Console.WriteLine($"Error saving images to database: {ex.Message}");
                        if (ex.InnerException != null) {
                            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                        }
                        throw new Exception("Error saving property images to the database.", ex);
                    }
                }
                
                // Commit the transaction
                await transaction.CommitAsync();
                
                // Reload the property with images to return
                var result = await _context.Properties
                    .Include(p => p.PropertyImages)
                    .FirstOrDefaultAsync(p => p.Id == property.Id);
                
                return _mapper.Map<PropertyDto>(result);
            }
            catch (Exception ex)
            {
                // Rollback on any error
                await transaction.RollbackAsync();
                Console.WriteLine($"Error saving property: {ex.Message}");
                throw new Exception("Error saving property to the database.", ex);
            }
        }

        public async Task<PropertyDto> EditPropertyAsync(int propertyId, PropertyUpdateDto updatedPropertyDto, int hostId)
        {
            // Get the property with all related data
            var property = await _context.Properties
                .Include(p => p.PropertyImages)
                .Include(p => p.Amenities)
                .Include(p => p.Host)
                    .ThenInclude(h => h.User)
                .FirstOrDefaultAsync(p => p.Id == propertyId && p.HostId == hostId);
            
            if (property == null) return null;

            // Update basic property information
            _mapper.Map(updatedPropertyDto, property);
            property.UpdatedAt = DateTime.UtcNow;

            // Handle amenities
            if (updatedPropertyDto.AmenityIds != null && updatedPropertyDto.AmenityIds.Any())
            {
                // Clear existing amenities
                property.Amenities.Clear();
                
                // Add new amenities
                foreach (var amenityId in updatedPropertyDto.AmenityIds)
                {
                    var amenity = await _context.Amenities.FindAsync(amenityId);
                    if (amenity != null)
                    {
                        property.Amenities.Add(amenity);
                    }
                }
            }

            // Handle images
            if (updatedPropertyDto.Images != null && updatedPropertyDto.Images.Any())
            {
                // Process existing images (update or delete)
                var existingImageIds = updatedPropertyDto.Images
                    .Where(img => img.Id.HasValue)
                    .Select(img => img.Id.Value)
                    .ToList();
                
                // Remove images that are not in the updated list
                var imagesToRemove = property.PropertyImages
                    .Where(img => !existingImageIds.Contains(img.Id))
                    .ToList();
                
                foreach (var image in imagesToRemove)
                {
                    property.PropertyImages.Remove(image);
                }
                
                // Update existing images
                foreach (var imageDto in updatedPropertyDto.Images.Where(img => img.Id.HasValue))
                {
                    var existingImage = property.PropertyImages.FirstOrDefault(img => img.Id == imageDto.Id);
                    if (existingImage != null)
                    {
                        existingImage.ImageUrl = imageDto.ImageUrl;
                        existingImage.Description = imageDto.Description;
                        existingImage.IsPrimary = imageDto.IsPrimary;
                        existingImage.Category = imageDto.Category;
                    }
                }
                
                // Add new images
                foreach (var imageDto in updatedPropertyDto.Images.Where(img => !img.Id.HasValue))
                {
                    var newImage = new PropertyImage
                    {
                        PropertyId = property.Id,
                        ImageUrl = imageDto.ImageUrl,
                        Description = imageDto.Description,
                        IsPrimary = imageDto.IsPrimary,
                        Category = imageDto.Category,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    property.PropertyImages.Add(newImage);
                }
                
                // Ensure only one primary image
                var primaryImages = property.PropertyImages.Where(img => img.IsPrimary).ToList();
                if (primaryImages.Count > 1)
                {
                    // Keep only the first one as primary
                    for (int i = 1; i < primaryImages.Count; i++)
                    {
                        primaryImages[i].IsPrimary = false;
                    }
                }
                else if (primaryImages.Count == 0 && property.PropertyImages.Any())
                {
                    // If no primary image, set the first one as primary
                    property.PropertyImages.First().IsPrimary = true;
                }
            }

            await _context.SaveChangesAsync();
            
            // Reload the property with all related data after update
            var updatedProperty = await _context.Properties
                .Include(p => p.PropertyImages)
                .Include(p => p.Amenities)
                .Include(p => p.Host)
                    .ThenInclude(h => h.User)
                .FirstOrDefaultAsync(p => p.Id == propertyId);
            
            return _mapper.Map<PropertyDto>(updatedProperty);
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
                .Where(p => p.Id == propertyId && p.Status == "Active")
                .Include(p => p.PropertyImages)
                .Include(p => p.Amenities)
                .Include(p => p.Host)
                    .ThenInclude(h => h.User)
                .Include(p => p.Bookings)
                    .ThenInclude(b => b.Review)
                .FirstOrDefaultAsync();

            if (property == null)
                return null;

            return _mapper.Map<PropertyDto>(property);
        }

        public async Task<List<PropertyDto>> GetHostPropertiesAsync(int hostId)
        {
            var properties = await _context.Properties
                .Where(p => p.Status == "Active" && p.HostId == hostId)
                .Include(p => p.PropertyImages)
                .Include(p => p.Amenities)
                .Include(p => p.Host)
                    .ThenInclude(h => h.User)
                .Include(p => p.Bookings)
                    .ThenInclude(b => b.Review)
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

        public async Task<List<string>> UploadImagesAsync(List<IFormFile> files)
        {
            // Create a generic properties directory for uploaded images
            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "properties");
            Directory.CreateDirectory(uploadPath);

            var imageUrls = new List<string>();
            var baseUrl = "https://localhost:7228"; // This should come from configuration in a real app

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var relativePath = $"/uploads/properties/{fileName}";
                    var fullImageUrl = $"{baseUrl}{relativePath}";
                    imageUrls.Add(fullImageUrl);
                }
            }

            return imageUrls;
        }

        public async Task<bool> AddImagesToPropertyAsync(int propertyId, List<string> imageUrls, int hostId)
        {
            var property = await _context.Properties
                .Include(p => p.PropertyImages)
                .FirstOrDefaultAsync(p => p.Id == propertyId && p.HostId == hostId);

            if (property == null)
            {
                throw new UnauthorizedAccessException("Property not found or you don't have permission to add images.");
            }

            // Create the property-specific upload directory
            var propertyUploadPath = Path.Combine(_environment.WebRootPath, "uploads", "properties", propertyId.ToString());
            Directory.CreateDirectory(propertyUploadPath);

            foreach (var imageUrl in imageUrls)
            {
                // Extract the filename from the URL
                var fileName = Path.GetFileName(imageUrl);
                var sourcePath = Path.Combine(_environment.WebRootPath, "uploads", "properties", fileName);
                
                if (File.Exists(sourcePath))
                {
                    var newPath = Path.Combine(propertyUploadPath, fileName);
                    
                    // Move file from general properties directory to property-specific directory
                    File.Move(sourcePath, newPath);

                    // Create relative URL for the new location
                    var newImageUrl = $"/uploads/properties/{propertyId}/{fileName}";

                    // Save image info to database
                    var propertyImage = new PropertyImage
                    {
                        PropertyId = propertyId,
                        ImageUrl = newImageUrl,
                        IsPrimary = !property.PropertyImages.Any(),
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.PropertyImages.Add(propertyImage);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePropertyAmenitiesAsync(int propertyId, List<int> amenityIds, int hostId)
        {
            var property = await _context.Properties
                .Include(p => p.Amenities)
                .FirstOrDefaultAsync(p => p.Id == propertyId && p.HostId == hostId);

            if (property == null)
                return false;

            // Clear existing amenities
            property.Amenities.Clear();

            // Add new amenities
            foreach (var amenityId in amenityIds)
            {
                var amenity = await _context.Amenities.FindAsync(amenityId);
                if (amenity != null)
                {
                    property.Amenities.Add(amenity);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
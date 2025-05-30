﻿using API.Data;
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
        private readonly IWebHostEnvironment _environment;

        public PropertyService(AppDbContext context, IMapper mapper, IWebHostEnvironment environment)
        {
            _context = context;
            _mapper = mapper;
            _environment = environment;
        }
        public async Task<PropertyDto> AddPropertyAsync(PropertyCreateDto propertyDto, int hostId)
        {
            if (propertyDto.Images != null && propertyDto.Images.Any())
            {
                Console.WriteLine("Image URLs to process:");
                foreach (var img in propertyDto.Images)
                {
                    Console.WriteLine($" - {img.ImageUrl} (IsPrimary: {img.IsPrimary})");
                }
            }

            // Validate MinNights and MaxNights
            if (propertyDto.MinNights > propertyDto.MaxNights)
            {
                throw new ArgumentException("MinNights cannot be greater than MaxNights.");
            }

            var hostExists = await _context.Users.AnyAsync(u => u.Id == hostId && u.Role == "host");
            if (!hostExists)
            {
                throw new ArgumentException($"Host with ID {hostId} does not exist.");
            }

            // Check if category exists
            var categoryExists = await _context.PropertyCategories.AnyAsync(c => c.CategoryId == propertyDto.CategoryId);
            if (!categoryExists)
            {
                Console.WriteLine($"Category with ID {propertyDto.CategoryId} does not exist in database");

                // List available categories for debugging
                var categories = await _context.PropertyCategories.Select(c => new { c.CategoryId, c.Name }).ToListAsync();
                throw new ArgumentException($"Category with ID {propertyDto.CategoryId} does not exist.");
            }

            // Check if cancellation policy exists
            if (propertyDto.CancellationPolicyId.HasValue && propertyDto.CancellationPolicyId.Value > 0)
            {
                var policyExists = await _context.CancellationPolicies.AnyAsync(c => c.Id == propertyDto.CancellationPolicyId);
                if (!policyExists)
                {
                    // Get default policy ID
                    var defaultPolicy = await _context.CancellationPolicies.FirstOrDefaultAsync();
                    if (defaultPolicy != null)
                    {
                        propertyDto.CancellationPolicyId = defaultPolicy.Id;
                    }
                    else
                    {
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
                    propertyDto.CancellationPolicyId = defaultPolicy.Id;
                }
                else
                {
                    throw new ArgumentException("No cancellation policies available. Please create at least one cancellation policy.");
                }
            }

            // Use a transaction to ensure both property and images are saved consistently
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create the property entity
                var property = _mapper.Map<Property>(propertyDto);
                property.HostId = hostId;
                property.CreatedAt = DateTime.UtcNow;
                property.UpdatedAt = DateTime.UtcNow;
                property.Status = "Pending";

                // Add amenities to the property
                if (propertyDto.Amenities != null && propertyDto.Amenities.Any())
                {
                    var amenities = await _context.Amenities
                        .Where(a => propertyDto.Amenities.Contains(a.Id))
                        .ToListAsync();

                    foreach (var amenity in amenities)
                    {
                        property.Amenities.Add(amenity);
                    }
                }

                // Add property to context but don't save yet
                _context.Properties.Add(property);
                try
                {
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Successfully saved property with ID: {property.Id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving property to database: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    throw new Exception("Error saving property to the database.", ex);
                }

                // Populate PropertyAvailability based on MinNights and MaxNights
                var availabilities = new List<PropertyAvailability>();
                var startDate = DateTime.UtcNow.Date;
                var endDate = startDate.AddDays(property.MaxNights);

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    availabilities.Add(new PropertyAvailability
                    {
                        PropertyId = property.Id,
                        Date = date,
                        IsAvailable = true,
                        BlockedReason = null,
                        Price = property.PricePerNight
                    });
                }

                await _context.PropertyAvailabilities.AddRangeAsync(availabilities);
                await _context.SaveChangesAsync();

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

                                    try
                                    {
                                        _context.PropertyImages.Add(propertyImage);
                                        Console.WriteLine($"Added image entity for: {fullImageUrl}");
                                    }
                                    catch (Exception ex)
                                    {
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

                                try
                                {
                                    _context.PropertyImages.Add(propertyImage);
                                    Console.WriteLine($"Added image entity with provided URL: {fullImageUrl}");
                                }
                                catch (Exception ex)
                                {
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

                    //Save all image entities
                    try
                    {
                        await _context.SaveChangesAsync();
                        Console.WriteLine("Successfully saved all image entities to database");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving images to database: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                        }
                        throw new Exception("Error saving property images to the database.", ex);
                    }
                }

                // Commit the transaction
                await transaction.CommitAsync();

                // Reload the property with images and amenities to return
                var result = await _context.Properties
                    .Include(p => p.PropertyImages)
                    .Include(p => p.Amenities)  // Include amenities in the result
                    .FirstOrDefaultAsync(p => p.Id == property.Id);

                return _mapper.Map<PropertyDto>(result);
            }
            catch (Exception ex)
            {
                // Rollback on any error
                await transaction.RollbackAsync();
                throw new Exception("Error saving property to the database.", ex);
            }
        }
        public async Task<PropertyDto> EditPropertyAsync(int propertyId, PropertyUpdateDto updatedPropertyDto, int hostId)
        {
            try
            {
                Console.WriteLine($"Starting EditPropertyAsync for property ID: {propertyId}, host ID: {hostId}");
                
            // Get the property with all related data
            var property = await _context.Properties
                .Include(p => p.PropertyImages)
                .Include(p => p.Amenities)
                .Include(p => p.Host)
                    .ThenInclude(h => h.User)
                .FirstOrDefaultAsync(p => p.Id == propertyId && p.HostId == hostId);

                if (property == null)
                {
                    Console.WriteLine($"Property with ID {propertyId} and host ID {hostId} not found");
                    return null;
                }
                
                Console.WriteLine($"Found property: {property.Title}, CategoryId: {property.CategoryId}, CancellationPolicyId: {property.CancellationPolicyId}");

                // Store the original values
                var originalCancellationPolicyId = property.CancellationPolicyId;
                var originalCategoryId = property.CategoryId;
                var originalAmenities = new List<Amenity>(property.Amenities);

                // Check if cancellation policy exists if it's being updated
                if (updatedPropertyDto.CancellationPolicyId.HasValue && updatedPropertyDto.CancellationPolicyId.Value > 0)
                {
                    Console.WriteLine($"Looking for cancellation policy with ID: {updatedPropertyDto.CancellationPolicyId}");
                    var policyExists = await _context.CancellationPolicies.AnyAsync(c => c.Id == updatedPropertyDto.CancellationPolicyId);
                    if (!policyExists)
                    {
                        Console.WriteLine($"Cancellation policy with ID {updatedPropertyDto.CancellationPolicyId} does not exist");
                        // Keep the existing policy ID instead of changing it
                        updatedPropertyDto.CancellationPolicyId = originalCancellationPolicyId;
                    }
                }
                else
                {
                    // If no cancellation policy ID is provided, keep the original one
                    updatedPropertyDto.CancellationPolicyId = originalCancellationPolicyId;
                }

                // Check if category exists if it's being updated
                if (updatedPropertyDto.CategoryId.HasValue && updatedPropertyDto.CategoryId.Value > 0)
                {
                    Console.WriteLine($"Looking for category with ID: {updatedPropertyDto.CategoryId}");
                    var categoryExists = await _context.PropertyCategories.AnyAsync(c => c.CategoryId == updatedPropertyDto.CategoryId);
                    if (!categoryExists)
                    {
                        Console.WriteLine($"Category with ID {updatedPropertyDto.CategoryId} does not exist");
                        // Keep the existing category ID instead of changing it
                        updatedPropertyDto.CategoryId = originalCategoryId;
                    }
                }
                else
                {
                    // If no category ID is provided, keep the original one
                    updatedPropertyDto.CategoryId = originalCategoryId;
                }

                // Update only the fields that are provided in the DTO
                if (updatedPropertyDto.Title != null)
                    property.Title = updatedPropertyDto.Title;
                
                if (updatedPropertyDto.Description != null)
                    property.Description = updatedPropertyDto.Description;
                
                if (updatedPropertyDto.PropertyType != null)
                    property.PropertyType = updatedPropertyDto.PropertyType;
             

                if (updatedPropertyDto.Country != null)
                    property.Country = updatedPropertyDto.Country;
                
                if (updatedPropertyDto.Address != null)
                    property.Address = updatedPropertyDto.Address;
                
                if (updatedPropertyDto.City != null)
                    property.City = updatedPropertyDto.City;
                
                if (updatedPropertyDto.Currency != null)
                    property.Currency = updatedPropertyDto.Currency;
                
                if (updatedPropertyDto.PricePerNight.HasValue)
                    property.PricePerNight = updatedPropertyDto.PricePerNight.Value;
                
                if (updatedPropertyDto.CleaningFee.HasValue)
                    property.CleaningFee = updatedPropertyDto.CleaningFee.Value;
                
                if (updatedPropertyDto.ServiceFee.HasValue)
                    property.ServiceFee = updatedPropertyDto.ServiceFee.Value;
                
                if (updatedPropertyDto.MinNights.HasValue)
                    property.MinNights = updatedPropertyDto.MinNights.Value;
                
                if (updatedPropertyDto.MaxNights.HasValue)
                    property.MaxNights = updatedPropertyDto.MaxNights.Value;
                
                if (updatedPropertyDto.MaxGuests.HasValue)
                    property.MaxGuests = updatedPropertyDto.MaxGuests.Value;
                
                if (updatedPropertyDto.Bedrooms.HasValue)
                    property.Bedrooms = updatedPropertyDto.Bedrooms.Value;
                
                if (updatedPropertyDto.Bathrooms.HasValue)
                    property.Bathrooms = updatedPropertyDto.Bathrooms.Value;
                
                if (updatedPropertyDto.CancellationPolicyId.HasValue)
                    property.CancellationPolicyId = updatedPropertyDto.CancellationPolicyId.Value;
                
                if (updatedPropertyDto.CategoryId.HasValue)
                    property.CategoryId = updatedPropertyDto.CategoryId.Value;
                
            property.UpdatedAt = DateTime.UtcNow;

                // Handle amenities only if AmenityIds is provided
                if (updatedPropertyDto.AmenityIds != null)
            {
                // Clear existing amenities
                //property.Amenities.Clear();

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

                // Only update availabilities if MaxNights has changed
                if (updatedPropertyDto.MaxNights.HasValue && updatedPropertyDto.MaxNights.Value != property.MaxNights)
                {
            var startDate = DateTime.UtcNow.Date; 
            var endDate = startDate.AddDays(property.MaxNights); 

            var existingAvailabilities = await _context.PropertyAvailabilities
                .Where(pa => pa.PropertyId == property.Id)
                .ToListAsync();

            var availabilitiesToRemove = existingAvailabilities
                .Where(pa => pa.Date < startDate || pa.Date > endDate)
                .ToList();

            _context.PropertyAvailabilities.RemoveRange(availabilitiesToRemove);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var existingAvailability = existingAvailabilities.FirstOrDefault(pa => pa.Date == date);
                if (existingAvailability != null)
                {
                    // Update existing availability
                    existingAvailability.IsAvailable = true;
                    existingAvailability.BlockedReason = null;
                    existingAvailability.Price = property.PricePerNight;
                }
                else
                {
                    // Add new availability
                    _context.PropertyAvailabilities.Add(new PropertyAvailability
                    {
                        PropertyId = property.Id,
                        Date = date,
                        IsAvailable = true,
                        BlockedReason = null,
                        Price = property.PricePerNight
                    });
                }
            }
                }

            await _context.SaveChangesAsync();
                Console.WriteLine($"Successfully saved property updates for ID: {propertyId}");

            // Reload the property with all related data after update
            var updatedProperty = await _context.Properties
                .Include(p => p.PropertyImages)
                .Include(p => p.Amenities)
                .Include(p => p.Host)
                    .ThenInclude(h => h.User)
                .FirstOrDefaultAsync(p => p.Id == propertyId);

            return _mapper.Map<PropertyDto>(updatedProperty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating property: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
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
                .Where(p => p.Id == propertyId)
                .Include(p => p.PropertyImages)
                .Include(p => p.Category)
                .Include(p => p.CancellationPolicy)
                .Include(p => p.Amenities)
                .Include(p => p.Host)
                    .ThenInclude(h => h.User)
                .Include(p => p.Bookings)
                    .ThenInclude(b => b.Review)
                    .ThenInclude(r => r.Reviewer)
                .FirstOrDefaultAsync();


            if (property == null)
                return null;

            return _mapper.Map<PropertyDto>(property);
        }

        public async Task<List<PropertyDto>> GetHostPropertiesAsync(int hostId)
        {
            var properties = await _context.Properties
                .Where(p => p.HostId == hostId &&( p.Status == "Active" || p.Status == "Pending"))
                .Include(p => p.Category)
                .Include(p => p.PropertyImages)
                .Include(p => p.Amenities)
                .Include(p => p.Host)
                    .ThenInclude(h => h.User)
                .Include(p => p.Bookings)
                    .ThenInclude(b => b.Review)
                .ToListAsync();

            return _mapper.Map<List<PropertyDto>>(properties);
        }

        public async Task<(List<PropertyDto> Properties, int Total)> GetAllPropertiesAsync(int page, int pageSize, int? categoryId = null, decimal? minPrice = null, decimal? maxPrice = null, int? excludeHostId = null)
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 12;

            // Build the base query for active properties
            IQueryable<Property> query = _context.Properties
                .Where(p => p.Status == "Active" && p.Host.IsVerified && p.Host.User.AccountStatus == "Active")
                .Include(p => p.Category)
                .Include(p => p.PropertyImages)
                .Include(p => p.CancellationPolicy)
                .Include(p => p.Amenities)
                .Include(p => p.Host)
                    .ThenInclude(h => h.User)
                .Include(p => p.Bookings)
                    .ThenInclude(b => b.Review);

            // Exclude properties owned by the specified host
            if (excludeHostId.HasValue)
            {
                query = query.Where(p => p.HostId != excludeHostId.Value);
            }

            // Apply category filter if categoryId is provided
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Apply price range filters if provided
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.PricePerNight >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.PricePerNight <= maxPrice.Value);
            }

            // Get the total count of filtered properties
            var total = await query.CountAsync();

            // Fetch paginated properties
            var properties = await query
                .OrderBy(p => p.Id) // Optional: Define a consistent order
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (_mapper.Map<List<PropertyDto>>(properties), total);
        }


        public async Task<List<string>> GetUniqueCountriesAsync()
        {
            return await _context.Properties
                .Where(p => p.Status == "Active")
                .Select(p => p.Country)
                .Distinct()
                .Where(c => !string.IsNullOrEmpty(c))
                .OrderBy(c => c)
                .ToListAsync();
        }


        public async Task<List<PropertyDto>> SearchPropertiesAsync(string title = null, string country = null, int? minNights = null, int? maxNights = null, DateTime? startDate = null, DateTime? endDate = null, int? maxGuests = null, int? excludeHostId = null, int? page = null, int? pageSize = null, int? categoryId = null)
        {
            // Start with all active properties
            var query = _context.Properties
                .Where(p => p.Status == "Active")
                .AsQueryable();

            // Exclude properties owned by the specified host
            if (excludeHostId.HasValue)
            {
                query = query.Where(p => p.HostId != excludeHostId.Value);
            }

            // Apply filters based on search criteria
            if (!string.IsNullOrEmpty(title))
            {
                query = query.Where(p => p.Title.Contains(title));
            }

            if (!string.IsNullOrEmpty(country))
            {
                query = query.Where(p => p.Country.Contains(country));
            }

            if (minNights.HasValue)
            {
                query = query.Where(p => p.MinNights >= minNights.Value);
            }

            if (maxNights.HasValue)
            {
                query = query.Where(p => p.MaxNights <= maxNights.Value);
            }

            if (maxGuests.HasValue)
            {
                query = query.Where(p => p.MaxGuests >= maxGuests.Value);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Include necessary relationships
            query = query
                .Include(p => p.Category)
                .Include(p => p.PropertyImages)
                .Include(p => p.Amenities)
                .Include(p => p.Host)
                    .ThenInclude(h => h.User)
                .Include(p => p.Bookings)
                    .ThenInclude(b => b.Review);

            // Apply pagination if provided
            if (page.HasValue && pageSize.HasValue)
            {
                int skipCount = (page.Value - 1) * pageSize.Value;
                query = query.Skip(skipCount).Take(pageSize.Value);
            }

            var properties = await query.ToListAsync();
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
            try
            {
                Console.WriteLine($"Starting AddImagesToPropertyAsync for property ID: {propertyId}, host ID: {hostId}");
                
                // Verify the property exists and belongs to the host
            var property = await _context.Properties
                .Include(p => p.PropertyImages)
                .FirstOrDefaultAsync(p => p.Id == propertyId && p.HostId == hostId);

            if (property == null)
            {
                    Console.WriteLine($"Property with ID {propertyId} and host ID {hostId} not found");
                    return false;
            }

                Console.WriteLine($"Found property: {property.Title}, current image count: {property.PropertyImages.Count}");
                
                // Create property-specific directory if it doesn't exist
            var propertyUploadPath = Path.Combine(_environment.WebRootPath, "uploads", "properties", propertyId.ToString());
            Directory.CreateDirectory(propertyUploadPath);
                
                var baseUrl = "https://localhost:7228"; // This should come from configuration in a real app

            foreach (var imageUrl in imageUrls)
            {
                    try
                    {
                        // Extract the filename from the URL or path
                var fileName = Path.GetFileName(imageUrl);
                var sourcePath = Path.Combine(_environment.WebRootPath, "uploads", "properties", fileName);
                        
                        Console.WriteLine($"Processing image: {fileName}");
                        Console.WriteLine($"Source path: {sourcePath}");

                if (File.Exists(sourcePath))
                {
                    var newPath = Path.Combine(propertyUploadPath, fileName);
                            Console.WriteLine($"Moving file to: {newPath}");
                            
                            // If the destination file already exists, delete it
                            if (File.Exists(newPath))
                            {
                                File.Delete(newPath);
                                Console.WriteLine("Deleted existing file at destination");
                            }
                            
                            // Copy instead of move to handle potential file locks
                            File.Copy(sourcePath, newPath);
                            Console.WriteLine("File copied successfully");
                            
                            try
                            {
                                // Try to delete the source file, but don't fail if we can't
                                File.Delete(sourcePath);
                                Console.WriteLine("Original file deleted");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Could not delete original file: {ex.Message}");
                            }

                    // Create relative URL for the new location
                            var relativePath = $"/uploads/properties/{propertyId}/{fileName}";
                            var fullImageUrl = $"{baseUrl}{relativePath}";

                    // Save image info to database
                    var propertyImage = new PropertyImage
                    {
                        PropertyId = propertyId,
                                ImageUrl = fullImageUrl,
                                IsPrimary = !property.PropertyImages.Any(), // Set as primary if it's the first image
                                CreatedAt = DateTime.UtcNow,
                                Category = "Additional"  // Default category
                            };
                            
                            _context.PropertyImages.Add(propertyImage);
                            Console.WriteLine($"Added image record with URL: {fullImageUrl}");
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Source image not found at {sourcePath}");
                            Console.WriteLine("Checking if URL is already in property-specific directory...");
                            
                            // Check if the file exists in the property-specific directory
                            var propertySpecificPath = Path.Combine(propertyUploadPath, fileName);
                            if (File.Exists(propertySpecificPath))
                            {
                                Console.WriteLine("File already exists in property directory");
                                var relativePath = $"/uploads/properties/{propertyId}/{fileName}";
                                var fullImageUrl = $"{baseUrl}{relativePath}";
                                
                                var propertyImage = new PropertyImage
                                {
                                    PropertyId = propertyId,
                                    ImageUrl = fullImageUrl,
                        IsPrimary = !property.PropertyImages.Any(),
                                    CreatedAt = DateTime.UtcNow,
                                    Category = "Additional"
                    };

                    _context.PropertyImages.Add(propertyImage);
                                Console.WriteLine($"Added image record for existing file: {fullImageUrl}");
                            }
                            else
                            {
                                Console.WriteLine("File not found in any expected location");
                                // If we can't find the file, log it but continue processing other images
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing image {imageUrl}: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                        }
                        // Continue with other images even if one fails
                }
            }

            await _context.SaveChangesAsync();
                Console.WriteLine($"Successfully added {imageUrls.Count} images to property {propertyId}");
                
            return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding images to property: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public async Task<bool> UpdatePropertyAmenitiesAsync(int propertyId, List<int> amenityIds, int hostId)
        {
            var property = await _context.Properties
                .Include(p => p.Amenities)
                .FirstOrDefaultAsync(p => p.Id == propertyId && p.HostId == hostId);

            if (property == null)
                return false;

            // Clear existing amenities
            //property.Amenities.Clear();

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

        public async Task<bool> DeletePropertyImageAsync(int propertyId, int imageId, int hostId)
        {
            try
            {
                Console.WriteLine($"Starting DeletePropertyImageAsync for property ID: {propertyId}, image ID: {imageId}, host ID: {hostId}");

                // Verify the property exists and belongs to the host
                var property = await _context.Properties
                    .Include(p => p.PropertyImages)
                    .FirstOrDefaultAsync(p => p.Id == propertyId && p.HostId == hostId);

                if (property == null)
                {
                    Console.WriteLine($"Property with ID {propertyId} and host ID {hostId} not found");
                    return false;
                }

                // Find the image to delete
                var imageToDelete = property.PropertyImages.FirstOrDefault(img => img.Id == imageId);
                if (imageToDelete == null)
                {
                    Console.WriteLine($"Image with ID {imageId} not found for property {propertyId}");
                    return false;
                }

                // Extract the filename from the image URL
                var imageUrl = imageToDelete.ImageUrl;
                var fileName = Path.GetFileName(imageUrl);

                // Construct the file path
                var propertyUploadPath = Path.Combine(_environment.WebRootPath, "uploads", "properties", propertyId.ToString());
                var filePath = Path.Combine(propertyUploadPath, fileName);

                // Remove the image from the database
                _context.PropertyImages.Remove(imageToDelete);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Successfully removed image {imageId} from database");

                // Try to delete the physical file
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                        Console.WriteLine($"Successfully deleted physical file at {filePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not delete physical file: {ex.Message}");
                        // Continue even if we can't delete the physical file
                    }
                }
                else
                {
                    Console.WriteLine($"Warning: Physical file not found at {filePath}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting property image: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }
    }
}
using API.Data;
using API.DTOs;
using API.DTOs.Profile;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace API.Services
{
    public class ProfileService : IProfileService
    {
        private readonly AppDbContext _context;

        public ProfileService(AppDbContext context)
        {
            _context = context;

        }


        public Task<User> GetUserByIdAsync(string id)
        {
            var user = _context.Users.FirstOrDefault(x =>
                x.Id.ToString() == id
            );
            if (user == null)
            {
                throw new Exception("User not found");
            }
            return Task.FromResult(user);
        }
        public Task<Models.Host> GetHostByUserIdAsync(int userId)
        {
            var host = _context.HostProfules.FirstOrDefault(x =>
                x.HostId == userId
            );
            if (host == null)
            {
                throw new Exception("Host not found");
            }
            return Task.FromResult(host);
        }


        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
        public async Task<bool> emailExists(string email)
        {

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
            return user != null;

        }


        public async Task<IEnumerable<HostReviewDto>> GetHostReviewsAsync(int hostId)
        {
            return await _context.Reviews
                .Where(r => r.Booking.Property.HostId == hostId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new HostReviewDto
                {
                    Id = r.Id,
                    PropertyId = r.Booking.PropertyId,
                    PropertyTitle = r.Booking.Property.Title,
                    GuestName = $"{r.Reviewer.FirstName} {r.Reviewer.LastName}",
                    GuestAvatar = r.Reviewer.ProfilePictureUrl ?? string.Empty,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();
        }



        public async Task<IEnumerable<HostProfileListingsDto>> GetHostListingsAsync(int hostId)
        {
            var listings = await _context.Properties
                .Where(p => p.HostId == hostId)
                .Include(p => p.Category)
                .Include(p => p.PropertyImages)
                .Select(p => new HostProfileListingsDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    PricePerNight = p.PricePerNight,
                    PropertyType = p.PropertyType,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    Images = p.PropertyImages.Select(i => new PropertyImageDto
                    {
                        ImageUrl = i.ImageUrl,
                    }).ToList(),
                    Rating = _context.Reviews
                .Where(r => r.Booking.PropertyId == p.Id)
                .Select(r => (double)r.Rating)
                .DefaultIfEmpty()
                .Average() // You'll need to implement this
                })
                .ToListAsync();

            return listings;

        }
        public async Task<EditProfileDto> getUserForEditProfile(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Host)
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            if (user == null)
            {
                throw new Exception("User not found");
            }

            var dto = new EditProfileDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                role = user.Role,
                Email = user.Email,
                DateOfBirth = user.DateOfBirth,
                ProfilePictureUrl = user.ProfilePictureUrl,

                AboutMe = user.Host?.AboutMe,
                Work = user.Host?.Work,
                Education = user.Host?.Education,
                Languages = user.Host?.Languages,
                LivesIn = user.Host?.LivesIn,
                DreamDestination = user.Host?.DreamDestination,
                FunFact = user.Host?.FunFact,
                Pets = user.Host?.Pets,
                ObsessedWith = user.Host?.ObsessedWith,
                SpecialAbout = user.Host?.SpecialAbout
            };
            return dto;


        }


        public async Task<EditProfileDto> EditProfileAsync(EditProfileDto dto)
        {

            var user = await _context.Users
                .Include(u => u.Host)
                .FirstOrDefaultAsync(u => u.Id.ToString()== dto.Id);
            if (user == null)
            {
                throw new Exception("User not found");
            }
            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Email = user.Email;
            user.DateOfBirth = dto.DateOfBirth;
            user.ProfilePictureUrl = dto.ProfilePictureUrl;

            if (user.Host != null)
            {
                user.Host.AboutMe = dto.AboutMe;
                user.Host.Work = dto.Work;
                user.Host.Education = dto.Education;
                user.Host.Languages = dto.Languages;
                user.Host.LivesIn = dto.LivesIn;
                user.Host.DreamDestination = dto.DreamDestination;
                user.Host.FunFact = dto.FunFact;
                user.Host.Pets = dto.Pets;
                user.Host.ObsessedWith = dto.ObsessedWith;
                user.Host.SpecialAbout = dto.SpecialAbout;
            }

            await _context.SaveChangesAsync();
            return dto;

        }



        public async Task<Favourite> AddToFavouritesAsync(Favourite fav)
        {

            try
            {

            var existingFavourite = await _context.Favourites
                .FirstOrDefaultAsync(f => f.UserId == fav.UserId && f.PropertyId == fav.PropertyId);
            if (existingFavourite != null)
            {
                // If it already exists, remove it
                _context.Favourites.Remove(existingFavourite);
            }
            else
            {
                // If it doesn't exist, add it
                fav.FavoritedAt = DateTime.UtcNow;
                await _context.Favourites.AddAsync(fav);
            }
            await _context.SaveChangesAsync();
            return fav;


            }
            catch (Exception ex)
            {
                throw new Exception("Error adding to favourites", ex);
            }

        }

        public async Task<List<object>> GetUserFavoritesAsync(int userId)
        {
            try
            {

            var favorites = await _context.Favourites
                .Where(f => f.UserId == userId)
                .Include(f => f.Property)
                .Include(f => f.Property.PropertyImages)
                .Include(f => f.Property.Category)
                .Select(f => new
                {
                    f.Id,
                    f.UserId,
                    f.PropertyId,
                    f.FavoritedAt,
                    Property = new
                    {
                        f.Property.Id,
                        f.Property.Title,
                        f.Property.Description,
                        f.Property.City,
                        f.Property.Country,
                        f.Property.Bedrooms,
                        f.Property.Bathrooms,
                        f.Property.MaxGuests,
                        f.Property.PricePerNight,
                        f.Property.Currency,
                        Category = f.Property.Category.Name,
                        Images = f.Property.PropertyImages
                            .Select(img => new
                            {
                                img.Id,
                                img.ImageUrl,
                                img.IsPrimary,
                                img.Category,
                                img.Description
                            })
                            .ToList(),
                        PrimaryImage = f.Property.PropertyImages
                            .Where(img => img.IsPrimary)
                            .Select(img => img.ImageUrl)
                            .FirstOrDefault(),
                        AverageRating = _context.Reviews
                            .Where(r => r.Booking.PropertyId == f.PropertyId)
                            .Select(r => r.Rating)
                            .DefaultIfEmpty()
                            .Average()
                    }
                })
                .OrderByDescending(f => f.FavoritedAt)
                .ToListAsync();

            return favorites.Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving user favorites", ex);
            }

        }

        public async Task<bool> IsPropertyInFavoritesAsync(int userId, int propertyId)
        {
            return await _context.Favourites
                .AnyAsync(f => f.UserId == userId && f.PropertyId == propertyId);
        }
        public async Task<Review> addReview(ReviewRequestDto review)
        {
            var newReview = new Review
            {
                BookingId = review.BookingId,
                ReviewerId = review.ReviewerId,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Reviews.AddAsync(newReview);
            await _context.SaveChangesAsync();
            return newReview;
        }
        public Task<IEnumerable<Review>> GetUserReviewsAsync(int userId)
        {
            var reviews = _context.Reviews
                .Where(r => r.ReviewerId == userId)
                ;
            return Task.FromResult(reviews.AsEnumerable());
        }
    }
}

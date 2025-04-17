using API.DTOs.Profile;
using API.Models;

namespace API.Services
{
    public interface IProfileService
    {
        Task<User> GetUserByIdAsync(string id);

        Task<Models.Host> GetHostByUserIdAsync(int userId);
        Task UpdateUserAsync(User user);
        Task<IEnumerable<HostReviewDto>> GetHostReviewsAsync(int hostId);
        Task<IEnumerable<HostProfileListingsDto>> GetHostListingsAsync(int hostId);
        Task<EditProfileDto> getUserForEditProfile(string userId);
        Task<EditProfileDto> EditProfileAsync(EditProfileDto dto);
        Task<Favourite> AddToFavouritesAsync(Favourite dto);
        Task<bool> IsPropertyInFavoritesAsync(int userId, int propertyId);
        Task<List<object>> GetUserFavoritesAsync(int userId);




    }

}

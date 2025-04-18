using API.DTOs;
using API.DTOs.Amenity;
using API.DTOs.Review;
using API.Models;
using AutoMapper;
using API.DTOs.Amenity;
using API.DTOs.Review;

public class PropertyProfile : Profile
{
    public PropertyProfile()
    {
        CreateMap<Property, PropertyDto>()
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.PropertyImages))
            //.ForMember(dest => dest.Amenities, opt => opt.MapFrom(src => src.Amenities)) // Uncommented
            .ForMember(dest => dest.Reviews, opt => opt.MapFrom(src => src.Bookings.Select(b => b.Review).Where(r => r != null)))
            .ForMember(dest => dest.HostName, opt => opt.MapFrom(src => $"{src.Host.User.FirstName} {src.Host.User.LastName}"))
            .ForMember(dest => dest.HostProfileImage, opt => opt.MapFrom(src => src.Host.User.ProfilePictureUrl))
            .ForMember(dest => dest.FavoriteCount, opt => opt.MapFrom(src => src.Favourites.Count))
            .ForMember(dest => dest.IsGuestFavorite, opt => opt.MapFrom(src => src.Favourites.Any()))
            .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src => src.Bookings.Select(b => b.Review).Where(r => r != null).Any()
                ? src.Bookings.Select(b => b.Review).Where(r => r != null).Average(r => r.Rating)
                : 0.0))
            .ForMember(dest => dest.ReviewCount, opt => opt.MapFrom(src => src.Bookings.Select(b => b.Review).Count(r => r != null)));

        CreateMap<PropertyCreateDto, Property>()
            .ForMember(dest => dest.CleaningFee, opt => opt.MapFrom(src => src.CleaningFee ?? 0m))
            .ForMember(dest => dest.ServiceFee, opt => opt.MapFrom(src => src.ServiceFee ?? 0m))
            .ForMember(dest => dest.MinNights, opt => opt.MapFrom(src => src.MinNights ?? 1))
            .ForMember(dest => dest.MaxNights, opt => opt.MapFrom(src => src.MaxNights ?? 30))
            .ForMember(dest => dest.Bedrooms, opt => opt.MapFrom(src => src.Bedrooms ?? 1))
            .ForMember(dest => dest.Bathrooms, opt => opt.MapFrom(src => src.Bathrooms ?? 1))
            .ForMember(dest => dest.MaxGuests, opt => opt.MapFrom(src => src.MaxGuests ?? 1))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency ?? "USD"))
            .ForMember(dest => dest.InstantBook, opt => opt.MapFrom(src => src.InstantBook ?? false))
                .ForMember(dest => dest.Amenities, opt => opt.Ignore()) // Add this line to ignore amenities during mapping
            .ForMember(dest => dest.CancellationPolicyId, opt => opt.MapFrom(src => src.CancellationPolicyId ?? 1));

        //CreateMap<PropertyUpdateDto, Property>()
        //    .ForMember(dest => dest.CheckInTime, opt => opt.MapFrom(src =>
        //        !string.IsNullOrEmpty(src.CheckInTime) ? TimeSpan.Parse(src.CheckInTime) : (TimeSpan?)null))
        //    .ForMember(dest => dest.CheckOutTime, opt => opt.MapFrom(src =>
        //        !string.IsNullOrEmpty(src.CheckOutTime) ? TimeSpan.Parse(src.CheckOutTime) : (TimeSpan?)null))
        //    .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<PropertyImage, PropertyImageDto>();
        CreateMap<Amenity, AmenityDto>();
        CreateMap<Review, ReviewDto>()
            .ForMember(dest => dest.Booking_Id, opt => opt.MapFrom(src => src.BookingId))
            .ForMember(dest => dest.ReviewerId, opt => opt.MapFrom(src => src.ReviewerId))
            .ForMember(dest => dest.ReviewerName, opt => opt.MapFrom(src =>
            src.Reviewer != null ? $"{src.Reviewer.FirstName} {src.Reviewer.LastName}" : null))
            .ForMember(dest => dest.ReviewerImage, opt => opt.MapFrom(src =>
            src.Reviewer != null ? src.Reviewer.ProfilePictureUrl : null));
    }
}
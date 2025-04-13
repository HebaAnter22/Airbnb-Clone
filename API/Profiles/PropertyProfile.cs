using API.DTOs;
using API.Models;
using AutoMapper;

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

        CreateMap<PropertyCreateDto, Property>();
        CreateMap<PropertyUpdateDto, Property>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<PropertyImage, PropertyImageDto>();
        CreateMap<Amenity, AmenityDto>();
        CreateMap<Review, ReviewDto>();
    }
}
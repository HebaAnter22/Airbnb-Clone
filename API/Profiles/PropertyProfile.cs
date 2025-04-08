using API.DTOs;
using API.Models;
using AutoMapper;

namespace API.Profiles
{
    public class PropertyProfile : Profile
    {
        public PropertyProfile()
        {
            CreateMap<PropertyCreateDto, Property>();
            CreateMap<PropertyUpdateDto, Property>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<Property, PropertyDto>();
            CreateMap<PropertyImage, PropertyImageDto>();
        }
    }
}
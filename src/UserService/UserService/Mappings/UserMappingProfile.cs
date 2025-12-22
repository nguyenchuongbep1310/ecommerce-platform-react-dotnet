using AutoMapper;
using UserService.Models;
using UserService.DTOs;

namespace UserService.Mappings
{
    /// <summary>
    /// AutoMapper profile for User-related mappings
    /// </summary>
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            // ApplicationUser -> UserProfileDto
            CreateMap<ApplicationUser, UserProfileDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName ?? string.Empty));

            // RegisterRequestDto -> ApplicationUser
            CreateMap<RegisterRequestDto, ApplicationUser>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName));
        }
    }
}

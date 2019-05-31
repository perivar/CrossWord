using AutoMapper;
using CrossWord.API.Models;
using Microsoft.AspNetCore.Identity;

namespace CrossWord.API
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<IdentityUser, UserModel>();
            CreateMap<UserModel, IdentityUser>();
        }
    }
}
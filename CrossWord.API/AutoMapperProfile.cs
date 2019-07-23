using AutoMapper;
using CrossWord.API.Models;
using Microsoft.AspNetCore.Identity;
using CrossWord.Scraper.MySQLDbService.Entities;

namespace CrossWord.API
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<ApplicationUser, UserModel>();
            CreateMap<UserModel, ApplicationUser>();

            CreateMap<ApplicationUser, UserModelRegister>();
            CreateMap<UserModelRegister, ApplicationUser>();            
        }
    }
}
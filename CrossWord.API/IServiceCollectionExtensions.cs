using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CrossWord.API
{
    /// <summary>
    /// Class to not add default UI when using Identity Frameword
    /// You cannot use AddDefaultIdentity, since internally, this calls AddDefaultUI, which contains the Razor Pages "endpoints" you don't want. 
    /// You'll need to use AddIdentity<TUser, TRole> or AddIdentityCore<TUser> instead.
    /// https://github.com/aspnet/Identity/blob/master/src/UI/IdentityServiceCollectionUIExtensions.cs#L47
    /// </summary>
    public static class IServiceCollectionExtensions
    {
        public static IdentityBuilder AddCustomDefaultIdentity<TUser>(this IServiceCollection services, Action<IdentityOptions> configureOptions) where TUser : class
        {
            services.AddAuthentication(o =>
            {
                o.DefaultScheme = IdentityConstants.ApplicationScheme;
                o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies(o => { });

            return services.AddIdentityCore<TUser>(o =>
            {
                o.Stores.MaxLengthForKeys = 128;
                configureOptions?.Invoke(o);
            })
            .AddDefaultTokenProviders();
        }
    }
}
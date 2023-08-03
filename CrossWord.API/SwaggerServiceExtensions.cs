using Microsoft.OpenApi.Models;

namespace CrossWord.API
{
    public static class SwaggerServiceExtensions
    {
        public static Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions EnableBearingAuthentication(this Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
        {
            // https://dev.to/eduardstefanescu/aspnet-core-swagger-documentation-with-bearer-authentication-40l6
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter the Bearer Authorization string as following: `Bearer GENERATED-JWT-TOKEN`",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                            Reference = new OpenApiReference
                            {
                                Id = "Bearer", // The name of the previously defined security scheme.
                                Type = ReferenceType.SecurityScheme,
                            },
                        },
                        new List<string>()
                    }
                });

            return options;
        }
    }
}
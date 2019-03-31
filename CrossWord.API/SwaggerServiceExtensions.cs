using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace CrossWord.API
{
    public static class SwaggerServiceExtensions
    {
        // https://ppolyzos.com/2017/10/30/add-jwt-bearer-authorization-to-swagger-and-asp-net-core/
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Main API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new ApiKeyScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });

                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    { "Bearer", new string[] { } }
                });
            });

            return services;
        }

        public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Main API V1");
            });

            return app;
        }
    }
}
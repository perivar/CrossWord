using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Swashbuckle.AspNetCore.Swagger;
using static Microsoft.AspNet.OData.Query.AllowedQueryOptions;
using static Microsoft.AspNetCore.Mvc.CompatibilityVersion;

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

        public static IServiceCollection AddSwaggerODataWorkaround(this IServiceCollection services)
        {
            // Required to get Swagger to work with OData Controllers
            // Workaround: https://github.com/OData/WebApi/issues/1177
            services.AddMvcCore(options =>
            {
                foreach (var outputFormatter in options.OutputFormatters.OfType<ODataOutputFormatter>().Where(_ => _.SupportedMediaTypes.Count == 0))
                {
                    outputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/prs.odatatestxx-odata"));
                }
                foreach (var inputFormatter in options.InputFormatters.OfType<ODataInputFormatter>().Where(_ => _.SupportedMediaTypes.Count == 0))
                {
                    inputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/prs.odatatestxx-odata"));
                }
            });

            return services;
        }

        public static IServiceCollection AddODataSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddOData();
            services.AddSwaggerODataWorkaround();
            services.AddSwaggerDocumentation();

            return services;
        }

        public static IServiceCollection AddODataVersioningSwaggerDocumentation(this IServiceCollection services)
        {
            // allow a client to call you without specifying an api version
            // since we haven't configured it otherwise, the assumed api version will be 1.0
            services.AddApiVersioning(option =>
            {
                option.AssumeDefaultVersionWhenUnspecified = true;
                option.DefaultApiVersion = new ApiVersion(1, 0);

                // This option enables sending the api-supported-versions and api-deprecated-versions HTTP header in responses.
                option.ReportApiVersions = true;

                // option.ApiVersionReader = new UrlSegmentApiVersionReader();
            });

            services.AddOData().EnableApiVersioning();
            services.AddODataApiExplorer(
                           options =>
                           {
                               // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                               // note: the specified format code will format the version as "'v'major[.minor][-status]"
                               options.GroupNameFormat = "'v'VVV";

                               // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                               // can also be used to control the format of the API version in route templates
                               options.SubstituteApiVersionInUrl = true;

                               options.AssumeDefaultVersionWhenUnspecified = true;
                               options.DefaultApiVersion = new ApiVersion(1, 0);

                               // configure query options (which cannot otherwise be configured by OData conventions)
                               //    options.QueryOptions.Controller<V2.PeopleController>()
                               //                                   .Action(c => c.Get(default)).Allow(Skip | Count).AllowTop(100);

                               //    options.QueryOptions.Controller<V3.PeopleController>()
                               //                        .Action(c => c.Get(default)).Allow(Skip | Count).AllowTop(100);
                           });

            services.AddSwaggerGen(
              options =>
              {
                  var provider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();

                  foreach (var description in provider.ApiVersionDescriptions)
                  {
                      options.SwaggerDoc(
                      description.GroupName,
                      new Info()
                      {
                          Title = $"Main API {description.ApiVersion}",
                          Version = description.ApiVersion.ToString()
                      });
                  }

                  // add a custom operation filter which sets default values
                  options.OperationFilter<SwaggerDefaultValues>();

                  // integrate xml comments, remember to generate the xml .csproj
                  options.IncludeXmlComments(XmlCommentsFilePath);

                  options.AddSecurityDefinition("Bearer", new ApiKeyScheme
                  {
                      Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                      Name = "Authorization",
                      In = "header",
                      Type = "apiKey"
                  });

                  options.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                    {
                        { "Bearer", new string[] { } }
                    });

                  // remove Conflicting schemaIds: Identical schemaIds detected for type when using both ControllerBase and ODataController
                  // options.CustomSchemaIds(x => x.Assembly.IsDynamic ? "Dynamic." + x.FullName : x.FullName);
                  // options.CustomSchemaIds(x => x.FullName);
              });

            services.AddSwaggerODataWorkaround();

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

        public static IApplicationBuilder UseODataVersioningSwaggerDocumentation(this IApplicationBuilder app,
                                                                VersionedODataModelBuilder modelBuilder,
                                                                IApiVersionDescriptionProvider provider)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(
                options =>
                {
                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                    }

                    // options.RoutePrefix = string.Empty; // To serve the Swagger UI at the app's root (http://localhost:<port>/), set the RoutePrefix property to an empty string
                });

            return app;
        }

        static string XmlCommentsFilePath
        {
            get
            {
                // Determine base path for the application.
                var basePath = AppContext.BaseDirectory;
                var assemblyName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
                var fileName = System.IO.Path.GetFileName(assemblyName + ".xml");
                return Path.Combine(basePath, fileName);
            }
        }
    }
}
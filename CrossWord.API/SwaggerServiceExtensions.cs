using System.Reflection;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc.Versioning.Conventions;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CrossWord.API
{
    public static class SwaggerServiceExtensions
    {
        private static Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions EnableBearingAuthentication(this Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
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

        private static IServiceCollection AddSwaggerODataWorkaround(this IServiceCollection services)
        {
            // Required to get Swagger to work with OData Controllers
            // System.InvalidOperationException: No media types found in 'Microsoft.AspNet.OData.Formatter.ODataInputFormatter.SupportedMediaTypes'. 
            // Add at least one media type to the list of supported media types.
            // Workaround: 
            // https://github.com/OData/WebApi/issues/1177
            // https://github.com/OData/WebApi/issues/2024
            // https://stackoverflow.com/questions/62404125/how-to-add-swagger-in-odata-enabled-web-api-running-on-asp-net-core-3-1
            services.AddControllers(options =>
            {
                IEnumerable<ODataOutputFormatter> outputFormatters =
                    options.OutputFormatters.OfType<ODataOutputFormatter>()
                        .Where(formatter => !formatter.SupportedMediaTypes.Any());

                foreach (var outputFormatter in outputFormatters)
                {
                    outputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/odata"));
                }

                IEnumerable<ODataInputFormatter> inputFormatters =
                    options.InputFormatters.OfType<ODataInputFormatter>()
                        .Where(formatter => !formatter.SupportedMediaTypes.Any());

                foreach (var inputFormatter in inputFormatters)
                {
                    inputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/odata"));
                }
            });

            return services;
        }



        // https://github.com/StefanescuEduard/DotnetSwaggerDocumentation/blob/master/API.WithAuthentication/Extensions/SwaggerDocumentationExtensions.cs
        private static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services, IReadOnlyList<ApiVersionDescription> apiVersionDescriptions)
        {
            services.AddSwaggerODataWorkaround();

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(options =>
            {
                // build a swagger endpoint for each API version
                foreach (var description in apiVersionDescriptions)
                {
                    options.SwaggerDoc(
                    description.GroupName,
                    new OpenApiInfo()
                    {
                        Title = $"Main API {description.ApiVersion}",
                        Version = description.ApiVersion.ToString()
                    });
                }

                options.EnableAnnotations();

                // add a custom operation filter which sets default values
                options.OperationFilter<SwaggerDefaultValues>();

                // add odate fields to swagger for the odata endpoints using [EnableQuery]
                options.OperationFilter<SwaggerEnableQueryFilter>();

                // integrate xml comments
                string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);

                options.EnableBearingAuthentication();
            });

            services.AddSwaggerGenNewtonsoftSupport(); // explicit opt-in - needs to be placed after AddSwaggerGen()

            return services;
        }

        /// <summary>
        /// OData v7.x (v8 has breaking changes)
        /// OData, AddControllers, AddNewtonsoftJson
        /// https://devblogs.microsoft.com/odata/enabling-endpoint-routing-in-odata/
        /// </summary>
        public static IServiceCollection AddODataSwaggerDocumentation(this IServiceCollection services, bool camelCase = true)
        {
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                if (camelCase)
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                }
                else
                {
                    // By default Model properties are converted to camelCase, below will change it back to PascalCase
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                }
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });
            services.AddOData();

            // this will generate Main API v1
            services.AddSwaggerDocumentation(
                new List<ApiVersionDescription>() {
                    new ApiVersionDescription(new ApiVersion(1, 0), "v1", false)
                }
            );

            return services;
        }

        /// <summary>
        /// OData v7.x (v8 has breaking changes)
        /// ApiVersioning, OData, AddControllers, AddNewtonsoftJson
        /// https://devblogs.microsoft.com/odata/enabling-endpoint-routing-in-odata/
        /// </summary>
        public static IServiceCollection AddODataVersioningSwaggerDocumentation(this IServiceCollection services, bool camelCase = true)
        {
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                if (camelCase)
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                }
                else
                {
                    // By default Model properties are converted to camelCase, below will change it back to PascalCase
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                }
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

            // Add API versioning to application. https://github.com/microsoft/aspnet-api-versioning/wiki/New-Services-Quick-Start#aspnet-core-with-odata-v40
            // allow a client to call you without specifying an api version
            // since we haven't configured it otherwise, the assumed api version will be 1.0
            services.AddApiVersioning(
                options =>
                {
                    // options.ApiVersionReader = new UrlSegmentApiVersionReader();

                    // Reporting api versions will return the headers "api-supported-versions" and "api-deprecated-versions"
                    options.ReportApiVersions = true;
                    options.AssumeDefaultVersionWhenUnspecified = true;

                    // https://github.com/microsoft/aspnet-api-versioning/tree/master/samples/aspnetcore/ByNamespaceSample
                    // automatically applies an api version based on the name of the defining controller's namespace
                    // 'v' | 'V' : [<year> '-' <month> '-' <day>] : [<major[.minor]>] : [<status>]
                    // ex: v2018_04_01_1_1_Beta
                    options.Conventions.Add(new VersionByNamespaceConvention());
                });

            services.AddVersionedApiExplorer(options =>
            {
                // Add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                // NOTE: the specified format code will format the version as "'v'major[.minor][-status]"
                options.GroupNameFormat = "'v'VVV";

                // NOTE: this option is only necessary when versioning by url segment. the SubstitutionFormat
                // can also be used to control the format of the API version in route templates
                options.SubstituteApiVersionInUrl = true;
            });

            // [EnableQuery] is needed for Actions
            // services.AddOData().EnableApiVersioning();
            services.AddOData().EnableApiVersioning(options =>
            {
                // https://github.com/microsoft/aspnet-api-versioning/tree/master/samples/aspnetcore/ByNamespaceSample
                // automatically applies an api version based on the name of the defining controller's namespace
                // 'v' | 'V' : [<year> '-' <month> '-' <day>] : [<major[.minor]>] : [<status>]
                // ex: v2018_04_01_1_1_Beta
                options.Conventions.Add(new VersionByNamespaceConvention());
            });

            services.AddODataApiExplorer(options =>
            {
                // Add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                // NOTE: the specified format code will format the version as "'v'major[.minor][-status]"
                options.GroupNameFormat = "'v'VVV";

                // NOTE: this option is only necessary when versioning by url segment. the SubstitutionFormat
                // can also be used to control the format of the API version in route templates
                options.SubstituteApiVersionInUrl = true;

                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
            });

            // get the api versions
            // 1. when using Asp.Versioning.OData
            //    use app.DescribeApiVersions()
            // 2. when using Microsoft.AspNetCore.OData.Versioning
            //    get the IApiVersionDescriptionProvider and call the ApiVersionDescriptions
            var apiVersionDescriptionProvider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();
            var descriptions = apiVersionDescriptionProvider.ApiVersionDescriptions;
            services.AddSwaggerDocumentation(descriptions);

            return services;
        }

        public static IApplicationBuilder UseODataSwaggerDocumentation(this IApplicationBuilder app)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            // app.UseSwagger();
            app.UseSwagger(options =>
            {
                // By default, Swashbuckle generates and exposes Swagger JSON in version 3.0 of the specification—officially called the OpenAPI Specification
                // options.SerializeAsV2 = true;

                options.PreSerializeFilters.Add((swagger, httpReq) =>
                {
                    swagger.Servers = new List<OpenApiServer>
                    {
                            new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" }
                    };
                });
            });

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(options =>
            {
                options.DefaultModelsExpandDepth(-1); // hides schemas dropdown
                options.EnableTryItOutByDefault();

                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Main API V1");
            });

            return app;
        }

        public static IApplicationBuilder UseODataVersioningSwaggerDocumentation(this IApplicationBuilder app, IApiVersionDescriptionProvider provider)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            // app.UseSwagger();
            app.UseSwagger(options =>
            {
                // By default, Swashbuckle generates and exposes Swagger JSON in version 3.0 of the specification—officially called the OpenAPI Specification
                // options.SerializeAsV2 = true;

                options.PreSerializeFilters.Add((swagger, httpReq) =>
                {
                    swagger.Servers = new List<OpenApiServer>
                    {
                            new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" }
                    };
                });
            });

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(options =>
            {
                options.DefaultModelsExpandDepth(-1); // hides schemas dropdown
                options.EnableTryItOutByDefault();

                // when using Asp.Versioning.OData
                // use app.DescribeApiVersions()
                // when using Microsoft.AspNetCore.OData.Versioning
                // get the IApiVersionDescriptionProvider and call the ApiVersionDescriptions                    
                // var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
                var descriptions = provider.ApiVersionDescriptions;

                // build a swagger endpoint for each discovered API version
                foreach (var description in descriptions)
                {
                    var url = $"/swagger/{description.GroupName}/swagger.json";
                    var name = description.GroupName.ToUpperInvariant();
                    options.SwaggerEndpoint(url, name);
                }
            });

            return app;
        }
    }
}
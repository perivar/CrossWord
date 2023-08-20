using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.NewtonsoftJson;
using Microsoft.OData.ModelBuilder;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TodoApi.Models;

namespace TodoApi
{
    public class Startup
    {
        readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var modelBuilder = new ODataConventionModelBuilder();
            modelBuilder.EnableLowerCamelCase(); // turn on "lower camel case" for the whole model 
            modelBuilder.EntityType<Order>();
            modelBuilder.EntitySet<Customer>("Customers");

            // Add services to the container.
            services.AddAuthorization();

            services.AddControllers()
            .AddNewtonsoftJson(options =>
                {
                    // options.UseCamelCasing(false);
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    // options.SerializerSettings.Culture = CultureInfo.InvariantCulture; // Specify culture for parsing and formatting
                    // options.SerializerSettings.Formatting = Formatting.None; // You can set it to Formatting.Indented if you want indented output
                    // options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore; // Ignore null values
                    // options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                    // options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    // options.SerializerSettings.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore; // Handle circular references
                })
            .AddOData(
                options =>
                {
                    options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(300);
                    options.AddRouteComponents("odata", modelBuilder.GetEdmModel());
                })
            .AddODataNewtonsoftJson();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                // Add support for OData-like REST endpoint with [EnableQuery]
                c.OperationFilter<ODataOperationFilter>();
            });
            services.AddCors(option =>
            {
                option.AddPolicy("Everything",
                    builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
                    );
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Configure the HTTP request pipeline.
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Use odata route debug, /$odata
            app.UseODataRouteDebug();

            app.UseSwagger();
            app.UseSwaggerUI();

            // app.UseHttpsRedirection();

            app.UseCors("Everything");

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            /*
                ASP.NET Core provides the Microsoft.AspNetCore.OpenApi package
                to interact with OpenAPI specifications for endpoints. 
                The package acts as a link between the OpenAPI models that are defined 
                in the Microsoft.AspNetCore.OpenApi package and the 
                endpoints that are defined in Minimal APIs. 
                The package provides an API that examines an endpoint's parameters, 
                responses, and metadata to construct an OpenAPI annotation 
                type that is used to describe an endpoint. 
                Calling .WithOpenApi() on the endpoint adds to the endpoint's metadata. T 
                https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/openapi?view=aspnetcore-7.0          
            */
        }
    }
}

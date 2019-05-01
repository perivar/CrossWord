using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Swashbuckle.AspNetCore.Swagger;
using CrossWord.Scraper.MySQLDbService;
using Serilog;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using CrossWord.Scraper.MySQLDbService.Models;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.Net.Http.Headers;

namespace CrossWord.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

            // output Config parameters to try to find out why it doesn't work in Docker
            foreach (var config in Configuration.AsEnumerable())
            {
                Log.Information("{0}", config);
            }

            // had to add this to get the error on startup away (since the _LoginPartial.cshtml is using SignInManager)
            // todo: should probably just remove all that stuff from _LoginPartial.cshtml
            services.AddScoped<SignInManager<IdentityUser>, SignInManager<IdentityUser>>();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.None;
            });

            // start DOCKER on port 3360
            // docker run -p 3360:3306 --name mysqldb -e MYSQL_ROOT_PASSWORD=secret -d mysql:8.0.15

            // Build database connection string
            var dbhost = Configuration["DBHOST"] ?? "localhost";
            var dbport = Configuration["DBPORT"] ?? "3306";
            var dbuser = Configuration["DBUSER"] ?? "user";
            var dbpassword = Configuration["DBPASSWORD"] ?? "password";
            var database = Configuration["DATABASE"] ?? "dictionary";

            services.AddDbContext<WordHintDbContext>(options =>
            {
                options.UseMySql($"server={dbhost}; user={dbuser}; pwd={dbpassword}; "
                    + $"port={dbport}; database={database}; charset=utf8;");
            });

            // Singleton objects are the same for every object and every request.
            // for this to be able to use the database
            // add a constructor that takes (ILoggerFactory logger, IServiceScopeFactory scopeFactory)
            // and instantiate the database whenever needed
            // using (var scope = scopeFactory.CreateScope())
            // {
            // var db = scope.ServiceProvider.GetRequiredService<WordHintDbContext>();
            //  }
            services.AddSingleton<ICrossDictionary, DatabaseDictionary>();

            // You cannot use AddDefaultIdentity, since internally, this calls AddDefaultUI, which contains the Razor Pages "endpoints" you don't want. 
            // You'll need to use AddIdentity<TUser, TRole> or AddIdentityCore<TUser> instead.
            // https://github.com/aspnet/Identity/blob/master/src/UI/IdentityServiceCollectionUIExtensions.cs#L47
            services.AddCustomDefaultIdentity<IdentityUser>
            (
                o =>
                {
                    // password policy options e.g 
                    // o.Password.RequireDigit = true;
                    // see defaults here:
                    // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-2.2
                }
            )
            .AddEntityFrameworkStores<WordHintDbContext>();

            // ===== Add Jwt Authentication ========
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); // => remove default claims
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

                })
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = Configuration["Jwt:Issuer"],
                        ValidAudience = Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"])),
                        ClockSkew = TimeSpan.Zero // remove delay of token when expire
                    };
                });

            // note: Endpoint Routing is enabled by default; however, it is unsupported by OData and MUST be false
            services.AddMvc(options =>
                {
                    options.EnableEndpointRouting = false; // TODO: Remove when OData does not causes exceptions anymore
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(opt =>
                {
                    opt.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    // opt.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    // opt.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                    // opt.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    // opt.SerializerSettings.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
                    opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                })
                ;

            // Register the Swagger generator and enable OData
            services.AddODataSwaggerDocumentation();

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

            services.AddCors(o =>
            {
                o.AddPolicy("Everything", p =>
                {
                    p.AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowAnyOrigin();
                });
            });

            // add Queued background tasks
            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

            // and a timed background task
            // services.AddHostedService<TimedHostedService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, WordHintDbContext db,
                            VersionedODataModelBuilder modelBuilder,
                            IApiVersionDescriptionProvider provider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseRequestResponseLogging();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // app.UseHttpsRedirection(); // disable to use within docker behind a proxy
            app.UseStaticFiles();
            app.UseCookiePolicy();

            // You would either call EnsureCreated() or Migrate(). 
            // EnsureCreated() is an alternative that completely skips the migrations pipeline and just creates a database that matches you current model. 
            // It's good for unit testing or very early prototyping, when you are happy just to delete and re-create the database when the model changes.
            // db.Database.EnsureDeleted();
            // db.Database.EnsureCreated();

            // Note! Therefore don't use EnsureDeleted() and EnsureCreated() but Migrate();
            db.Database.Migrate();

            app.UseAuthentication();

            app.UseCors("Everything");

            // Add support for OData to MVC pipeline
            var models = modelBuilder.GetEdmModels();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                // version with query parameter
                routes.MapVersionedODataRoutes(
                    routeName: "odata",
                    routePrefix: "odata",
                    models: models);

                // version by path
                // routes.MapVersionedODataRoutes(
                //     routeName: "odata-bypath",
                //     routePrefix: "odata/v{version:apiVersion}",
                //     models: models);

                // Workaround: https://github.com/OData/WebApi/issues/1175
                // routes.EnableDependencyInjection();
            });

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            app.UseODataSwaggerDocumentation(modelBuilder, provider);
        }
    }
}

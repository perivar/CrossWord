using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Serilog;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CrossWord.Scraper.MySQLDbService;
using CrossWord.Scraper.MySQLDbService.Entities;
using CrossWord.API.Services;
using CrossWord.API.Hubs;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using CrossWord.Scraper.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using CrossWord.API.Configuration;
using Microsoft.OData.ModelBuilder;
using Microsoft.AspNetCore.OData;
using System.Text.Json;

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
            // output Config parameters to debug in Docker
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

            foreach (var config in Configuration.AsEnumerable())
            {
                Log.Information("{0}", config);
            }

            // configure the edm model for odata
            var model = WordModelConfiguration.GetEdmModel(new ODataConventionModelBuilder());

            services.AddProblemDetails();

            // had to add this to get the error on startup away (since the _LoginPartial.cshtml is using SignInManager)
            // todo: should probably just remove all that stuff from _LoginPartial.cshtml
            services.AddScoped<SignInManager<ApplicationUser>, SignInManager<ApplicationUser>>();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // add the token service
            services.AddTransient<ITokenService, TokenService>();

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // either use a local mysql install running on port 3306 (default)
            // or use Docker:
            // spinning up directly on port 3360
            // docker run -p 3360:3306 --name mysqldb -e MYSQL_ROOT_PASSWORD=password -d mysql:8.0.15            
            // or use docker compose to init with data on port 3360
            // docker compose up db -d
            // and enter using
            // docker exec -it crosswordapi.db mysql -uroot -psecret dictionary

            // Build database connection string
            var dbhost = Configuration["DBHOST"] ?? "localhost";
            var dbport = Configuration["DBPORT"] ?? "3306";
            var dbuser = Configuration["DBUSER"] ?? "user";
            var dbpassword = Configuration["DBPASSWORD"] ?? "password";
            var database = Configuration["DATABASE"] ?? "dictionary";

            services.AddDbContext<WordHintDbContext>(options =>
            {
                string connectionString = $"server={dbhost}; user={dbuser}; pwd={dbpassword}; "
                    + $"port={dbport}; database={database}; charset=utf8;";
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            });

            services.AddDatabaseDeveloperPageExceptionFilter();

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
            services.AddCustomDefaultIdentity<ApplicationUser>
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
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(configureOptions =>
                {
                    // Gets or sets if HTTPS is required for the metadata address or authority. 
                    // The default is true. This should be disabled only in development environments.
                    // configureOptions.RequireHttpsMetadata = false;

                    // Defines whether the bearer token should be stored in the AuthenticationProperties after a successful authorization.
                    // Used to indicate whether the server must save token server side to validate them. 
                    // So even when a user have a properly signed and encrypted it ll not pass token validation if it is not generated by the server. 
                    // This a security reinforcement so even when signing key is compromised your application is not.
                    configureOptions.SaveToken = true;

                    configureOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = Configuration["Jwt:Issuer"],

                        ValidateAudience = true,
                        ValidAudience = Configuration["Jwt:Audience"],

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"])),

                        RequireExpirationTime = true, // indicating whether tokens must have an 'expiration' value.
                        ValidateLifetime = true, // here we are saying that we care about the token's expiration date

                        // Allow for some drift in server time
                        // (a lower value is better; we recommend two minutes or less)
                        ClockSkew = TimeSpan.Zero // remove delay of token when expire, the default for this setting is 5 minutes
                    };

                    configureOptions.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var exceptionType = context.Exception.GetType();
                            if (exceptionType == typeof(SecurityTokenExpiredException)
                            || exceptionType == typeof(SecurityTokenInvalidLifetimeException))
                            {
                                context.Response.Headers.Add("Token-Expired", "true");
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            // add auto mapper
            services.AddAutoMapper(typeof(Startup), typeof(AutoMapperProfile));

            services.AddControllers()
            .AddJsonOptions(options =>
                {
                    // turn on camel case for non EDM controllers, see EnableLowerCamelCase() for EDM models
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    // options.JsonSerializerOptions.WriteIndented = true; // disable indented to save bandwidth and align with odata defaults
                })
            .AddOData(
                options =>
                {
                    options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(300);
                    options.AddRouteComponents("odata", model);
                });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                // Add support for OData-like REST endpoint with [EnableQuery]
                c.OperationFilter<ODataOperationFilter>();

                c.EnableBearingAuthentication();
            });

            services.AddCors(o =>
            {
                o.AddPolicy("Everything",

                    // To avoid the following error - use SetIsOriginAllowed(_ => true)
                    // Access to XMLHttpRequest at 'https://api.nerseth.com/crosswordsignalrhub/negotiate' from origin 'https://crossword.nerseth.com' 
                    // has been blocked by CORS policy: Response to preflight request doesn't pass access control check: 
                    // The value of the 'Access-Control-Allow-Origin' header in the response must not be the wildcard '*' 
                    // when the request's credentials mode is 'include'. 

                    // The credentials mode of requests initiated by the XMLHttpRequest is controlled by the withCredentials attribute.
                    // When using "AllowCredentials()" we cannot use AllowAnyOrigin()
                    // instead the SetIsOriginAllowed(_ => true) is required.
                    builder => builder
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        // .AllowAnyOrigin()
                        .SetIsOriginAllowed(_ => true)
                        .AllowCredentials()
                        .WithExposedHeaders("WWW-Authenticate", "Token-Expired", "Refresh-Token-Expired", "Invalid-Token", "Invalid-Refresh-Token")
                    );
            });

            // add Queued background tasks
            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

            // and a timed background task
            // services.AddHostedService<TimedHostedService>();

            // https://blog.mindgaze.tech/2019/04/09/properly-configure-forwarded-headers-in-asp-net-core/            
            // Use the following in docker-compose to set this parameter.
            //  environment:
            //   - KNOWNPROXIES='10.0.0.1, 10.0.0.2'
            var proxies = Configuration.GetArrayValues("KNOWNPROXIES", "");

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                foreach (var proxy in proxies)
                {
                    options.KnownProxies.Add(IPAddress.Parse(proxy));
                }
            });

            // Enable SignalR
            services.AddSignalR();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        // https://github.com/rgudkov-uss/aspnetcore-net6-odata-swagger-versioned/blob/main/WebApiTest6/Program.cs
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, WordHintDbContext db)
        {
            // Configure the HTTP request pipeline.
            app.UseForwardedHeaders();

            // if using WebApplication app, use app.Environment.IsDevelopment()
            if (env.IsDevelopment())
            {
                app.UseRequestResponseLogging();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Use odata route debug, /$odata
            app.UseODataRouteDebug();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.DefaultModelsExpandDepth(-1); // hides schemas dropdown
                options.EnableTryItOutByDefault();
            });

            // app.UseHttpsRedirection(); // disable to use within docker behind a proxy

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            // You would either call EnsureCreated() or Migrate(). 
            // EnsureCreated() is an alternative that completely skips the migrations pipeline and just creates a database that matches you current model. 
            // It's good for unit testing or very early prototyping, when you are happy just to delete and re-create the database when the model changes.
            // db.Database.EnsureDeleted();
            // db.Database.EnsureCreated();

            // Note! Therefore don't use EnsureDeleted() and EnsureCreated() but Migrate();
            db.Database.Migrate();

            app.UseCors("Everything");

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // add signalr hub url
                endpoints.MapHub<CrossWordSignalRHub>("/crosswordsignalrhub");

                // this automatically maps all the controllers with [ApiController] and [Route] attributes
                endpoints.MapControllers();
            });
        }
    }
}

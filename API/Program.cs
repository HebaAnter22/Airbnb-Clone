using System.Security.Claims;
using System.Text;
using API.Data;
using API.Middleware;
using API.Services;
using API.Services.AmenityRepo;
using API.Services.BookingRepo;
using API.Services.AIRepo;
using API.Services.PromotionRepo;
using API.Services.PropertyAvailabilityRepo;
using API.Services.PropertyCategoryRepo;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.FileProviders;
using API.Services.AdminRepo;
using API.Services.HostVerificationRepo;
using Stripe;
using API.Hubs;
using API.Services.BookingPaymentRepo;
using API.Services.Payoutrepo;
using API.Services.NotificationRepository;
using System.Text.Json.Serialization;
using API.Services.NotificationRepository;

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var stripeSettings = builder.Configuration.GetSection("Stripe");
            StripeConfiguration.ApiKey = stripeSettings["SecretKey"];

            builder.Services.AddSignalR();
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true; // Helps with debugging
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            }).AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

            // Configure to allow reading the raw request body for Stripe webhooks
            builder.Services.AddControllers(options =>
            {
                options.AllowEmptyInputInBodyModelBinding = true;
            });

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Property API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
                });

            builder.Services.AddDALService(builder.Configuration);
            builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });


            builder.Services.AddCors(options => {
                options.AddPolicy("AllowAll", policy => {
                    policy.WithOrigins("http://localhost:4200")
                          .AllowAnyMethod()
                          .AllowAnyHeader().
                          AllowCredentials();
                });
            });


            builder.Services.AddAuthentication(options =>
            {
                // For API auth
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                // For external providers
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
                    ValidateIssuerSigningKey = true,
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/chatHub"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });




            // Update the Google authentication configuration
            builder.Services.AddAuthentication()
                .AddGoogle(GoogleDefaults.AuthenticationScheme, opt =>
                {
                    var googleAuth = builder.Configuration.GetSection("Authentication:Google");
                    opt.ClientId = googleAuth["ClientId"];
                    opt.ClientSecret = googleAuth["ClientSecret"];
                    opt.CallbackPath = "/api/auth/google-callback";
                    opt.SaveTokens = true;

                    // Map Google's response to our claims
                    opt.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
                    opt.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                    opt.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
                });


            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IAmenityService, AmenityService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();
            builder.Services.AddScoped<IPropertyService, PropertyService>();
            builder.Services.AddScoped<IPropertyCategoryService, PropertyCategoryService>();
            builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
            builder.Services.AddScoped<IHostVerificationRepository, HostVerificationRepository>();

            builder.Services.AddScoped<IBookingRepository>(sp => 
                new BookingRepository(
                    sp.GetRequiredService<AppDbContext>(),
                    sp.GetRequiredService<IPropertyAvailabilityRepository>(),
                    sp.GetRequiredService<IPropertyService>(),
                    sp.GetRequiredService<IPromotionRepository>(),
                    sp
                )
            );
            builder.Services.AddScoped<IBookingPaymentRepository, BookingPaymentRepository>();
            builder.Services.AddScoped<IPayoutService, Services.Payoutrepo.PayoutService>();
            builder.Services.AddScoped<IPropertyAvailabilityRepository, PropertyAvailabilityRepository>();
            builder.Services.AddScoped<IAdminRepository, AdminRepository>();
            builder.Services.AddScoped<IHostVerificationRepository, HostVerificationRepository>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();


            // Add AI Services
            builder.Services.Configure<AIConfiguration>(builder.Configuration.GetSection("OpenAI"));
            builder.Services.AddScoped<IOpenAIService, OpenAIService>();
            builder.Services.AddAutoMapper(typeof(Program));

            // Register services
            builder.Services.AddScoped<IViolationService, ViolationService>();

            var app = builder.Build();
            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}

            //app.UseExceptionMiddleware();

            app.MapHub<ChatHub>("/chatHub");

            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCors("AllowAll");
            app.UseHttpsRedirection();

            // Configure to read raw request body for Stripe webhooks
            app.Use(async (context, next) =>
            {
                context.Request.EnableBuffering();
                await next();
            });

            // Ensure the uploads directory exists
            var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsPath);

            // Configure static files
            app.UseStaticFiles(); // Serve files from wwwroot

            // No need for additional static files provider since files are in wwwroot
            // The default provider will handle all files under wwwroot

            app.UseAuthentication();
            app.UseStaticFiles();
            app.UseWebSockets();

            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
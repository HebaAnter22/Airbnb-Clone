using System.Security.Claims;
using System.Text;
using API.Data;
using API.Middleware;
using API.Models;
using API.Services;
using API.Services.AdminRepo;
using API.Services.AmenityRepo;
using API.Services.BookingPaymentRepo;
using API.Services.BookingRepo;
using API.Services.HostVerificationRepo;
using API.Services.NotificationRepository;
using API.Services.PromotionRepo;
using API.Services.PropertyAvailabilityRepo;
using API.Services.PropertyCategoryRepo;
using API.Services.ReviewRepo;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Stripe;

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            // Load Stripe configuration
            var stripeSettings = builder.Configuration.GetSection("Stripe");
            StripeConfiguration.ApiKey = stripeSettings["SecretKey"];

            builder.Services.AddControllers();

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

         

            builder.Services.AddCors(options => {
                options.AddPolicy("AllowAll", policy => {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
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
            builder.Services.AddScoped<IPropertyService, PropertyService>();
            builder.Services.AddScoped<IBookingRepository, BookingRepository>();
            builder.Services.AddScoped<IPropertyAvailabilityRepository, PropertyAvailabilityRepository>();
            builder.Services.AddScoped<IAmenityService, AmenityService>();
            builder.Services.AddScoped<IPropertyCategoryService, PropertyCategoryService>();
            builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
            builder.Services.AddScoped<IAdminRepository, AdminRepository>();
            builder.Services.AddScoped<IHostVerificationRepository, HostVerificationRepository>();
            builder.Services.AddScoped<IBookingPaymentRepository, BookingPaymentRepository>();
            builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();



            builder.Services.AddAutoMapper(typeof(Program));





            var app = builder.Build();
            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}
            
            //app.UseExceptionMiddleware();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCors("AllowAll");
            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
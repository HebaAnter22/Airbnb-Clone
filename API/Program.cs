using Microsoft.OpenApi.Models;

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //// Add services to the container
            builder.Services.AddControllers();

            //// Configure Swagger (Swashbuckle)
            builder.Services.AddSwaggerGen();

            builder.Services.AddDALService(builder.Configuration);


            //// Configure CORS
            builder.Services.AddCors(options => {
                options.AddPolicy("AllowAll", policy => {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();


            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseCors("AllowAll"); 

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();



        }
    }
}
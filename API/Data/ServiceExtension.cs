using API.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API
{
    public static class ServiceExtension
    {
        public static void AddDALService(this IServiceCollection service, IConfiguration config)
        {
            var connectionString = config.GetConnectionString("cs");
            service.AddDbContext<AppDbContext>(option => option.UseSqlServer(connectionString));

        }
    }
}


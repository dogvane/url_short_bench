using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace v2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var builder = WebApplication.CreateBuilder(args);

                // 配置 Kestrel 服务器以处理高并发
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.Limits.MaxConcurrentConnections = 1000;
                    options.Limits.MaxConcurrentUpgradedConnections = 1000;
                    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
                    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
                });

                // 添加控制器服务
                builder.Services.AddControllers();

                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                builder.Services.AddSingleton(new DbRepository(connectionString));

                var app = builder.Build();

                app.Urls.Add("http://*:10086");

                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL ERROR: Failed to start service: {ex.Message}");
                throw;
            }
        }
    }
}

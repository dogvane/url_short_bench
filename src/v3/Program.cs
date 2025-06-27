using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

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

                // 配置数据库
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                builder.Services.AddSingleton(new DbRepository(connectionString));

                // 配置Redis
                var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString");
                if (!string.IsNullOrEmpty(redisConnectionString))
                {
                    try
                    {
                        var redis = ConnectionMultiplexer.Connect(redisConnectionString);
                        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
                        builder.Services.AddSingleton<CacheService>();
                        Console.WriteLine($"Redis connected successfully: {redisConnectionString}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"WARNING: Failed to connect to Redis ({redisConnectionString}): {ex.Message}");
                        Console.WriteLine("Application will continue without Redis caching.");
                        // 注册一个空的缓存服务
                        builder.Services.AddSingleton<CacheService>(provider => null);
                    }
                }
                else
                {
                    Console.WriteLine("WARNING: Redis connection string not found. Application will run without caching.");
                    builder.Services.AddSingleton<CacheService>(provider => null);
                }

                var app = builder.Build();

                app.Urls.Add("http://*:8080");

                app.MapControllers();

                Console.WriteLine("URL Short Service started successfully");
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

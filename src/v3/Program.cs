using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Prometheus;

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
                    options.Limits.MaxConcurrentConnections = 2000;
                    options.Limits.MaxConcurrentUpgradedConnections = 2000;
                    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
                    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
                    options.Limits.MaxRequestBodySize = 1024 * 1024; // 1MB
                    options.Limits.MinRequestBodyDataRate = null; // 禁用最小数据速率限制
                    options.Limits.MinResponseDataRate = null; // 禁用最小数据速率限制
                    
                    // 添加更多的并发处理配置
                    options.Limits.MaxRequestHeaderCount = 100;
                    options.Limits.MaxRequestHeadersTotalSize = 32768; // 32KB
                    options.Limits.MaxRequestLineSize = 8192; // 8KB
                    options.Limits.MaxRequestBufferSize = 1048576; // 1MB
                    options.Limits.MaxResponseBufferSize = 1048576; // 1MB
                    
                    // 启用HTTP/2支持
                    options.ConfigureEndpointDefaults(listenOptions =>
                    {
                        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                    });
                });

                // 添加控制器服务
                builder.Services.AddControllers(options =>
                {
                    // 配置更宽松的模型绑定
                    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(x => "Invalid value provided");
                    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(x => "Value must be a number");
                    options.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(x => "Required value is missing");
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    // 自定义模型验证错误响应
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var errors = context.ModelState
                            .Where(x => x.Value?.Errors.Count > 0)
                            .Select(x => new { Field = x.Key, Errors = x.Value?.Errors.Select(e => e.ErrorMessage) })
                            .ToArray();
                        
                        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
                        {
                            Message = "Validation failed",
                            Errors = errors,
                            Timestamp = DateTime.UtcNow
                        });
                    };
                });

                // 添加监控服务
                builder.Services.AddSingleton<MonitoringService>();

                // 配置数据库（支持雪花算法参数注入）
                var snowflakeSection = builder.Configuration.GetSection("Snowflake");
                int workerId = snowflakeSection.GetValue<int>("WorkerId", 1);
                int datacenterId = snowflakeSection.GetValue<int>("DatacenterId", 1);
                Console.WriteLine($"Snowflake ID Generator configured: WorkerId={workerId}, DatacenterId={datacenterId}");
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
                    throw new InvalidOperationException("DefaultConnection string is required");
                builder.Services.AddSingleton(new DbRepository(connectionString, workerId, datacenterId));

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
                        builder.Services.AddSingleton<CacheService>(provider => null!);
                    }
                }
                else
                {
                    Console.WriteLine("WARNING: Redis connection string not found. Application will run without caching.");
                    builder.Services.AddSingleton<CacheService>(provider => null!);
                }

                var app = builder.Build();

                app.Urls.Add("http://*:8080");

                // 添加全局异常处理中间件（需要在其他中间件之前）
                app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

                // 启用 Prometheus 指标
                app.UseMetricServer();
                app.UseHttpMetrics();

                // 添加自定义监控中间件
                app.UseMiddleware<MonitoringMiddleware>();

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

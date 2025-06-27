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
            var builder = WebApplication.CreateBuilder(args);

            // 添加控制器服务
            builder.Services.AddControllers();

            // 配置短链接选项
            builder.Services.Configure<ShortUrlOptions>(options =>
            {
                // 从配置文件读取连接字符串
                options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                    ?? "Data Source=shortlinks.db";

                // 从配置文件读取仓储类型，默认为原始实现
                var repositoryType = builder.Configuration.GetValue<string>("ShortUrl:RepositoryType");
                Console.WriteLine($"当前仓储类型: {repositoryType}");
                options.RepositoryType = repositoryType?.ToLower() switch
                {
                    "autoincrement" => ShortUrlRepositoryType.AutoIncrement,
                    "original" => ShortUrlRepositoryType.Original,
                    _ => ShortUrlRepositoryType.Original
                };

                // 从配置文件读取是否启用性能监控
                options.EnablePerformanceLogging = builder.Configuration.GetValue<bool>("ShortUrl:EnablePerformanceLogging");
            });

            // 注册工厂和仓储服务
            builder.Services.AddSingleton<ShortUrlRepositoryFactory>();
            builder.Services.AddSingleton<IShortUrlRepository>(serviceProvider =>
            {
                var factory = serviceProvider.GetRequiredService<ShortUrlRepositoryFactory>();
                return factory.CreateRepository();
            });

            var app = builder.Build();

            app.Urls.Add("http://*:10086");

            app.MapControllers();

            // 输出当前配置信息
            var factory = app.Services.GetRequiredService<ShortUrlRepositoryFactory>();
            Console.WriteLine($"\n当前配置: {factory.GetConfigurationDescription()}\n");

            app.Run();
        }
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace v1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 添加控制器服务
            builder.Services.AddControllers();

            var app = builder.Build();

            app.Urls.Add("http://*:10086");

            app.MapControllers();

            app.Run();
        }
    }
}

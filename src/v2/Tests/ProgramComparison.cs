using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace v2
{
    class ComparisonTest
    {
        static async Task TestMain(string[] args)
        {
            var connectionString = "Data Source=shortlinks.db";
            
            Console.WriteLine("=== 短链接生成方法对比测试 ===\n");
            
            // 使用工厂模式创建不同的实现
            Console.WriteLine("1. 测试原始方法 (通过工厂创建):");
            var originalOptions = new ShortUrlOptions 
            { 
                ConnectionString = connectionString, 
                RepositoryType = ShortUrlRepositoryType.Original 
            };
            var originalFactory = new ShortUrlRepositoryFactory(originalOptions);
            var originalRepo = originalFactory.CreateRepository();
            await TestCreateMethod("原始方法", () => originalRepo.CreateShortLink("https://example.com/test1", null));
            
            Console.WriteLine("\n" + new string('=', 50) + "\n");
            
            // 测试自增ID方法
            Console.WriteLine("2. 测试自增ID方法 (通过工厂创建):");
            var autoIncrementOptions = new ShortUrlOptions 
            { 
                ConnectionString = connectionString, 
                RepositoryType = ShortUrlRepositoryType.AutoIncrement 
            };
            var autoIncrementFactory = new ShortUrlRepositoryFactory(autoIncrementOptions);
            var autoIncrementRepo = autoIncrementFactory.CreateRepository();
            await TestCreateMethod("自增ID方法", () => autoIncrementRepo.CreateShortLink("https://example.com/test2", null));
            
            Console.WriteLine("\n=== 测试完成 ===");
            
            // 显示当前状态
            Console.WriteLine($"\n原始方法当前最大ID: {originalRepo.GetCurrentMaxId()}");
            Console.WriteLine($"自增ID方法当前最大ID: {autoIncrementRepo.GetCurrentMaxId()}");
            
            Console.WriteLine("\n=== 配置描述 ===");
            Console.WriteLine($"原始方法: {originalFactory.GetConfigurationDescription()}");
            Console.WriteLine($"自增ID方法: {autoIncrementFactory.GetConfigurationDescription()}");
        }
        
        static Task TestCreateMethod(string methodName, Func<(long id, string alias)> createMethod)
        {
            const int testCount = 10;
            var stopwatch = new Stopwatch();
            
            Console.WriteLine($"{methodName} - 创建 {testCount} 个短链接:");
            stopwatch.Start();
            
            for (int i = 0; i < testCount; i++)
            {
                try
                {
                    var result = createMethod();
                    Console.WriteLine($"  第{i + 1}个: ID={result.id}, Alias={result.alias}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  第{i + 1}个创建失败: {ex.Message}");
                }
            }
            
            stopwatch.Stop();
            Console.WriteLine($"\n{methodName} 总耗时: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"平均耗时: {stopwatch.ElapsedMilliseconds / (double)testCount:F2}ms/个");
            
            return Task.CompletedTask;
        }
    }
}

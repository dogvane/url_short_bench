using BenchmarkDotNet.Running;

namespace PerformanceTests
{
    /// <summary>
    /// 性能测试程序入口
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("DbRepository 性能测试工具");
            Console.WriteLine("========================");
            Console.WriteLine();

            if (args.Length > 0 && args[0] == "--help")
            {
                ShowHelp();
                return;
            }

            // 根据参数选择运行的基准测试
            BenchmarkDotNet.Reports.Summary summary;
            
            if (args.Length > 0)
            {
                summary = args[0] switch
                {
                    "quick" or "Quick" => BenchmarkRunner.Run<QuickDbRepositoryBenchmark>(),
                    "simple" or "Simple" => BenchmarkRunner.Run<SimpleCreateShortLinkBenchmark>(),
                    _ => BenchmarkRunner.Run<DbRepositoryBenchmark>()
                };
            }
            else
            {
                summary = BenchmarkRunner.Run<DbRepositoryBenchmark>();
            }
            
            Console.WriteLine();
            Console.WriteLine("性能测试完成！");
            Console.WriteLine($"测试结果已保存到: {summary.ResultsDirectoryPath}");
            Console.WriteLine();
            Console.WriteLine("主要指标说明:");
            Console.WriteLine("- Mean: 平均执行时间");
            Console.WriteLine("- Error: 标准误差");
            Console.WriteLine("- StdDev: 标准差");
            Console.WriteLine("- Allocated: 内存分配量");
            Console.WriteLine();
            Console.WriteLine("注意事项:");
            Console.WriteLine("1. 测试结果会因硬件环境而异");
            Console.WriteLine("2. 建议多次运行以获得稳定结果");
            Console.WriteLine("3. 生产环境性能可能与测试环境不同");
        }

        static void ShowHelp()
        {
            Console.WriteLine("使用方法 (简化版):");
            Console.WriteLine("  dotnet run                    # 完整性能测试");
            Console.WriteLine("  dotnet run quick              # 快速测试");
            Console.WriteLine("  dotnet run simple             # 简化对比测试 ⭐");
            Console.WriteLine("  dotnet run --help             # 显示帮助");
            Console.WriteLine();
            Console.WriteLine("推荐测试:");
            Console.WriteLine("  🚀 dotnet run simple          # 最重要的性能对比");
            Console.WriteLine();
            Console.WriteLine("核心测试内容 (simple):");
            Console.WriteLine("  • 单次创建操作对比 (基准测试)");
            Console.WriteLine("  • 10次批量创建对比");
            Console.WriteLine();
            Console.WriteLine("期望结果:");
            Console.WriteLine("  ✅ AutoIncrement 版本性能提升 2倍+");
            Console.WriteLine("  ✅ 内存使用减少 40%+");
            Console.WriteLine("  ✅ 响应时间更稳定");
            Console.WriteLine();
            Console.WriteLine("批处理文件 (简化版):");
            Console.WriteLine("  - run_simple.bat              # 最简化测试");
        }
    }
}

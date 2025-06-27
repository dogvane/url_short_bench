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
            var summary = args.Length > 0 && args[0] == "QuickDbRepositoryBenchmark" 
                ? BenchmarkRunner.Run<QuickDbRepositoryBenchmark>()
                : BenchmarkRunner.Run<DbRepositoryBenchmark>();
            
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
            Console.WriteLine("使用方法:");
            Console.WriteLine("  dotnet run                              # 运行完整性能测试");
            Console.WriteLine("  dotnet run QuickDbRepositoryBenchmark   # 运行快速性能测试");
            Console.WriteLine("  dotnet run --help                      # 显示帮助信息");
            Console.WriteLine();
            Console.WriteLine("完整测试内容:");
            Console.WriteLine("  1. 单次创建操作性能");
            Console.WriteLine("  2. 单次查询操作性能");
            Console.WriteLine("  3. 批量创建操作性能（10/100/1000）");
            Console.WriteLine("  4. 批量查询操作性能（10/100/1000）");
            Console.WriteLine("  5. 并发创建操作性能（10/50）");
            Console.WriteLine("  6. 并发查询操作性能（10/50）");
            Console.WriteLine("  7. 混合操作性能（创建+查询）");
            Console.WriteLine();
            Console.WriteLine("快速测试内容:");
            Console.WriteLine("  1. 单次创建操作（基准）");
            Console.WriteLine("  2. 单次查询操作");
            Console.WriteLine("  3. 10次批量创建");
            Console.WriteLine("  4. 10次批量查询");
            Console.WriteLine();
            Console.WriteLine("输出文件:");
            Console.WriteLine("  - BenchmarkDotNet.Artifacts/ 目录包含详细报告");
            Console.WriteLine("  - *.html: 图表报告");
            Console.WriteLine("  - *.md: Markdown 格式报告");
            Console.WriteLine("  - *.csv: CSV 数据文件");
            Console.WriteLine();
            Console.WriteLine("批处理文件:");
            Console.WriteLine("  - run_benchmark.bat: 运行完整测试");
            Console.WriteLine("  - run_quick_benchmark.bat: 运行快速测试");
        }
    }
}

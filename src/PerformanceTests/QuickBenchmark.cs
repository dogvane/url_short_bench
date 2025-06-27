using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Engines;
using Dapper;
using Microsoft.Data.Sqlite;
using System.IO;
using url_short.common;

namespace PerformanceTests
{
    /// <summary>
    /// 快速性能测试版本 - 用于快速验证性能
    /// 运行时间较短，适合开发调试使用
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, RuntimeMoniker.Net90, launchCount: 1, iterationCount: 3)]
    [MarkdownExporter]
    public class QuickDbRepositoryBenchmark
    {
        private DbRepository _repository = null!;
        private string _testDbPath = null!;
        private readonly List<string> _createdAliases = new();

        [GlobalSetup]
        public void Setup()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"quick_benchmark_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={_testDbPath}";
            _repository = new DbRepository(connectionString);

            // 预创建10个短链接用于查询测试
            for (int i = 0; i < 10; i++)
            {
                var (id, alias) = _repository.CreateShortLink($"https://example.com/test/{i}", null);
                _createdAliases.Add(alias);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            try
            {
                // 强制释放所有连接
                _repository?.Dispose();
                
                // 强制垃圾回收，确保连接被释放
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                // 等待一段时间确保文件句柄被释放
                System.Threading.Thread.Sleep(100);
                
                if (File.Exists(_testDbPath))
                {
                    File.Delete(_testDbPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理数据库文件时出错: {ex.Message}");
            }
        }

        [Benchmark(Baseline = true)]
        public void CreateSingleShortLink()
        {
            // 使用独立的数据库实例避免锁竞争
            var tempPath = Path.Combine(Path.GetTempPath(), $"create_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={tempPath}";
            using var tempRepository = new DbRepository(connectionString);
            
            tempRepository.CreateShortLink("https://example.com/benchmark", null);
            
            try { File.Delete(tempPath); } catch { }
        }

        [Benchmark]
        public async Task QuerySingleShortLink()
        {
            if (_createdAliases.Count > 0)
            {
                await _repository.GetUrlByAliasAsync(_createdAliases[0]);
            }
        }

        [Benchmark]
        public void CreateBatch10()
        {
            // 使用独立的数据库实例避免锁竞争
            var tempPath = Path.Combine(Path.GetTempPath(), $"batch_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={tempPath}";
            using var tempRepository = new DbRepository(connectionString);
            
            for (int i = 0; i < 10; i++)
            {
                tempRepository.CreateShortLink($"https://example.com/batch/{i}", null);
            }
            
            try { File.Delete(tempPath); } catch { }
        }

        [Benchmark]
        public async Task QueryBatch10()
        {
            var tasks = new Task[Math.Min(10, _createdAliases.Count)];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = _repository.GetUrlByAliasAsync(_createdAliases[i]);
            }
            await Task.WhenAll(tasks);
        }
    }
}

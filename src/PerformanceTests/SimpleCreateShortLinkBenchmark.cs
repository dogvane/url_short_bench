using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.IO;
using url_short.common;
using v2;

namespace PerformanceTests
{
    /// <summary>
    /// 简化的 CreateShortLink 性能对比测试
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net90)]
    [MarkdownExporter]
    public class SimpleCreateShortLinkBenchmark
    {
        private DbRepository _dbRepository = null!;
        private DbRepositoryAutoIncrement _dbRepositoryAutoIncrement = null!;
        private string _testDbPath1 = null!;
        private string _testDbPath2 = null!;
        private readonly List<string> _testUrls = new();

        [GlobalSetup]
        public void Setup()
        {
            // 创建两个独立的测试数据库
            _testDbPath1 = Path.Combine(Path.GetTempPath(), $"simple_test_db1_{Guid.NewGuid()}.db");
            _testDbPath2 = Path.Combine(Path.GetTempPath(), $"simple_test_db2_{Guid.NewGuid()}.db");
            
            var connectionString1 = $"Data Source={_testDbPath1}";
            var connectionString2 = $"Data Source={_testDbPath2}";
            
            _dbRepository = new DbRepository(connectionString1);
            _dbRepositoryAutoIncrement = new DbRepositoryAutoIncrement(connectionString2);

            // 准备少量测试数据
            for (int i = 0; i < 100; i++)
            {
                _testUrls.Add($"https://example.com/page/{i}");
            }

            Console.WriteLine($"测试准备完成 - 创建了 {_testUrls.Count} 个测试URL");
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            try
            {
                // 清理测试文件
                if (File.Exists(_testDbPath1)) File.Delete(_testDbPath1);
                if (File.Exists(_testDbPath2)) File.Delete(_testDbPath2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// DbRepository 单次创建测试（基准）
        /// </summary>
        [Benchmark(Baseline = true)]
        public void DbRepository_SingleCreate()
        {
            var url = _testUrls[Random.Shared.Next(_testUrls.Count)];
            _dbRepository.CreateShortLink(url, null);
        }

        /// <summary>
        /// DbRepositoryAutoIncrement 单次创建测试
        /// </summary>
        [Benchmark]
        public void DbRepositoryAutoIncrement_SingleCreate()
        {
            var url = _testUrls[Random.Shared.Next(_testUrls.Count)];
            _dbRepositoryAutoIncrement.CreateShortLink(url, null);
        }

        /// <summary>
        /// DbRepository 批量创建测试
        /// </summary>
        [Benchmark]
        [Arguments(10)]
        public void DbRepository_BatchCreate(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var url = _testUrls[i % _testUrls.Count];
                _dbRepository.CreateShortLink(url, null);
            }
        }

        /// <summary>
        /// DbRepositoryAutoIncrement 批量创建测试
        /// </summary>
        [Benchmark]
        [Arguments(10)]
        public void DbRepositoryAutoIncrement_BatchCreate(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var url = _testUrls[i % _testUrls.Count];
                _dbRepositoryAutoIncrement.CreateShortLink(url, null);
            }
        }
    }
}

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Dapper;
using Microsoft.Data.Sqlite;
using System.IO;
using url_short.common;

namespace PerformanceTests
{
    /// <summary>
    /// DbRepository 性能测试基准类
    /// 使用 BenchmarkDotNet 进行精确的性能测量
    /// </summary>
    [MemoryDiagnoser]  // 监测内存分配
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net90)]  // 指定运行时
    [RPlotExporter]     // 生成图表
    [MarkdownExporter]  // 生成 Markdown 报告
    public class DbRepositoryBenchmark
    {
        private DbRepository _repository = null!;
        private string _testDbPath = null!;
        private string _connectionString = null!;
        private readonly List<string> _testUrls = new();
        private readonly List<string> _createdAliases = new();

        [GlobalSetup]
        public void Setup()
        {
            // 创建临时测试数据库
            _testDbPath = Path.Combine(Path.GetTempPath(), $"benchmark_test_{Guid.NewGuid()}.db");
            _connectionString = $"Data Source={_testDbPath}";
            _repository = new DbRepository(_connectionString);

            // 预准备测试数据
            PrepareTestData();
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

        private void PrepareTestData()
        {
            // 准备测试用的 URL 数据
            for (int i = 0; i < 1000; i++)
            {
                _testUrls.Add($"https://example.com/page/{i}?param={Guid.NewGuid()}");
            }

            // 预创建一些短链接用于查询测试
            for (int i = 0; i < 100; i++)
            {
                var (id, alias) = _repository.CreateShortLink(_testUrls[i], null);
                _createdAliases.Add(alias);
            }
        }

        /// <summary>
        /// 测试创建短链接的性能（不带过期时间）
        /// </summary>
        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1000)]
        public void CreateShortLink_WithoutExpiry(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var url = _testUrls[i % _testUrls.Count];
                _repository.CreateShortLink(url, null);
            }
        }

        /// <summary>
        /// 测试创建短链接的性能（带过期时间）
        /// </summary>
        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1000)]
        public void CreateShortLink_WithExpiry(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var url = _testUrls[i % _testUrls.Count];
                _repository.CreateShortLink(url, 3600); // 1小时过期
            }
        }

        /// <summary>
        /// 测试查询短链接的性能
        /// </summary>
        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1000)]
        public async Task GetUrlByAlias(int count)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < count; i++)
            {
                var alias = _createdAliases[i % _createdAliases.Count];
                tasks.Add(_repository.GetUrlByAliasAsync(alias));
            }
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 测试单次创建操作的性能
        /// </summary>
        [Benchmark]
        public void SingleCreateOperation()
        {
            _repository.CreateShortLink("https://example.com/single-test", null);
        }

        /// <summary>
        /// 测试单次查询操作的性能
        /// </summary>
        [Benchmark]
        public async Task SingleQueryOperation()
        {
            if (_createdAliases.Count > 0)
            {
                await _repository.GetUrlByAliasAsync(_createdAliases[0]);
            }
        }

        /// <summary>
        /// 测试并发创建操作的性能
        /// </summary>
        [Benchmark]
        [Arguments(10)]
        [Arguments(50)]
        public void ConcurrentCreateOperations(int concurrency)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < concurrency; i++)
            {
                int index = i;
                tasks.Add(Task.Run(() =>
                {
                    var url = _testUrls[index % _testUrls.Count];
                    _repository.CreateShortLink(url, null);
                }));
            }
            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// 测试并发查询操作的性能
        /// </summary>
        [Benchmark]
        [Arguments(10)]
        [Arguments(50)]
        public async Task ConcurrentQueryOperations(int concurrency)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < concurrency && i < _createdAliases.Count; i++)
            {
                int index = i;
                tasks.Add(Task.Run(async () =>
                {
                    await _repository.GetUrlByAliasAsync(_createdAliases[index]);
                }));
            }
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 测试混合操作的性能（创建+查询）
        /// </summary>
        [Benchmark]
        [Arguments(50)]
        public async Task MixedOperations(int operationCount)
        {
            var tasks = new List<Task>();
            
            for (int i = 0; i < operationCount; i++)
            {
                if (i % 2 == 0)
                {
                    // 创建操作
                    int index = i;
                    tasks.Add(Task.Run(() =>
                    {
                        var url = _testUrls[index % _testUrls.Count];
                        _repository.CreateShortLink(url, null);
                    }));
                }
                else
                {
                    // 查询操作
                    if (_createdAliases.Count > 0)
                    {
                        int aliasIndex = i % _createdAliases.Count;
                        tasks.Add(_repository.GetUrlByAliasAsync(_createdAliases[aliasIndex]));
                    }
                }
            }
            
            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// DbRepository 类的简化版本用于性能测试
    /// 直接在测试项目中包含，避免依赖问题
    /// </summary>
    public class DbRepository : IDisposable
    {
        private readonly string _connectionString;
        private readonly IShortCodeGen _shortCodeGen;
        private bool _disposed = false;
        private const string TableCheckSql = @"CREATE TABLE IF NOT EXISTS short_links (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            alias TEXT UNIQUE,
            url TEXT NOT NULL,
            expire INTEGER DEFAULT 0,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        );";

        public DbRepository(string connectionString)
        {
            _connectionString = connectionString;
            _shortCodeGen = new Base62Converter();
            EnsureTable();
        }

        private void EnsureTable()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            // 性能优化：WAL模式、同步OFF、临时表内存
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA journal_mode=WAL;";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "PRAGMA synchronous=OFF;";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "PRAGMA temp_store=MEMORY;";
                cmd.ExecuteNonQuery();
            }
            connection.Execute(TableCheckSql);
        }

        public async Task<(long id, string url, long expire)> GetUrlByAliasAsync(string alias)
        {
            using var connection = new SqliteConnection(_connectionString);
            var sql = "SELECT id, url, expire FROM short_links WHERE alias = @alias";
            return await connection.QuerySingleOrDefaultAsync<(long, string, long)>(sql, new { alias });
        }

        private static readonly object _createLock = new object();
        public (long id, string alias) CreateShortLink(string url, int? expireSeconds)
        {
            lock (_createLock)
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                var expire = expireSeconds.HasValue ? (DateTime.UtcNow.AddSeconds(expireSeconds.Value).Ticks) : 0;
                var tempAlias = Guid.NewGuid().ToString("N");
                var insertSql = "INSERT INTO short_links (alias, url, expire) VALUES (@alias, @url, @expire);";
                connection.Execute(insertSql, new { alias = tempAlias, url, expire });

                var id = connection.ExecuteScalar<long>("SELECT last_insert_rowid();");
                string aliasStr = _shortCodeGen.Encode(id);
                var updateSql = "UPDATE short_links SET alias = @alias WHERE id = @id;";
                connection.Execute(updateSql, new { alias = aliasStr, id });

                return (id, aliasStr);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 清理托管资源
                    // SQLite连接在using语句中已经自动释放
                    // 这里确保所有连接池都被清理
                    SqliteConnection.ClearAllPools();
                }
                _disposed = true;
            }
        }

        ~DbRepository()
        {
            Dispose(false);
        }
    }
}

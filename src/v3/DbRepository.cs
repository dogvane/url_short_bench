using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Text;
using common;
using url_short.common;
using System.Data;

namespace v2
{
    public class DbRepository
    {
        private readonly string _connectionString;
        
        private const string TableCheckSql = @"CREATE TABLE IF NOT EXISTS short_links (
            id BIGINT PRIMARY KEY,
            alias VARCHAR(255) UNIQUE,
            url TEXT NOT NULL,
            expire BIGINT DEFAULT 0,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            INDEX idx_alias (alias),
            INDEX idx_expire (expire),
            INDEX idx_created_at (created_at)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=COMPRESSED;";

        private readonly common.SnowflakeIdGenerator _snowflake;

        Base62Converter base62 = new Base62Converter(12);

        public DbRepository(string connectionString, int workerId, int datacenterId)
        {
            // 优化连接字符串以支持连接池
            var builder = new MySqlConnectionStringBuilder(connectionString)
            {
                Pooling = true,
                MinimumPoolSize = 10,
                MaximumPoolSize = 100,
                ConnectionTimeout = 30,
                DefaultCommandTimeout = 30,
                ConnectionLifeTime = 300, // 5分钟
                ConnectionReset = true,
                CharacterSet = "utf8mb4"
            };
            _connectionString = builder.ConnectionString;
            
            _snowflake = new common.SnowflakeIdGenerator(workerId, datacenterId);
            Console.WriteLine($"DbRepository initialized with optimized connection pool: WorkerId={workerId}, DatacenterId={datacenterId}");
            Console.WriteLine($"Connection pool settings: MinPool=10, MaxPool=100, Timeout=30s");
            EnsureTable();
        }

        private void EnsureTable()
        {
            const int maxRetries = 30;
            const int delaySeconds = 2;
            
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    Console.WriteLine($"Attempting to connect to database (attempt {i + 1}/{maxRetries})...");
                    using var connection = new MySqlConnection(_connectionString);
                    connection.Open();
                    connection.Execute(TableCheckSql);
                    Console.WriteLine("Database connection successful and table ensured.");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database connection attempt {i + 1} failed: {ex.Message}");
                    
                    if (i == maxRetries - 1)
                    {
                        Console.WriteLine($"ERROR: Failed to connect to database after {maxRetries} attempts");
                        throw;
                    }
                    
                    Console.WriteLine($"Retrying in {delaySeconds} seconds...");
                    Thread.Sleep(delaySeconds * 1000);
                }
            }
        }

        // 异步版本：获取短链信息 - 优化查询
        public async Task<(long id, string url, long expire)> GetUrlByAliasAsync(string alias)
        {
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                // 移除不必要的连接打开调用，让Dapper自动处理
                var sql = "SELECT id, url, expire FROM short_links WHERE alias = @alias LIMIT 1";
                var result = await connection.QuerySingleOrDefaultAsync<(long, string, long)>(sql, new { alias });
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to get URL by alias '{alias}': {ex.Message}");
                throw;
            }
        }

        // 异步版本：创建短链 - 优化性能
        public async Task<(long id, string alias)> CreateShortLinkAsync(string url, int? expireSeconds)
        {
            try
            {
                var id = _snowflake.NextId();
                var alias = base62.Encode(id);
                var expire = expireSeconds.HasValue ? (DateTime.UtcNow.AddSeconds(expireSeconds.Value).Ticks) : 0;
                
                await using var connection = new MySqlConnection(_connectionString);
                // 让Dapper自动处理连接管理
                var sql = "INSERT INTO short_links (id, alias, url, expire) VALUES (@id, @alias, @url, @expire)";
                await connection.ExecuteAsync(sql, new { id, alias, url, expire });
                return (id, alias);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to create short link for url '{url}': {ex.Message}");
                throw;
            }
        }

        // 批量创建短链接 - 用于高性能场景
        public async Task<List<(long id, string alias)>> CreateShortLinksBatchAsync(List<(string url, int? expireSeconds)> requests)
        {
            try
            {
                var results = new List<(long id, string alias)>();
                var parameters = new List<object>();
                
                foreach (var (url, expireSeconds) in requests)
                {
                    var id = _snowflake.NextId();
                    var alias = base62.Encode(id);
                    var expire = expireSeconds.HasValue ? (DateTime.UtcNow.AddSeconds(expireSeconds.Value).Ticks) : 0;
                    
                    results.Add((id, alias));
                    parameters.Add(new { id, alias, url, expire });
                }
                
                await using var connection = new MySqlConnection(_connectionString);
                var sql = "INSERT INTO short_links (id, alias, url, expire) VALUES (@id, @alias, @url, @expire)";
                await connection.ExecuteAsync(sql, parameters);
                
                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to create batch short links: {ex.Message}");
                throw;
            }
        }
    }
}

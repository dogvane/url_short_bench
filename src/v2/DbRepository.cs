using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Threading.Tasks;
using url_short.common;

namespace v2
{
    public class DbRepository
    {
        private readonly string _connectionString;
        private readonly IShortCodeGen _shortCodeGen;
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
            Console.WriteLine($"Ensuring database table exists... {_connectionString}");
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
            
            // 启动时输出数据库基本情况
            OutputDatabaseStats();
        }

        private void OutputDatabaseStats()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                
                // 获取总记录数
                var totalCount = connection.ExecuteScalar<long>("SELECT COUNT(*) FROM short_links");
                
                // 获取有过期时间的记录数
                var expiredCount = connection.ExecuteScalar<long>("SELECT COUNT(*) FROM short_links WHERE expire > 0 AND expire < @now", new { now = DateTime.UtcNow.Ticks });
                
                // 获取最早和最新的记录创建时间
                var earliestRecord = connection.QuerySingleOrDefault<DateTime?>("SELECT MIN(created_at) FROM short_links");
                var latestRecord = connection.QuerySingleOrDefault<DateTime?>("SELECT MAX(created_at) FROM short_links");
                
                // 获取数据库文件大小
                var dbSize = connection.ExecuteScalar<long>("PRAGMA page_count") * connection.ExecuteScalar<long>("PRAGMA page_size");
                
                Console.WriteLine("=== 数据库基本情况 ===");
                Console.WriteLine($"总记录数: {totalCount:N0}");
                Console.WriteLine($"已过期记录数: {expiredCount:N0}");
                Console.WriteLine($"有效记录数: {totalCount - expiredCount:N0}");
                if (earliestRecord.HasValue && latestRecord.HasValue)
                {
                    Console.WriteLine($"数据时间范围: {earliestRecord:yyyy-MM-dd HH:mm:ss} 至 {latestRecord:yyyy-MM-dd HH:mm:ss}");
                }
                Console.WriteLine($"数据库大小: {dbSize / 1024.0 / 1024.0:F2} MB");
                Console.WriteLine("==================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取数据库统计信息时出错: {ex.Message}");
            }
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
    }
}

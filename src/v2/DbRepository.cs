using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Threading.Tasks;

namespace v2
{
    public class DbRepository
    {
        private readonly string _connectionString;
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
        }

        public async Task<(long id, string url, long expire)> GetUrlByAliasAsync(string alias)
        {
            using var connection = new SqliteConnection(_connectionString);
            var sql = "SELECT id, url, expire FROM short_links WHERE alias = @alias";
            return await connection.QuerySingleOrDefaultAsync<(long, string, long)>(sql, new { alias });
        }

        private static readonly object _createLock = new object();
        public async Task<(long id, string alias)> CreateShortLinkAsync(string url, int? expireSeconds)
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
                string aliasStr = Base62Converter.Encode(id);
                var updateSql = "UPDATE short_links SET alias = @alias WHERE id = @id;";
                connection.Execute(updateSql, new { alias = aliasStr, id });

                return (id, aliasStr);
            }
        }
    }
}

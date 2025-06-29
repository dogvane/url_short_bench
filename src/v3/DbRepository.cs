using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Text;
using common;
using url_short.common;

namespace v2
{
    public class DbRepository
    {
        private readonly string _connectionString;
        
        private const string TableCheckSql = @"CREATE TABLE IF NOT EXISTS short_links (
            id BIGINT AUTO_INCREMENT PRIMARY KEY,
            alias VARCHAR(255) UNIQUE,
            url TEXT NOT NULL,
            expire BIGINT DEFAULT 0,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            INDEX idx_alias (alias)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

        private readonly SnowflakeIdGenerator _snowflake;

        Base62Converter base62 = new Base62Converter(12);

        public DbRepository(string connectionString, int workerId, int datacenterId)
        {
            _connectionString = connectionString;
            _snowflake = new SnowflakeIdGenerator(workerId, datacenterId);
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

        // 异步版本：获取短链信息
        public async Task<(long id, string url, long expire)> GetUrlByAliasAsync(string alias)
        {
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
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

        // 异步版本：创建短链
        public async Task<(long id, string alias)> CreateShortLinkAsync(string url, int? expireSeconds)
        {
            try
            {
                var id = _snowflake.NextId();
                var alias = base62.Encode(id);
                var expire = expireSeconds.HasValue ? (DateTime.UtcNow.AddSeconds(expireSeconds.Value).Ticks) : 0;
                await using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
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
    }

    /// <summary>
    /// 雪花算法ID生成器 - 解决并发ID生成问题
    /// </summary>
    public class SnowflakeIdGenerator
    {
        private const long Epoch = 1288834974657L; // 自定义纪元时间戳
        private const int WorkerIdBits = 5;
        private const int DatacenterIdBits = 5;
        private const int SequenceBits = 12;

        private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
        private const long MaxDatacenterId = -1L ^ (-1L << DatacenterIdBits);
        private const long SequenceMask = -1L ^ (-1L << SequenceBits);

        private const int WorkerIdShift = SequenceBits;
        private const int DatacenterIdShift = SequenceBits + WorkerIdBits;
        private const int TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;

        private readonly long _workerId;
        private readonly long _datacenterId;
        private long _sequence = 0L;
        private long _lastTimestamp = -1L;
        private readonly object _lock = new object();

        public SnowflakeIdGenerator(long workerId, long datacenterId)
        {
            if (workerId > MaxWorkerId || workerId < 0)
                throw new ArgumentException($"Worker ID can't be greater than {MaxWorkerId} or less than 0");
            
            if (datacenterId > MaxDatacenterId || datacenterId < 0)
                throw new ArgumentException($"Datacenter ID can't be greater than {MaxDatacenterId} or less than 0");

            _workerId = workerId;
            _datacenterId = datacenterId;
        }

        public long NextId()
        {
            lock (_lock)
            {
                var timestamp = TimeGen();

                if (timestamp < _lastTimestamp)
                    throw new Exception($"Invalid system clock. Timestamp {timestamp} is before {_lastTimestamp}");

                if (_lastTimestamp == timestamp)
                {
                    _sequence = (_sequence + 1) & SequenceMask;
                    if (_sequence == 0)
                        timestamp = TilNextMillis(_lastTimestamp);
                }
                else
                {
                    _sequence = 0L;
                }

                _lastTimestamp = timestamp;

                return ((timestamp - Epoch) << TimestampLeftShift) |
                       (_datacenterId << DatacenterIdShift) |
                       (_workerId << WorkerIdShift) |
                       _sequence;
            }
        }

        private long TilNextMillis(long lastTimestamp)
        {
            var timestamp = TimeGen();
            while (timestamp <= lastTimestamp)
                timestamp = TimeGen();
            return timestamp;
        }

        private static long TimeGen()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}

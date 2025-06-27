using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Text;

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

        public DbRepository(string connectionString)
        {
            _connectionString = connectionString;
            EnsureTable();
        }

        private void EnsureTable()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                connection.Execute(TableCheckSql);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to ensure database table: {ex.Message}");
                throw;
            }
        }

        public (long id, string url, long expire) GetUrlByAlias(string alias)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                var sql = "SELECT id, url, expire FROM short_links WHERE alias = @alias LIMIT 1";
                var result = connection.QuerySingleOrDefault<(long, string, long)>(sql, new { alias });
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to get URL by alias '{alias}': {ex.Message}");
                throw;
            }
        }

        private static readonly SnowflakeIdGenerator _snowflake = new SnowflakeIdGenerator(1, 1);
        
        public (long id, string alias) CreateShortLink(string url, int? expireSeconds)
        {
            try
            {
                // 使用雪花算法生成唯一ID，避免数据库竞争
                var id = _snowflake.NextId();
                var alias = Base62Converter_Optimized.Encode(id);
                var expire = expireSeconds.HasValue ? (DateTime.UtcNow.AddSeconds(expireSeconds.Value).Ticks) : 0;
                
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();
                
                var sql = "INSERT INTO short_links (id, alias, url, expire) VALUES (@id, @alias, @url, @expire)";
                connection.Execute(sql, new { id, alias, url, expire });
                
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

    /// <summary>
    /// 优化版本的Base62转换器，专门用于高并发场景
    /// </summary>
    public static class Base62Converter_Optimized
    {
        private const string Characters = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static readonly int Base = Characters.Length;

        /// <summary>
        /// 简单版本：直接编码ID，适用于使用雪花算法或预分配ID的场景
        /// </summary>
        public static string Encode(long number)
        {
            if (number < 0) throw new ArgumentOutOfRangeException(nameof(number), "Number must be non-negative.");
            
            if (number == 0) return Characters[0].ToString();

            var sb = new StringBuilder();
            while (number > 0)
            {
                sb.Insert(0, Characters[(int)(number % Base)]);
                number /= Base;
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// 解码Base62字符串为数字
        /// </summary>
        public static long Decode(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) throw new ArgumentNullException(nameof(str));

            long number = 0;
            foreach (char c in str)
            {
                var index = Characters.IndexOf(c);
                if (index == -1)
                    throw new ArgumentException("Invalid character in Base62 string.", nameof(str));
                number = number * Base + index;
            }
            
            return number;
        }
    }
}

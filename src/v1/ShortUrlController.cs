using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Threading;
using url_short.common;

namespace v1
{
    [ApiController]
    public class ShortUrlController : ControllerBase
    {
        // 内存存储短链接映射 - 预分配容量减少扩容开销
        private static readonly ConcurrentDictionary<long, (string url, DateTime? expireAt)> urlStore = new(Environment.ProcessorCount * 2, 1000000);
        
        // Base62编码器，生成6位长度的短码
        private static readonly Base62Converter converter = new(6);
        
        // 自增ID生成器
        private static long nextId = 1;

        private static readonly ShortUrlStats stats = new(() => urlStore.Count);

        [HttpPost("/create")]
        public IActionResult Create([FromBody] CreateRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.url))
                return BadRequest("url is required");

            // 生成唯一的数字ID
            long id = Interlocked.Increment(ref nextId);
            
            // 将数字ID编码为Base62短码
            string alias = converter.Encode(id);

            DateTime? expireAt = null;
            if (req.expire.HasValue && req.expire.Value > 0)
            {
                // 缓存当前时间，避免多次系统调用
                var now = DateTime.UtcNow;
                expireAt = now.AddSeconds(req.expire.Value);
            }

            urlStore[id] = (req.url, expireAt);

            stats.IncCreate();

            return Ok(new { alias, url = req.url, expireAt });
        }

        [HttpGet("/u/{alias}")]
        public IActionResult RedirectToUrl(string alias)
        {
            try
            {
                // 将Base62短码解码为数字ID
                long id = converter.Decode(alias);
                
                if (urlStore.TryGetValue(id, out var entry))
                {
                    // 简化过期检查，减少性能开销
                    if (entry.expireAt.HasValue && entry.expireAt.Value < DateTime.UtcNow)
                    {
                        urlStore.TryRemove(id, out _);
                        return NotFound();
                    }
                    
                    stats.IncGet();
                    return Redirect(entry.url);
                }
                return NotFound();
            }
            catch (ArgumentException)
            {
                // Base62解码失败，说明alias无效
                return NotFound();
            }
        }

        [HttpGet("/stats")]
        public IActionResult GetStats()
        {
            var totalEntries = urlStore.Count;
            var memoryMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
            var uptime = DateTime.UtcNow.Subtract(stats.StartTime);
            
            return Ok(new {
                totalEntries,
                memoryUsageMB = Math.Round(memoryMB, 2),
                uptimeSeconds = Math.Round(uptime.TotalSeconds, 1),
                nextId = Interlocked.Read(ref nextId) - 1 // 当前已使用的最大ID
            });
        }

        public class CreateRequest
        {
            public required string url { get; set; }
            public int? expire { get; set; }
        }
    }
}
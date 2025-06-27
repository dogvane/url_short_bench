using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace v1
{
    [ApiController]
    public class ShortUrlController : ControllerBase
    {
        // 内存存储短链接映射
        private static readonly ConcurrentDictionary<string, (string url, DateTime? expireAt)> urlStore = new();

        private static readonly ShortUrlStats stats = new(() => urlStore.Count);

        [HttpPost("/create")]
        public async Task<IActionResult> Create([FromBody] CreateRequest req)
        {
            return await Task.Run<IActionResult>(() =>
            {
                if (req == null || string.IsNullOrWhiteSpace(req.url))
                    return BadRequest("url is required");

                // 生成 alias
                string alias;
                do
                {
                    alias = Guid.NewGuid().ToString("N")[..6];
                } while (urlStore.ContainsKey(alias));

                DateTime? expireAt = null;
                if (req.expire.HasValue && req.expire.Value > 0)
                    expireAt = DateTime.UtcNow.AddSeconds(req.expire.Value);

                urlStore[alias] = (req.url, expireAt);

                stats.IncCreate();

                return Ok(new { alias, url = req.url, expireAt });
            });
        }

        [HttpGet("/u/{alias}")]
        public async Task<IActionResult> RedirectToUrl(string alias)
        {
            return await Task.Run<IActionResult>(() =>
            {
                if (urlStore.TryGetValue(alias, out var entry))
                {
                    if (entry.expireAt.HasValue && entry.expireAt.Value < DateTime.UtcNow)
                    {
                        urlStore.TryRemove(alias, out _);
                        return NotFound();
                    }
                    stats.IncGet();
                    return Redirect(entry.url);
                }
                return NotFound();
            });
        }

        public class CreateRequest
        {
            public required string url { get; set; }
            public int? expire { get; set; }
        }
    }
}
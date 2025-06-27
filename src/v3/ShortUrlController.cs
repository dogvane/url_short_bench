using Microsoft.AspNetCore.Mvc;

namespace v2
{
    [ApiController]
    public class ShortUrlController : ControllerBase
    {
        private readonly DbRepository _dbRepository;
        private readonly CacheService? _cacheService;
        private static readonly ShortUrlStats stats = new(() => 0); // TODO: fix count

        public ShortUrlController(DbRepository dbRepository, CacheService? cacheService = null)
        {
            _dbRepository = dbRepository;
            _cacheService = cacheService;
        }

        [HttpPost("/create")]
        public async Task<IActionResult> Create([FromBody] CreateRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.url))
                {
                    return BadRequest("url is required");
                }

                var (id, alias) = _dbRepository.CreateShortLink(req.url, req.expire);
                
                // 将新创建的短链接缓存到Redis
                if (_cacheService != null)
                {
                    var expireTime = req.expire.HasValue ? DateTime.UtcNow.AddSeconds(req.expire.Value).Ticks : 0;
                    await _cacheService.SetShortLinkAsync(alias, id, req.url, expireTime);
                }
                
                stats.IncCreate();

                return Ok(new { alias, url = req.url, id });

            }
            catch (System.Text.Json.JsonException ex)
            {
                return BadRequest("Invalid JSON body");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"ERROR: Unhandled exception in Create: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("/u/{alias}")]
        public async Task<IActionResult> RedirectToUrl(string alias)
        {
            try
            {
                (long id, string url, long expire) result = (0, null, 0);

                // 首先尝试从Redis缓存获取
                if (_cacheService != null)
                {
                    var cachedResult = await _cacheService.GetShortLinkAsync(alias);
                    if (cachedResult.HasValue)
                    {
                        result = cachedResult.Value;
                        Console.WriteLine($"Cache hit for alias: {alias}");
                    }
                }

                // 如果缓存未命中，从数据库获取
                if (string.IsNullOrEmpty(result.url))
                {
                    result = _dbRepository.GetUrlByAlias(alias);
                    Console.WriteLine($"Database query for alias: {alias}");
                    
                    // 将数据库结果缓存到Redis
                    if (_cacheService != null && !string.IsNullOrEmpty(result.url))
                    {
                        await _cacheService.SetShortLinkAsync(alias, result.id, result.url, result.expire);
                    }
                }

                if (!string.IsNullOrEmpty(result.url))
                {
                    if (result.expire > 0 && result.expire < System.DateTime.UtcNow.Ticks)
                    {
                        // 链接已过期，从缓存中移除
                        Console.WriteLine($"Link expired for alias: {alias}, expire: {result.expire}, now: {System.DateTime.UtcNow.Ticks}");
                        if (_cacheService != null)
                        {
                            await _cacheService.RemoveShortLinkAsync(alias);
                        }
                        // TODO: add delete from database
                        return NotFound();
                    }
                    Console.WriteLine($"Redirecting alias {alias} to {result.url}");
                    stats.IncGet();
                    return Redirect(result.url);
                }
                return NotFound();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"ERROR: Unhandled exception in RedirectToUrl for alias '{alias}': {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("/health")]
        public IActionResult Health()
        {
            try
            {
                // 测试数据库连接
                var testResult = _dbRepository.GetUrlByAlias("health-check-dummy");
                
                // 测试Redis连接
                var redisStatus = "disabled";
                if (_cacheService != null)
                {
                    try
                    {
                        // 尝试一个简单的Redis操作
                        _cacheService.SetStringAsync("health-check", "ok", TimeSpan.FromSeconds(10)).Wait();
                        redisStatus = "connected";
                    }
                    catch
                    {
                        redisStatus = "error";
                    }
                }

                return Ok(new { 
                    status = "healthy", 
                    timestamp = DateTime.UtcNow,
                    database = "connected",
                    redis = redisStatus,
                    version = "v3.0"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Health check failed: {ex.Message}");
                return StatusCode(503, new { 
                    status = "unhealthy", 
                    error = ex.Message,
                    timestamp = DateTime.UtcNow 
                });
            }
        }

        public class CreateRequest
        {
            public required string url { get; set; }
            public int? expire { get; set; }
        }
    }
}
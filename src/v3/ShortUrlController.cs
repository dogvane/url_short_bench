using Microsoft.AspNetCore.Mvc;
using url_short.common;
using System.Diagnostics;

namespace v2
{
    [ApiController]
    public class ShortUrlController : ControllerBase
    {
        private readonly DbRepository _dbRepository;
        private readonly CacheService? _cacheService;
        private readonly MonitoringService _monitoringService;
        private static readonly ShortUrlStats stats = new(() => 0);

        public ShortUrlController(DbRepository dbRepository, MonitoringService monitoringService, CacheService? cacheService = null)
        {
            _dbRepository = dbRepository;
            _cacheService = cacheService;
            _monitoringService = monitoringService;
        }

        [HttpPost("/create")]
        public async Task<IActionResult> Create([FromBody] CreateRequest req)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.url))
                {
                    return BadRequest("url is required");
                }

                var (id, alias) = await _monitoringService.MeasureDatabaseOperation("create_shorturl", 
                    async () => await _dbRepository.CreateShortLinkAsync(req.url, req.expire));
                
                // 将新创建的短链接缓存到Redis
                if (_cacheService != null)
                {
                    var expireTime = req.expire.HasValue ? DateTime.UtcNow.AddSeconds(req.expire.Value).Ticks : 0;
                    await _monitoringService.MeasureCacheOperation<bool>("set_shorturl",
                        async () => {
                            await _cacheService.SetShortLinkAsync(alias, id, req.url, expireTime);
                            return true;
                        });
                }
                
                stats.IncCreate();
                _monitoringService.RecordShortUrlCreated(sw.Elapsed.TotalSeconds);

                return Ok(new { alias, url = req.url, id });

            }
            catch (System.Text.Json.JsonException ex)
            {
                _monitoringService.RecordError("json_parse_error", "create");
                Console.WriteLine($"JSON parsing error in Create: {ex.Message}");
                return BadRequest("Invalid JSON body");
            }
            catch (Microsoft.AspNetCore.Http.BadHttpRequestException ex) when (ex.Message.Contains("Unexpected end of request content"))
            {
                // 处理客户端连接中断或请求不完整的情况
                _monitoringService.RecordError("incomplete_request", "create");
                Console.WriteLine($"Incomplete request in Create: {ex.Message}");
                return BadRequest("Request content incomplete");
            }
            catch (System.OperationCanceledException ex)
            {
                // 处理请求取消的情况
                _monitoringService.RecordError("request_cancelled", "create");
                Console.WriteLine($"Request cancelled in Create: {ex.Message}");
                return StatusCode(499, "Request cancelled"); // 499 Client Closed Request
            }
            catch (System.Exception ex)
            {
                _monitoringService.RecordError("unhandled_exception", "create");
                Console.WriteLine($"ERROR: Unhandled exception in Create: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("/u/{alias}")]
        public async Task<IActionResult> RedirectToUrl(string alias)
        {
            var sw = Stopwatch.StartNew();
            bool cacheHit = false;
            
            try
            {
                (long id, string? url, long expire) result = (0, null, 0);

                // 首先尝试从Redis缓存获取
                if (_cacheService != null)
                {
                    var cachedResult = await _monitoringService.MeasureCacheOperation("get_shorturl",
                        async () => await _cacheService.GetShortLinkAsync(alias));
                    
                    if (cachedResult.HasValue)
                    {
                        result = cachedResult.Value;
                        cacheHit = true;
                        Console.WriteLine($"Cache hit for alias: {alias}");
                    }
                }

                // 如果缓存未命中，从数据库获取
                if (string.IsNullOrEmpty(result.url))
                {
                    result = await _monitoringService.MeasureDatabaseOperation("get_shorturl_by_alias",
                        async () => await _dbRepository.GetUrlByAliasAsync(alias));
                    Console.WriteLine($"Database query for alias: {alias}");
                    
                    // 将数据库结果缓存到Redis
                    if (_cacheService != null && !string.IsNullOrEmpty(result.url))
                    {
                        await _monitoringService.MeasureCacheOperation<bool>("set_shorturl_from_db",
                            async () => {
                                await _cacheService.SetShortLinkAsync(alias, result.id, result.url, result.expire);
                                return true;
                            });
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
                            await _monitoringService.MeasureCacheOperation<bool>("remove_expired_shorturl",
                                async () => {
                                    await _cacheService.RemoveShortLinkAsync(alias);
                                    return true;
                                });
                        }
                        // TODO: add delete from database
                        return NotFound();
                    }
                    Console.WriteLine($"Redirecting alias {alias} to {result.url}");
                    stats.IncGet();
                    _monitoringService.RecordShortUrlQueried(cacheHit, sw.Elapsed.TotalSeconds);
                    return Redirect(result.url);
                }
                
                _monitoringService.RecordShortUrlQueried(cacheHit, sw.Elapsed.TotalSeconds);
                return NotFound();
            }
            catch (System.OperationCanceledException ex)
            {
                // 处理请求取消的情况
                _monitoringService.RecordError("request_cancelled", "redirect");
                Console.WriteLine($"Request cancelled in RedirectToUrl for alias '{alias}': {ex.Message}");
                return StatusCode(499, "Request cancelled"); // 499 Client Closed Request
            }
            catch (Microsoft.AspNetCore.Http.BadHttpRequestException ex)
            {
                // 处理HTTP请求相关错误
                _monitoringService.RecordError("bad_http_request", "redirect");
                Console.WriteLine($"Bad HTTP request in RedirectToUrl for alias '{alias}': {ex.Message}");
                return BadRequest("Invalid request");
            }
            catch (System.Exception ex)
            {
                _monitoringService.RecordError("unhandled_exception", "redirect");
                Console.WriteLine($"ERROR: Unhandled exception in RedirectToUrl for alias '{alias}': {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("/health")]
        public async Task<IActionResult> Health()
        {
            try
            {
                // 测试数据库连接
                var testResult = await _dbRepository.GetUrlByAliasAsync("health-check-dummy");
                
                // 测试Redis连接
                var redisStatus = "disabled";
                if (_cacheService != null)
                {
                    try
                    {
                        // 尝试一个简单的Redis操作
                        await _cacheService.SetStringAsync("health-check", "ok", TimeSpan.FromSeconds(10));
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

        [HttpGet("/snowflake/config")]
        public IActionResult CheckSnowflakeConfig()
        {
            try
            {
                // 假设 DbRepository 有 Snowflake 属性
                var snowflake = _dbRepository.Snowflake;
                var config = new
                {
                    WorkerId = snowflake.WorkerId,
                    DatacenterId = snowflake.DatacenterId,
                    LastTimestamp = snowflake.LastTimestamp,
                    CurrentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    IsTimeRollback = snowflake.LastTimestamp > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                return Ok(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CheckSnowflakeConfig error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public class CreateRequest
        {
            public required string url { get; set; }
            public int? expire { get; set; }
        }
    }
}
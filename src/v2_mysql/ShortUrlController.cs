using Microsoft.AspNetCore.Mvc;

namespace v2
{
    [ApiController]
    public class ShortUrlController : ControllerBase
    {
        private readonly DbRepository _dbRepository;
        private static readonly ShortUrlStats stats = new(() => 0); // TODO: fix count

        public ShortUrlController(DbRepository dbRepository)
        {
            _dbRepository = dbRepository;
        }

        [HttpPost("/create")]
        public IActionResult Create([FromBody] CreateRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.url))
                {
                    return BadRequest("url is required");
                }

                var (id, alias) = _dbRepository.CreateShortLink(req.url, req.expire);
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
        public IActionResult RedirectToUrl(string alias)
        {
            try
            {
                var (id, url, expire) = _dbRepository.GetUrlByAlias(alias);
                if (url != null)
                {
                    if (expire > 0 && expire < System.DateTime.UtcNow.Ticks)
                    {
                        // TODO: add delete
                        return NotFound();
                    }
                    stats.IncGet();
                    return Redirect(url);
                }
                return NotFound();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"ERROR: Unhandled exception in RedirectToUrl for alias '{alias}': {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        public class CreateRequest
        {
            public required string url { get; set; }
            public int? expire { get; set; }
        }
    }
}
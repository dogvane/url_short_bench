using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using url_short.common;

namespace v2
{
    [ApiController]
    public class ShortUrlController : ControllerBase
    {
        private readonly IShortUrlRepository _repository;
        private static readonly ShortUrlStats stats = new(() => 0); // TODO: fix count

        public ShortUrlController(IShortUrlRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("/create")]
        public IActionResult Create([FromBody] CreateRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.url))
                    return BadRequest("url is required");

                var (id, alias) = _repository.CreateShortLink(req.url, req.expire);

                stats.IncCreate();

                return Ok(new { alias, url = req.url, id });
            }
            catch (System.Text.Json.JsonException)
            {
                return BadRequest("Invalid JSON body");
            }
            catch (System.Exception ex)
            {
                // 可选：记录日志
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("/u/{alias}")]
        public async Task<IActionResult> RedirectToUrl(string alias)
        {
            var (id, url, expire) = await _repository.GetUrlByAliasAsync(alias);
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

        public class CreateRequest
        {
            public required string url { get; set; }
            public int? expire { get; set; }
        }
    }
}
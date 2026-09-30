using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace OrbitX.Controllers
{
    [ApiController]
    public class SitemapController : ControllerBase
    {
        private readonly IMemoryCache _cache;

        private const string CACHE_KEY = "SitemapXmlContent";

        public SitemapController(IMemoryCache cache)
        {
            _cache = cache;
        }

        [HttpGet("/sitemap.xml")]
        public IActionResult GetSitemap()
        {
            // Пытаемся достать сгенерированную XML - строку из оперативной памяти
            if (_cache.TryGetValue(CACHE_KEY, out string? xmlContent)) // Замени строку на свою константу CACHE_KEY
            {
                // Обязательно возвращаем тип application/xml, чтобы браузеры и роботы поняли формат
                return Content(xmlContent!, "application/xml", System.Text.Encoding.UTF8);
            }

            return NotFound("Карта сайта еще не сгенерирована...");
        }
    }
}

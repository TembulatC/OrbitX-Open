
using Core.Modules.SatelliteData.Infrastructure.DBContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Xml.Linq;

namespace OrbitX.BackgroundWorkers
{
    public partial class SitemapBackgroundWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider; // Вызов провайдера для Scoped
        private readonly ILogger<SitemapBackgroundWorker> _logger; // Логгирование
        private readonly IMemoryCache _cache; // Кэширование

        private const string BASE_URL = "https://orbitx-web.com";
        public const string CACHE_KEY = "SitemapXmlContent";

        public SitemapBackgroundWorker(IServiceProvider serviceProvider, ILogger<SitemapBackgroundWorker> logger, IMemoryCache cache)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LogLaunchWorker();

            // Небольшая пауза при старте
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);          

            try
            {
                while(!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        LogCreateXML();

                        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
                        var urlset = new XElement(ns + "urlset");
               
                        urlset = AddStaticPages(urlset, ns); // Статические страницы

                        using (var scope = _serviceProvider.CreateScope())
                        {
                            LogAddDynamicPages();

                            stoppingToken.ThrowIfCancellationRequested();

                            var dbContext = scope.ServiceProvider.GetRequiredService<OMMDBContext>();
                            var listIdsSatellites = await dbContext.Satellites.AsNoTracking().Select(id => new { id.NORAD_CAT_ID, id.UpdatedAt }).ToListAsync();

                            if (listIdsSatellites == null || listIdsSatellites.Count <= 0)
                            {
                                LogNullOrEmptyListId();
                                throw new Exception();
                            }

                            foreach (var sat in listIdsSatellites)
                            {
                                urlset.Add(new XElement(ns + "url",
                                    new XElement(ns + "loc", $"{BASE_URL}/satellites-modeling/{sat.NORAD_CAT_ID}"),
                                    new XElement(ns + "lastmod", sat.UpdatedAt.ToString("yyyy-MM-dd")),
                                    new XElement(ns + "changefreq", "daily"),
                                    new XElement(ns + "priority", "0.7")
                                ));
                            }

                        }

                        string result = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + urlset.ToString();
                        _cache.Set(CACHE_KEY, result, TimeSpan.FromDays(1.5));

                        LogCreatedXML();

                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
                        var urlset = new XElement(ns + "urlset");

                        urlset = AddStaticPages(urlset, ns); // Статические страницы

                        string result = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + urlset.ToString();
                        _cache.Set(CACHE_KEY, result, TimeSpan.FromHours(1));

                        LogError(ex);
                    }

                    LogEndWorker();

                    // Засыпаем на 1 день
                    await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Метод корректно завершается при выключении сервера
            }
        }

        private XElement AddStaticPages(XElement urlset, XNamespace ns)
        {
            LogAddStaticPages();

            var staticPages = new[]
            {
                new { Loc = "/", ChangeFreq = "weekly", Priority = "1.0" },
                new { Loc = "/satellites-modeling", ChangeFreq = "daily", Priority = "0.9" },
                new { Loc = "/donate", ChangeFreq = "monthly", Priority = "0.6" },
                new { Loc = "/privacy", ChangeFreq = "yearly", Priority = "0.3" },
                new { Loc = "/terms", ChangeFreq = "yearly", Priority = "0.3" },
            };

            foreach (var page in staticPages)
            {
                urlset.Add(
                    new XElement(ns + "url",
                    new XElement(ns + "loc", $"{BASE_URL}{page.Loc}"),
                    new XElement(ns + "changefreq", page.ChangeFreq),
                    new XElement(ns + "priority", page.Priority)
                ));
            }

            return urlset;
        }
    }
}

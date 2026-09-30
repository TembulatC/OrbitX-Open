namespace OrbitX.BackgroundWorkers
{
    public partial class SitemapBackgroundWorker
    {
        [LoggerMessage(
            EventId = 5001,
            Level = LogLevel.Information,
            Message = "Фоновый процесс OrbitX запущен. Через 20 секунд будет запущен цикл создания sitemap.xml")]
        private partial void LogLaunchWorker();

        [LoggerMessage(
            EventId = 5002,
            Level = LogLevel.Information,
            Message = "Цикл создания sitemap.xml запущен")]
        private partial void LogCreateXML();

        [LoggerMessage(
            EventId = 5003,
            Level = LogLevel.Information,
            Message = "Добавление статических страниц")]
        private partial void LogAddStaticPages();

        [LoggerMessage(
            EventId = 5004,
            Level = LogLevel.Information,
            Message = "Добавление динамических страниц")]
        private partial void LogAddDynamicPages();

        [LoggerMessage(
            EventId = 5005,
            Level = LogLevel.Warning,
            Message = "Список ID спутников для добавления в динамические страницы пришел пустым")]
        private partial void LogNullOrEmptyListId();


        [LoggerMessage(
            EventId = 5006,
            Level = LogLevel.Error,
            Message = "Ошибка при генерации sitemap.xml: {Ex}")]
        private partial void LogError(Exception ex);

        [LoggerMessage(
            EventId = 5008,
            Level = LogLevel.Information,
            Message = "sitemap.xml успешно создан и кеширован")]
        private partial void LogCreatedXML();

        [LoggerMessage(
            EventId = 5009,
            Level = LogLevel.Information,
            Message = "Цикл создания sitemap.xml окончен. Следующий запуск через 1 день")]
        private partial void LogEndWorker();
    }
}
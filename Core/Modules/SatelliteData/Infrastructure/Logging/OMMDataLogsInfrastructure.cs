using Microsoft.Extensions.Logging;

namespace Core.Modules.SatelliteData.Infrastructure.Repositories
{
    public partial class SatellitesDataRepository
    {
        [LoggerMessage(
            EventId = 301,
            Level = LogLevel.Information,
            Message = "Запуск добавления данных в базу данных")]
        private partial void LogLaunch();

        [LoggerMessage(
            EventId = 302,
            Level = LogLevel.Warning,
            Message = "Добавление данных отменено. Список спутников пришел пустым")]
        private partial void LogCancellationAddData();

        [LoggerMessage(
            EventId = 303,
            Level = LogLevel.Information,
            Message = "Данные по спутникам успешно обновлены")]
        private partial void LogUpdateData();

        [LoggerMessage(
            EventId = 304,
            Level = LogLevel.Information,
            Message = "Запуск добавления данных в базу данных")]
        private partial void LogLaunchCts();

        [LoggerMessage(
            EventId = 305,
            Level = LogLevel.Warning,
            Message = "Добавление данных отменено. Список спутников пришел пустым")]
        private partial void LogCancellationAddDataCts();

        [LoggerMessage(
            EventId = 306,
            Level = LogLevel.Information,
            Message = "Данные по спутникам успешно обновлены")]
        private partial void LogUpdateDataCts();
    }

    public partial class SatellitesGetDataRepository
    {
        #region GetDatayID

        [LoggerMessage(
            EventId = 901,
            Level = LogLevel.Information,
            Message = "Запуск получения данных спутника с ID {NoradId} из базы данных")]
        private partial void LogLaunchById(int noradId);

        [LoggerMessage(
            EventId = 902,
            Level = LogLevel.Warning,
            Message = "Отмена получения данных. ID спутника не может быть отрицательным")]
        private partial void LogNegativeNumber();

        [LoggerMessage(
            EventId = 903,
            Level = LogLevel.Information,
            Message = "Данные о спутнике успешно получены")]
        private partial void LogSuccessById();

        [LoggerMessage(
            EventId = 904,
            Level = LogLevel.Warning,
            Message = "Спутника с ID {NoradId} не существует в базе данных")]
        private partial void LogNotFoundById(int noradId);

        #endregion

        #region GetDataByName

        [LoggerMessage(
            EventId = 905,
            Level = LogLevel.Information,
            Message = "Запуск получения данных спутника {SatelliteName} из базы данных")]
        private partial void LogLaunchByName(string satelliteName);

        [LoggerMessage(
            EventId = 906,
            Level = LogLevel.Warning,
            Message = "Отмена получения данных. Имя спутника не может быть пустым")]
        private partial void LogCancelNullByName();

        [LoggerMessage(
            EventId = 907,
            Level = LogLevel.Information,
            Message = "Данные о спутнике успешно получены")]
        private partial void LogSuccessByName();

        [LoggerMessage(
            EventId = 908,
            Level = LogLevel.Warning,
            Message = "Спутника {SatelliteName} не существует в базе данных")]
        private partial void LogNotFoundByName(string satelliteName);

        #endregion
    }
}

namespace Core.Modules.SatelliteData.Infrastructure.HttpClients
{
    public partial class HttpSatellitesData
    {
        [LoggerMessage(
            EventId = 401,
            Level = LogLevel.Information,
            Message = "Создаем HTTP запрос для поиска спутников")]
        private partial void LogCreateHTTP();

        [LoggerMessage(
            EventId = 402,
            Level = LogLevel.Information,
            Message = "URL запроса: {Url}")]
        private partial void LogURL(string url);

        [LoggerMessage(
            EventId = 403,
            Level = LogLevel.Error,
            Message = "(HTTP {StatusCode}) Доступ временно заблокирован из-за слишком частых запросов")]
        private partial void Log403StatusCode(int statusCode);

        [LoggerMessage(
           EventId = 404,
           Level = LogLevel.Warning,
           Message = "(HTTP {StatusCode}) Страницы к которой был HTTP-запрос не существует")]
        private partial void Log404StatusCode(int statusCode);

        [LoggerMessage(
           EventId = 405,
           Level = LogLevel.Error,
           Message = "(HTTP {StatusCode}) На внешнем сервере запросов произошел сбой")]
        private partial void Log500StatusCode(int statusCode);

        [LoggerMessage(
           EventId = 406,
           Level = LogLevel.Warning,
           Message = "(HTTP {StatusCode}) Внешний сервер запросов временно недоступен")]
        private partial void Log503StatusCode(int statusCode);

        [LoggerMessage(
           EventId = 407,
           Level = LogLevel.Error,
           Message = "(HTTP {StatusCode}) Сетевой запрос завершился с неизвестной ошибкой: {ReasonPhrase}")]
        private partial void LogUnknownError(int statusCode, string? reasonPhrase);

        [LoggerMessage(
           EventId = 408,
           Level = LogLevel.Warning,
           Message = "Тело HTTP-запроса пришло пустым")]
        private partial void LogNullOrWhitSpace();

        [LoggerMessage(
           EventId = 409,
           Level = LogLevel.Information,
           Message = "HTTP-запрос был завершен успешно")]
        private partial void LogSuccessful();

        [LoggerMessage(
           EventId = 410,
           Level = LogLevel.Error,
           Message = "Неизвестная сетевая ошибка при HTTP-запросе")]
        private partial void LogUnknownHttpError(HttpRequestException ex);

        [LoggerMessage(
           EventId = 411,
           Level = LogLevel.Error,
           Message = "Неизвестная ошибка при HTTP-запросе")]
        private partial void LogOtherError(Exception ex);

        [LoggerMessage(
           EventId = 412,
           Level = LogLevel.Warning,
           Message = "Неверный запрос. Ссылка некорректна/спутников такой категории не существует")]
        private partial void LogInvalid();
    }
}
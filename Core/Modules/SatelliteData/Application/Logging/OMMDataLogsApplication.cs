using Microsoft.Extensions.Logging;

namespace Core.Modules.SatelliteData.Application.Services
{
    public partial class SatellitesDataService
    {
        [LoggerMessage(
            EventId = 101,
            Level = LogLevel.Information,
            Message = "Цикл запущен")]
        private partial void LogLaunch();

        [LoggerMessage(
            EventId = 102,
            Level = LogLevel.Warning,
            Message = "Отмена запуска парсера. Строка данных пришла пустой")]
        private partial void LogCancellationParser();

        [LoggerMessage(
            EventId = 103,
            Level = LogLevel.Warning,
            Message = "Отмена запуска добавления данных в базу данных. Список спутников вернулся пустым")]
        private partial void LogCancellationDB();

        [LoggerMessage(
            EventId = 104,
            Level = LogLevel.Information,
            Message = "Цикл окончен")]
        private partial void LogEnding();
    }

    public partial class SatellitesParserService
    {
        [LoggerMessage(
            EventId = 201,
            Level = LogLevel.Information,
            Message = "Запуск парсера")]
        private partial void LogLaunch();

        [LoggerMessage(
            EventId = 202,
            Level = LogLevel.Warning,
            Message = "Отмена обработки парсером. Строка OMM данных пришла пустой")]
        private partial void LogCancellationParserProcessing();

        [LoggerMessage(
            EventId = 203,
            Level = LogLevel.Information,
            Message = "Данные спутников отформатированы")]
        private partial void LogParserFormatting();
    }

    public partial class SatellitesGetDataService
    {
        #region GetDataByID

        [LoggerMessage(
            EventId = 551,
            Level = LogLevel.Information,
            Message = "Цикл запущен")]
        private partial void LogLaunchById();

        [LoggerMessage(
            EventId = 552,
            Level = LogLevel.Warning,
            Message = "Отмена цикла. Передан отрицательный NoradId")]
        private partial void LogCancelNegativeNumber();

        [LoggerMessage(
            EventId = 553,
            Level = LogLevel.Warning,
            Message = "Отмена цикла. Данные о спутниках в ID которых входит {NoradId} не найдены")]
        private partial void LogCancelNull(int noradId);

        [LoggerMessage(
            EventId = 554,
            Level = LogLevel.Information,
            Message = "Цикл окончен")]
        private partial void LogEndById();

        #endregion

        #region GetDataByName

        [LoggerMessage(
            EventId = 555,
            Level = LogLevel.Information,
            Message = "Цикл запущен")]
        private partial void LogLaunchByName();

        [LoggerMessage(
            EventId = 556,
            Level = LogLevel.Warning,
            Message = "Отмена цикла. Пришло пустое имя спутника")]
        private partial void LogCancelNameNull();

        [LoggerMessage(
            EventId = 557,
            Level = LogLevel.Warning,
            Message = "Отмена цикла. Данные о спутниках в имя которых входит {SatelliteName} не найдены")]
        private partial void LogCancelNull(string satelliteName);

        [LoggerMessage(
            EventId = 558,
            Level = LogLevel.Information,
            Message = "Цикл окончен")]
        private partial void LogEndByName();

        #endregion
    }
}
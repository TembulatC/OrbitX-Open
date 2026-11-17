namespace OrbitX.SignalRHubs
{
    public partial class SignalRHub
    {
        [LoggerMessage(
            EventId = 2001,
            Level = LogLevel.Warning,
            Message = "Клиент SignalR отключен аварийно. ConnectionId: {ConnectionId}. Ошибка: {ExceptionMessage}")]
        private partial void LogClientAbruptDisconnect(string connectionId, string exceptionMessage);

        [LoggerMessage(
            EventId = 2002,
            Level = LogLevel.Warning,
            Message = "Создание/подключение к группе SatellitesCategory:{SatellitesCategory} не удалось, данной категории не существует")]
        private partial void LogNotContainsCategory(string satellitesCategory);

        [LoggerMessage(
            EventId = 2003,
            Level = LogLevel.Warning,
            Message = "Невозможно передать ID меньше 1")]
        private partial void LogNegativeNoradId();

        [LoggerMessage(
            EventId = 2004,
            Level = LogLevel.Warning,
            Message = "Пользователь ушел с несуществующего спутника: ID спутника {NoradId}, категория: {SatellitesCategory}")]
        private partial void LogNotFoundSatellite(int noradId, string satellitesCategory);

        [LoggerMessage(
            EventId = 2005,
            Level = LogLevel.Warning,
            Message = "Пользователь ушел со страницы несуществующего/их спутника/ов")]
        private partial void LogNotFoundSatellitePage();
    }
}
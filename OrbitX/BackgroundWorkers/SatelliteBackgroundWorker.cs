using Core.Modules.SGP4Data.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OrbitX.BackgroundWorkers.Helper;
using OrbitX.SignalRHubs;
using Serilog.Context;
using System.Collections.Concurrent;

namespace OrbitX.BackgroundWorkers
{
    public partial class SatelliteBackgroundWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider; // Вызов провайдера для Scoped
        private readonly IHubContext<SignalRHub> _hubContext; // Связываем с SignalR      
        private readonly ILogger<SatelliteBackgroundWorker> _logger; // Логгер

        private readonly static string[] CelestrakCategories = new[]
        {
            "weather", "resource", "sar", "sarsat", "dmc", "tdrss", "argos",
            "planet", "spire", "geo", "gpz", "gpz-plus", "intelsat", "ses",
            "eutelsat", "telesat", "starlink", "oneweb", "qianfan", "hulianwang",
            "kuiper", "iridium-next", "orbcomm", "globalstar", "amateur",
            "satnogs", "x-comm", "other-comm", "gnss", "gps-ops", "glo-ops",
            "galileo", "beidou", "sbas", "science", "geodetic", "engineering",
            "education", "military", "radar", "cubesat"
        };

        private static readonly ConcurrentDictionary<string, (CancellationTokenSource cts, int counter, ConcurrentDictionary<int, int> _satelliteUsers)> _satellitesCategoryThreads = new(); // Адресная книга. Категория спутников -> Количество людей на категории в общем

        private CancellationToken _serverStoppingToken; // Ссылка на токен остановки всего сервера

        // Внедряем логгер для отслеживания тактов в консоли
        public SatelliteBackgroundWorker(ILogger<SatelliteBackgroundWorker> logger, IHubContext<SignalRHub> hubContext, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;
        }

        // Метод вызывается из SignalR при первом подключении к спутнику
        public void OnSatelliteWatched(int noradId, string satellitesCategory)
        {
            if (noradId <= 0)
            {
                LogNegativeNoradId();
                return;
            }

            if (!CelestrakCategories.Contains(satellitesCategory))
            {
                LogNotContainsCategory(satellitesCategory);
                return;
            }

            while (true)
            {
                // Связываем токен категории с токеном сервера
                // Поток умрет или при вызове OnSatelliteUnwatched, или при выключении всего сервера
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_serverStoppingToken);

                bool addTaskRun = _satellitesCategoryThreads.TryAdd(satellitesCategory, (linkedCts, 1, new ConcurrentDictionary<int, int> { [noradId] = 1 }));

                if (addTaskRun)
                {
                    LogAddToken(satellitesCategory);

                    // Если успешно добавили — запускаем поток расчета. Токен ушел в словарь
                    _ = Task.Run(async () => await StartSatelliteStreamingThread(satellitesCategory, linkedCts.Token, linkedCts), linkedCts.Token);

                    return;
                }
                else
                {
                    if (!_satellitesCategoryThreads.TryGetValue(satellitesCategory, out var oldValue))
                    {
                        linkedCts.Dispose();
                        continue; // Категорию удалил какой-то из потоков, идем на второй круг
                    }

                    int updatedUserCount = oldValue.counter + 1;
                    int updateNoradId;
                    int udpateNoradIdUserCount;

                    if (oldValue._satelliteUsers.TryGetValue(noradId, out var count))
                    {
                        updateNoradId = noradId;
                        udpateNoradIdUserCount = count + 1;
                    }
                    else
                    {
                        updateNoradId = noradId;
                        udpateNoradIdUserCount = 1;
                    }

                    ConcurrentDictionary<int, int> updateSatelliteUsers = new ConcurrentDictionary<int, int>(oldValue._satelliteUsers)
                    {
                        [updateNoradId] = udpateNoradIdUserCount
                    };

                    var newValue = (oldValue.cts, updatedUserCount, updateSatelliteUsers);

                    if (_satellitesCategoryThreads.TryUpdate(satellitesCategory, newValue, oldValue))
                    {
                        linkedCts.Dispose();
                        return;
                    }
                    else continue;
                }
            }
        }

        // Метод вызывается из SignalR при отключении
        public void OnSatelliteUnwatched(int noradId, string satellitesCategory)
        {
            // Бесконечный цикл нужен только на случай, если другой поток вклинится,
            // чтобы мы просто прокрутили логику заново с актуальными цифрами
            while (true)
            {
                // Достаем текущее состояние категории из словаря
                if (!_satellitesCategoryThreads.TryGetValue(satellitesCategory, out var oldValue)) return; // Категории уже нет, выходим

                // Уменьшаем счетчик зрителей на 1
                int updatedUserCount = oldValue.counter - 1;

                if (updatedUserCount > 0)
                {
                    // --- СЦЕНАРИЙ А: Категорию еще кто-то смотрит ---
                    // Копируем старый внутренний словарь
                    var updateSatelliteUsers = new ConcurrentDictionary<int, int>(oldValue._satelliteUsers);

                    if (updateSatelliteUsers.TryGetValue(noradId, out var currentNoradCount))
                    {
                        if (currentNoradCount > 1) updateSatelliteUsers[noradId] = currentNoradCount - 1; // У этого спутника еще остались зрители — уменьшаем на 1
                        else updateSatelliteUsers.TryRemove(noradId, out _); // Это был последний зритель данного спутника — удаляем noradId из словаря
                    }

                    // Собираем новый кортеж из 3 элементов
                    var newState = (oldValue.cts, updatedUserCount, updateSatelliteUsers);

                    // TryUpdate меняет старое значение на новое
                    // Он сработает, ТОЛЬКО если в словаре всё еще лежит oldValue
                    if (_satellitesCategoryThreads.TryUpdate(satellitesCategory, newState, oldValue)) return; // Успешно обновили, выходим

                    // Если TryUpdate вернул false — значит, кто-то вклинился параллельно
                    // Цикл while автоматически уйдет на вторую попытку
                }
                else
                {
                    // --- СЦЕНАРИЙ Б: Это был ПОСЛЕДНИЙ пользователь (updatedUserCount == 0) ---
                    // Он удалит ключ ТОЛЬКО если значение в словаре равно oldValue (счетчик == 1)
                    var entryToRemove = KeyValuePair.Create(satellitesCategory, oldValue);
                    if (_satellitesCategoryThreads.TryRemove(entryToRemove))
                    {
                        LogCancelTask(satellitesCategory);

                        oldValue.cts.Cancel();

                        return;
                    }

                    // Если TryRemove не смог удалить (потому что кто-то успел подключиться и поднять счетчик),
                    // цикл while прокрутится заново, увидит актуальный counter и уйдет в Сценарий А
                }
            }
        }

        // Главный бесконечный цикл фонового процесса. Вызывается один раз при старте сервера
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LogLaunchWorker();

            // Запоминаем токен сервера, чтобы связывать его с токенами спутников
            _serverStoppingToken = stoppingToken;

            // Цикл получения и обновления данных
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    Log2HoursСycle();

                    await Task.Delay(TimeSpan.FromHours(2), stoppingToken);

                    LogLaunchСycle();

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        stoppingToken.ThrowIfCancellationRequested();

                        var downloaderTLE = scope.ServiceProvider.GetRequiredService<SatelliteOMMDownloader>();
                        await downloaderTLE.GetSatelliteData(stoppingToken);
                    }

                    LogEndСycle();

                }
            }
            catch (OperationCanceledException)
            {
                // Метод корректно завершается при выключении сервера
            }

            LogStopWorker();
        }

        // ИЗОЛИРОВАННЫЙ, ПАРАЛЛЕЛЬНЫЙ ПОТОК РАСЧЕТА ДЛЯ КОНКРЕТНОй КАТЕГОРИИ СПУТНИКОВ
        private async Task StartSatelliteStreamingThread(string satellitesCategory, CancellationToken token, CancellationTokenSource linkedCts)
        {
            LogAddTask(satellitesCategory);

            try
            {
                // Бесконечный цикл
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        using (LogContext.PushProperty("RequestSource", "Worker or Special"))
                        {
                            // Scope на каждом такте внутри цикла while, чтобы EF Core очищал соеднинение с базой PostgreSQL!
                            using var scope = _serviceProvider.CreateScope();
                            var sgpService = scope.ServiceProvider.GetRequiredService<ISatelliteSGPServices>();

                            List<int> activeNoradIds = [];

                            if (_satellitesCategoryThreads.TryGetValue(satellitesCategory, out var categoryState))
                            {
                                // Достаем все ключи (noradId) из вложенного словаря и превращаем в List<int>
                                activeNoradIds = categoryState._satelliteUsers.Keys.ToList();
                            }

                            // Получаем координаты на текущий такт
                            var data = await sgpService.GetSGPListByID(activeNoradIds);

                            if (data != null && data.Count > 0)
                            {
                                // Пуш в группу категории
                                await _hubContext.Clients.Group($"SatellitesCategory:{satellitesCategory}").SendAsync("ReceivePosition", data, token);
                            }
                        }

                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        LogException(satellitesCategory, ex.Message);

                    }

                    // Обновление каждую секунду
                    await Task.Delay(1000, token);
                }

                LogOperationCanceled(satellitesCategory);
            }
            catch (OperationCanceledException) { LogOperationCanceledException(satellitesCategory); } // Игнорируем: поток завершает работу по требованию CancellationToken
            finally
            {
                // Поток гарантированно завершил работу (вышел из цикла или упал)
                // Освобождаем ресурсы токена здесь
                linkedCts.Dispose();
                LogDisposeTask(satellitesCategory);
            }
        }
    }
}
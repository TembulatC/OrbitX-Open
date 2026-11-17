using Microsoft.AspNetCore.SignalR;
using OrbitX.BackgroundWorkers;

namespace OrbitX.SignalRHubs
{
    public partial class SignalRHub : Hub
    {
        private readonly ILogger<SignalRHub> _logger;
        private readonly SatelliteBackgroundWorker _worker;

        public SignalRHub(ILogger<SignalRHub> logger, SatelliteBackgroundWorker worker)
        {
            _logger = logger;
            _worker = worker;
        }

        // Свой словарь на каждое соединение: noradId -> категория этого спутника
        private Dictionary<int, string> GetWatchingCategories()
        {
            if (Context.Items.TryGetValue("SatellitesCategories", out var cachedCategories) && cachedCategories is Dictionary<int, string> satellitesCategories)
            {
                return satellitesCategories;
            }

            // Если добавляется первый спутник
            var createdSatellitesCategories = new Dictionary<int, string>();
            Context.Items["SatellitesCategories"] = createdSatellitesCategories;

            return createdSatellitesCategories;
        }

        // Этот метод автоматически вызовет TypeScript, когда пользователь зайдет на страницу /{id_satellite}
        public async Task WatchSatellite(int noradId, string satellitesCategory)
        {
            Dictionary<int, string> satellitesCategories = GetWatchingCategories();
            if (satellitesCategories.ContainsKey(noradId)) return; // Спутник уже есть в словаре, поток второй раз не пробуем запускать

            satellitesCategories[noradId] = satellitesCategory; // Добавляем новый спутник в словарь

            // Веерный пуш: Сажаем сокет пользователя в изолированную комнату (Группу) категории спутника которого он выбрал
            await Groups.AddToGroupAsync(Context.ConnectionId, $"SatellitesCategory:{satellitesCategory}");
            // Сигнализируем нашему фоновому воркеру, чтобы он проверил/запустил поток расчета
            _worker.OnSatelliteWatched(noradId, satellitesCategory);
        }

        // Пользователь убрал спутник, соединение остаётся открытым
        public async Task UnwatchSatellite(int noradId, string satellitesCategory)
        {
            if (Context.Items.TryGetValue("SatellitesCategories", out var cachedCategories) && cachedCategories is Dictionary<int, string> satellitesCategories)
            {
                // Убираем сокет из группы
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"SatellitesCategory:{satellitesCategory}");
                // Сигнализируем воркеру, чтобы он проверил, не пора ли тушить параллельный поток
                _worker.OnSatelliteUnwatched(noradId, satellitesCategory);

                satellitesCategories.Remove(noradId);
            }
        }

        // Вызывается автоматически, когда вкладка закрыта или оборвался интернет
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null) LogClientAbruptDisconnect(Context.ConnectionId, exception.Message);

            if (Context.Items.TryGetValue("SatellitesCategories", out var cachedCategories) && cachedCategories is Dictionary<int, string> satellitesCategories)
            {
                foreach (var (noradId, satellitesCategory) in satellitesCategories)
                {
                    // Убираем сокет из группы
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"SatellitesCategory:{satellitesCategory}");
                    // Сигнализируем воркеру, чтобы он проверил, не пора ли тушить параллельный поток
                    _worker.OnSatelliteUnwatched(noradId, satellitesCategory);

                    satellitesCategories.Remove(noradId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
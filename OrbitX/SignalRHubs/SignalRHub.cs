using Microsoft.AspNetCore.SignalR;
using OrbitX.BackgroundWorkers;

namespace OrbitX.SignalRHubs
{
    public partial class SignalRHub : Hub
    {
        private readonly ILogger<SignalRHub> _logger;
        private readonly SatelliteBackgroundWorker _worker;

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
                satellitesCategories.Remove(noradId);

                // Сигнализируем воркеру, чтобы он проверил, не пора ли тушить параллельный поток
                _worker.OnSatelliteUnwatched(noradId, satellitesCategory);

                if (satellitesCategories.Values.Count(category => category == satellitesCategory) <= 0)
                {
                    // Убираем сокет из группы
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"SatellitesCategory:{satellitesCategory}");
                }
            }
            else LogNotFoundSatellite(noradId, satellitesCategory);
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
            else LogNotFoundSatellitePage();

            await base.OnDisconnectedAsync(exception);
        }
    }
}
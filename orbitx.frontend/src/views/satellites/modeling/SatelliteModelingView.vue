<template>
  <section class="modeling-section">
    <div class="container">

      <!-- ВЕРХНИЙ БЛОК: ИНФОРМАЦИЯ О ТЕКУЩЕМ СПУТНИКЕ -->
      <SatelliteStatusBar :display-name="displayName" :norad-id="noradId" :sync-status="syncStatus" />

      <!-- КАРТА + ТЕЛЕМЕТРИЯ ПОД НЕЙ -->
      <div class="modeling-layout">

        <!-- КАРТА НА ВСЮ ШИРИНУ -->
        <div class="map-column">
          <div class="map-container-box">
            <!-- Главный контейнер для Leaflet.js -->
            <div id="orbitx-leaflet-map"
                 class="real-map-element"
                 :class="{ 'map-visible': isMapLoaded }"></div>

            <!-- ПЛАШКА СТАТУСА ПОДКЛЮЧЕНИЯ КАРТЫ (поверх карты) -->
            <div class="map-status-toast" v-if="showMapConnectingToast">
              <span class="toast-spinner"></span>
              <p class="toast-text">Loading map...</p>
            </div>
          </div>
        </div>

        <!-- ПАНЕЛЬ МЕЖДУ КАРТОЙ И БЛОКАМИ СПУТНИКОВ: чекбоксы подписей + кнопка добавления -->
        <div class="map-toolbar">
          <div class="map-options">
            <label class="map-option">
              <input type="checkbox" v-model="showNames" />
              <span>Show satellite names</span>
            </label>
            <label class="map-option" :class="{ 'map-option-disabled': !showNames }">
              <input type="checkbox" v-model="showDetails" :disabled="!showNames" />
              <span>Show detailed info</span>
            </label>
            <label class="map-option" :class="{ 'map-option-disabled': !showNames }">
              <input type="checkbox" v-model="hoverOnly" :disabled="!showNames" />
              <span>Show on hover only</span>
            </label>
          </div>

          <div class="add-sat-wrap" ref="addWrapRef">
            <div v-if="showAdd" class="add-sat-menu">
              <div class="add-sat-search-row">
                <input v-model="addQuery"
                       type="text"
                       class="add-sat-input"
                       placeholder="Search by name or NORAD ID..."
                       @keyup.enter="runAddSearch" />
                <button type="button"
                        class="add-sat-search-btn"
                        :disabled="addLoading || addQuery.trim() === ''"
                        @click="runAddSearch">
                  Search
                </button>
              </div>
              <div class="add-sat-list">
                <div v-if="addLoading" class="add-sat-hint">Searching...</div>
                <div v-else-if="addMessage" class="add-sat-hint">{{ addMessage }}</div>
                <label v-for="r in addResults"
                       :key="r.noradId"
                       class="add-sat-item"
                       :class="{ 'add-sat-item-disabled': !canToggle(r.noradId) }">
                  <input v-model="selectedIds"
                         type="checkbox"
                         class="add-sat-check"
                         :value="r.noradId"
                         :disabled="!canToggle(r.noradId)" />
                  <span class="add-sat-item-id">{{ r.noradId }}</span>
                  <span class="add-sat-item-name">{{ r.name }}</span>
                </label>
              </div>
              <button v-if="selectedIds.length > 0"
                      type="button"
                      class="add-sat-confirm-btn"
                      @click="addSelectedSatellites">
                Add ({{ selectedIds.length }})
              </button>
            </div>
            <button type="button" class="add-sat-btn" :disabled="limitReached" :style="limitReached ? 'opacity:0.5;cursor:not-allowed' : ''" @click.stop="toggleAddMenu">+ Add Satellite ({{ extraSats.length }}/{{ MAX_EXTRA_SATS }})</button>
          </div>
        </div>

        <!-- ДОПОЛНИТЕЛЬНЫЕ СПУТНИКИ -->
        <div class="extra-bars">
          <SatelliteTelemetryBar v-for="s in extraSats"
                                 :key="s.noradId"
                                 :name="s.name"
                                 :norad-id="s.noradId"
                                 :data="s.data"
                                 :color="s.color"
                                 :is-live="s.status === 'success'"
                                 removable
                                 @remove="removeExtraSat(s.noradId)" />
        </div>

        <!-- ПОЛОСА ТЕЛЕМЕТРИИ ОСНОВНОГО СПУТНИКА -->
        <SatelliteTelemetryBar :name="displayName"
                               :norad-id="noradId"
                               :data="satelliteData"
                               :is-live="syncStatus === 'success'" />

      </div>

    </div>
  </section>
</template>

<script setup lang="ts">
  import { ref, shallowRef, computed, watch, onMounted, onUnmounted, nextTick } from 'vue'
  import { useRoute } from 'vue-router'
  import { onBeforeRouteLeave } from 'vue-router'
  import * as signalR from '@microsoft/signalr'
  import L from 'leaflet'

  import SatelliteStatusBar from './SatelliteStatusBar.vue'
  import SatelliteTelemetryBar from './SatelliteTelemetryBar.vue'
  import type { SGP4DataDTO, SatelliteFilterDTO, SyncStatus, ExtraSat } from '../../../types/satellite'
  import { MAIN_SAT_COLOR, pickDistinctColor } from '../../../utils/satelliteColors'
  import '../../../assets/satellite-map.css'

  const route = useRoute()

  const noradId = ref<number>(0)
  const satelliteData = shallowRef<SGP4DataDTO | null>(null)

  // Имя спутника из обычного GET-запроса (GetDataById).
  // Приходит сразу при загрузке страницы, не зависит от WebSocket и не пропадает при его обрыве
  const satelliteName = ref<string | null>(null)

  // Итоговое имя для отображения: сначала из сокета (если уже пришло), иначе из GET-запроса
  const displayName = computed(() => satelliteData.value?.name ?? satelliteName.value)

  const syncStatus = ref<SyncStatus>('pending')
  const connectionStatus = ref<'connecting' | 'connected' | 'disconnected'>('connecting')
  const statusText = ref('Establishing a connection...')

  const isMapLoaded = ref<boolean>(false)

  const isMapTileLoading = ref<boolean>(true)
  const isOnline = ref<boolean>(typeof navigator !== 'undefined' ? navigator.onLine : true)

  const showMapConnectingToast = computed(() => {
    return !isOnline.value || isMapTileLoading.value || !isMapLoaded.value
  })

  let map: L.Map | null = null
  let mapTileLayer: L.TileLayer | null = null
  let satelliteMarker: L.Marker | null = null
  let orbitPath: L.Polyline | null = null
  let resizeObserver: ResizeObserver | null = null

  let hubConnection: signalR.HubConnection | null = null
  let dbTimeoutTimer: number | null = null
  let heartbeatTimer: number | null = null
  let isFirstPosition = true

  // Хранилище для отслеживания долготы предыдущей полученной точки спутника
  let prevLongitude: number | null = null

  const handleBrowserOnline = () => {
    isOnline.value = true
    // Как только сеть вернулась, пробуем переинициировать загрузку тайлов заново
    if (mapTileLayer) {
      isMapTileLoading.value = true
      mapTileLayer.redraw()
    }
  }

  const handleBrowserOffline = () => {
    isOnline.value = false
  }

  // Загружаем имя спутника обычным GET-запросом по ID из URL.
  // Эндпоинт возвращает список (ищет ID, в который входит число), поэтому берём точное совпадение.
  const loadSatelliteInfo = async (id: number) => {
    try {
      const response = await fetch(`/api/v1/GetDataById?noradId=${id}`)
      if (!response.ok) return

      const list: SatelliteFilterDTO[] = await response.json()
      const sat = list.find((s) => s.noradId === id)

      // Если пользователь уже перешёл на другого спутника, устаревший ответ игнорируем
      if (sat && noradId.value === id) {
        satelliteName.value = sat.name
      }
    } catch (err) {
      console.warn('[ЦУП] Не удалось получить имя спутника:', err)
    }
  }

  const initLeafletMap = () => {
    if (map) {
      map.remove()
      map = null
      mapTileLayer = null
      satelliteMarker = null
      orbitPath = null
    }

    isMapLoaded.value = true
    isMapTileLoading.value = true

    // Даем Vue один тик на обновление DOM-дерева и очистку плейсхолдера
    nextTick(() => {
      // Ограничиваем область карты максимальными координатами планеты
      const corner1 = L.latLng(-90, -180)
      const corner2 = L.latLng(90, 180)
      const bounds = L.latLngBounds(corner1, corner2)

      map = L.map('orbitx-leaflet-map', {
        center: [0, 0],
        zoom: window.innerWidth < 600 ? 1 : 2,
        minZoom: 1,
        maxZoom: 15,
        zoomControl: true,
        maxBounds: bounds,
        maxBoundsViscosity: 1.0
      })

      mapTileLayer = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
      })

      // Отслеживаем реальное состояние загрузки тайлов, а не момент инициализации карты.
      // 'loading' срабатывает при каждом запросе новых тайлов (в т.ч. при zoom),
      // 'load' — когда все запрошенные тайлы догрузились,
      // 'tileerror' — когда тайл не удалось получить
      mapTileLayer.on('loading', () => {
        isMapTileLoading.value = true
      })
      mapTileLayer.on('load', () => {
        isMapTileLoading.value = false
      })
      mapTileLayer.on('tileerror', () => {
        isMapTileLoading.value = true
      })

      mapTileLayer.addTo(map)

      // ResizeObserver: карта пересчитывает размер при любом изменении контейнера (поворот телефона, resize окна)
      const mapEl = document.getElementById('orbitx-leaflet-map')
      if (mapEl) {
        resizeObserver?.disconnect()
        resizeObserver = new ResizeObserver(() => map?.invalidateSize())
        resizeObserver.observe(mapEl)
      }

      orbitPath = L.polyline([], {
        color: '#ea75a2',
        weight: 3,
        opacity: 0.8,
        dashArray: '5, 5',
        smoothFactor: 2
      }).addTo(map)

      // Фикс для дефолтных картинок маркеров Leaflet
      delete (L.Icon.Default.prototype as any)._getIconUrl
      L.Icon.Default.mergeOptions({
        iconRetinaUrl: new URL('leaflet/dist/images/marker-icon-2x.png', import.meta.url).href,
        iconUrl: new URL('leaflet/dist/images/marker-icon.png', import.meta.url).href,
        shadowUrl: new URL('leaflet/dist/images/marker-shadow.png', import.meta.url).href
      })

      // Принудительный пересчет размеров контейнера через таймаут
      setTimeout(() => {
        if (map) {
          map.invalidateSize()
          map.setView([0, 0], 2)
        }
      }, 100)
    })
  }

  // ── Режим обновления карты ──
  // 0   — каждая пришедшая позиция применяется сразу (максимальная точность, выше нагрузка на ЦП);
  // >0  — позиции копятся и применяются пакетом раз в N мс (меньше нагрузка, задержка до N мс).
  // Пакетом одновременно обновляются таблица, точка, траектория и подпись.
  const MAP_FLUSH_INTERVAL_MS: number = 0
  const pendingMapUpdates: Array<() => void> = []
  let mapFlushTimer: number | null = null

  const scheduleMapUpdate = (apply: () => void) => {
    if (MAP_FLUSH_INTERVAL_MS <= 0) {
      apply()
    } else {
      pendingMapUpdates.push(apply)
    }
  }

  const flushMapUpdates = () => {
    if (pendingMapUpdates.length === 0) return
    const batch = pendingMapUpdates.splice(0)
    for (const apply of batch) apply()
  }

  const applySatelliteOnMap = (lat: number, lng: number) => {
    if (!map) return

    const newPos = L.latLng(lat, lng)

    // Маркер-точка спутника
    if (!satelliteMarker) {
      // Размер иконки 30x30 = размер пульсирующей волны, поэтому центр точки ровно совпадает с координатой (anchor = половина размера)
      const satelliteIcon = L.divIcon({
        className: 'custom-satellite-icon',
        html: `<div class="satellite-ping-wave"></div><div class="satellite-ping-core"></div>`,
        iconSize: [30, 30],
        iconAnchor: [15, 15]
      })

      satelliteMarker = L.marker(newPos, {
        icon: satelliteIcon,
        interactive: true,    // нужно для показа названия при наведении
        keyboard: false,
        zIndexOffset: 1000    // всегда поверх остальных слоёв
      }).addTo(map)
      refreshAllLabels()
    } else {
      satelliteMarker.setLatLng(newPos)
    }

    if (prevLongitude !== null) {
      // Если дельта по долготе больше 180 градусов, значит произошел переход через край карты (180°/-180°)
      // Спутник пошел на новый круг, полностью очищаем накопленные координаты траектории
      if (Math.abs(lng - prevLongitude) > 180) {
        orbitPath?.setLatLngs([])
        console.log('[ЦУП] Спутник завершил виток. Траектория сброшена для нового круга.')
      }
    }
    // Обновляем значение долготы текущим шагом
    prevLongitude = lng

    // Линия тянется за точкой
    // Новая координата добавляется в конец линии в тот же момент, когда на неё переезжает точка, поэтому конец пунктира всегда совпадает с положением спутника
    orbitPath?.addLatLng(newPos)

    if (isFirstPosition) {
      map.setView(newPos, 3, { animate: true })
      isFirstPosition = false
    }

  }

  // Таблица, точка, траектория и подпись обновляются вместе, поэтому всегда показывают одно и то же
  const updateSatelliteOnMap = (data: SGP4DataDTO) => {
    scheduleMapUpdate(() => {
      satelliteData.value = data
      applySatelliteOnMap(data.latitude, data.longitude)
      refreshLabelContent(satelliteMarker, displayName.value, MAIN_SAT_COLOR, data)
    })
  }

  const clearDbTimeout = () => {
    if (dbTimeoutTimer) { clearTimeout(dbTimeoutTimer); dbTimeoutTimer = null; }
  }

  const clearHeartbeatTimeout = () => {
    if (heartbeatTimer) { clearTimeout(heartbeatTimer); heartbeatTimer = null; }
  }

  const resetHeartbeatTimeout = () => {
    clearHeartbeatTimeout()
    heartbeatTimer = window.setTimeout(() => {
      if (syncStatus.value === 'success') {
        syncStatus.value = 'pending'
        console.warn('[ЦУП] Поток координат приостановлен. Ожидание восстановления связи...')
      }
    }, 4000)
  }

  const stopSignalR = async () => {
    await stopAllExtras()
    clearDbTimeout()
    clearHeartbeatTimeout()
    isFirstPosition = true
    // Сброс долготы при смене спутника
    prevLongitude = null

    if (hubConnection) {
      try {
        hubConnection.off('ReceivePosition')
        await hubConnection.stop()
        console.log('[SignalR] Соединение успешно разорвано.')
      } catch (err) {
        console.warn('[SignalR] Мягкий перехват при остановке группы/сокета:', err)
      } finally {
        hubConnection = null
        connectionStatus.value = 'disconnected'
        satelliteData.value = null
        syncStatus.value = 'pending'
      }
    }

    // Отбрасываем неприменённые обновления предыдущего спутника
    pendingMapUpdates.length = 0
  }

  const startSatelliteTracking = async (id: number) => {
    await stopSignalR()
    await nextTick()

    initLeafletMap()

    noradId.value = id

    // Сбрасываем имя предыдущего спутника и сразу грузим имя нового обычным GET-запросом.
    // Без await — чтобы не задерживать подключение WebSocket
    satelliteName.value = null
    loadSatelliteInfo(id)

    connectionStatus.value = 'connecting'
    syncStatus.value = 'pending'
    statusText.value = 'Connection...'

    dbTimeoutTimer = window.setTimeout(() => {
      if (syncStatus.value === 'pending') {
        syncStatus.value = 'db_error'
        console.warn('[ЦУП] Превышено время ожидания ответа от базы данных.')
      }
    }, 30000)

    hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${window.location.origin}/ws/satellite`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    hubConnection.on('ReceivePosition', (data: SGP4DataDTO) => {
      clearDbTimeout()

      if (syncStatus.value !== 'success') {
        syncStatus.value = 'success'
      }

      updateSatelliteOnMap(data)
      resetHeartbeatTimeout()
    })

    hubConnection.onreconnecting(() => {
      clearDbTimeout()
      clearHeartbeatTimeout()
      syncStatus.value = 'reconnecting'
      connectionStatus.value = 'connecting'
      statusText.value = 'Reconnecting...'
      satelliteData.value = null
    })

    hubConnection.onclose(() => {
      clearDbTimeout()
      clearHeartbeatTimeout()
      syncStatus.value = 'error'
      connectionStatus.value = 'disconnected'
      statusText.value = 'Connection lost.'
      satelliteData.value = null
    })

    try {
      await hubConnection.start()
      connectionStatus.value = 'connected'
      statusText.value = 'Tracking is enabled'
      await hubConnection.invoke('WatchSatellite', id)
      resetHeartbeatTimeout()
    } catch (err) {
      clearDbTimeout()
      clearHeartbeatTimeout()
      syncStatus.value = 'error'
      connectionStatus.value = 'disconnected'
      statusText.value = 'Connection error. The server is temporarily unavailable.'
    }
  }

  // ДОПОЛНИТЕЛЬНЫЕ СПУТНИКИ
  interface ExtraRuntime {
    hub: signalR.HubConnection | null
    marker: L.Marker | null
    path: L.Polyline | null
    prevLng: number | null
    heartbeat: number | null
    disposed: boolean
  }

  const extraSats = ref<ExtraSat[]>([])
  const extraRuntime = new Map<number, ExtraRuntime>()

  const showAdd = ref(false)
  const addQuery = ref('')
  const addResults = ref<SatelliteFilterDTO[]>([])
  const addLoading = ref(false)
  const addMessage = ref('')
  const addWrapRef = ref<HTMLElement | null>(null)

  const MAX_EXTRA_SATS = 30
  const limitReached = computed(() => extraSats.value.length >= MAX_EXTRA_SATS)

  const isAlreadyTracked = (id: number) =>
    id === noradId.value || extraSats.value.some((s) => s.noradId === id)

  // Выбор нескольких спутников чекбоксами
  const selectedIds = ref<number[]>([])
  const slotsLeft = computed(() => MAX_EXTRA_SATS - extraSats.value.length)

  // Отметить можно только ещё не отслеживаемый спутник и не больше, чем осталось свободных мест
  const canToggle = (id: number) =>
    !isAlreadyTracked(id) && (selectedIds.value.includes(id) || selectedIds.value.length < slotsLeft.value)

  // Отметки сбрасываются при новом поиске (кнопка Search / Enter) и при закрытии меню.
  // Пока пользователь просто печатает в поле, отметки сохраняются.
  watch(showAdd, (open) => { if (!open) selectedIds.value = [] })

  const addSelectedSatellites = () => {
    const chosen = addResults.value.filter((r) => selectedIds.value.includes(r.noradId))
    selectedIds.value = []
    chosen.forEach((r) => { void addSatellite(r) })
  }

  const toggleAddMenu = () => {
    showAdd.value = !showAdd.value
  }

  const handleDocumentClick = (e: MouseEvent) => {
    if (showAdd.value && addWrapRef.value && !addWrapRef.value.contains(e.target as Node)) {
      showAdd.value = false
    }
  }

  const runAddSearch = async () => {
    const q = addQuery.value.trim()
    selectedIds.value = []
    addResults.value = []
    addMessage.value = ''
    if (q === '') return

    addLoading.value = true
    try {
      const isId = !isNaN(Number(q)) && !isNaN(parseFloat(q))
      const url = isId
        ? `/api/v1/GetDataById?noradId=${encodeURIComponent(q)}`
        : `/api/v1/GetDataByName?satelliteName=${encodeURIComponent(q)}`
      const response = await fetch(url)
      if (response.status === 404) {
        addMessage.value = 'Satellite not found'
      } else if (response.ok) {
        const data = await response.json()
        const list: SatelliteFilterDTO[] = Array.isArray(data) ? data : [data]
        addResults.value = list
        if (list.length === 0) addMessage.value = 'Satellite not found'
      } else {
        addMessage.value = 'Search error. Try again later'
      }
    } catch (err) {
      console.error(err)
      addMessage.value = 'Search error. Try again later'
    } finally {
      addLoading.value = false
    }
  }

  // Подписи спутников на карте
  // Берём уже известное имя спутника (из списка/GET-запроса), дополнительных запросов в БД нет.
  // Подпись привязана к маркеру (Leaflet tooltip), поэтому сама следует за точкой.
  const showNames = ref(false)    // чекбокс 1: показывать названия
  const showDetails = ref(false)  // чекбокс 2: добавить координаты и высоту (доступен после первого)
  const hoverOnly = ref(false)    // чекбокс 3: только при наведении (доступен после первого)

  // Данные для подписи берутся из того же состояния, что и таблицы под картой
  // (satelliteData / extraSats[].data), то есть приходят по уже открытому соединению.
  // Новых подписок и запросов нет, а цифры в подписи всегда совпадают с таблицей.
  type LabelData = { latitude: number; longitude: number; altitude: number }

  // Содержимое подписи: название (+ координаты и высота, если включено "Show detailed info")
  const buildLabel = (name: string, color: string, data?: LabelData | null): HTMLElement => {
    const root = document.createElement('div')

    const title = document.createElement('div')
    title.className = 'sat-tip-name'
    title.textContent = name
    title.style.color = color
    root.appendChild(title)

    if (showDetails.value && data) {
      const rows = [
        `Lat: ${data.latitude.toFixed(6)}°`,
        `Lon: ${data.longitude.toFixed(6)}°`,
        `Alt: ${data.altitude.toFixed(2)} km`
      ]
      for (const text of rows) {
        const row = document.createElement('div')
        row.className = 'sat-tip-info'
        row.textContent = text
        root.appendChild(row)
      }
    }
    return root
  }

  const applyLabel = (marker: L.Marker | null | undefined, name: string | null | undefined, color: string, data?: LabelData | null) => {
    if (!marker) return
    marker.unbindTooltip()
    if (!showNames.value || !name) return

    marker.bindTooltip(buildLabel(name, color, data), {
      permanent: !hoverOnly.value,
      direction: 'right',
      offset: [14, 0],
      className: 'sat-name-tooltip',
      opacity: 1
    })
  }

  // Обновить цифры в уже созданной подписи в момент прихода новых данных (как и таблицы)
  const refreshLabelContent = (marker: L.Marker | null | undefined, name: string | null | undefined, color: string, data: LabelData) => {
    if (!marker || !name || !showNames.value || !showDetails.value || !marker.getTooltip()) return
    marker.setTooltipContent(buildLabel(name, color, data))
  }

  const refreshAllLabels = () => {
    applyLabel(satelliteMarker, displayName.value, MAIN_SAT_COLOR, satelliteData.value)
    extraRuntime.forEach((rt, id) => {
      const it = extraSats.value.find((s) => s.noradId === id)
      applyLabel(rt.marker, it?.name, it?.color ?? MAIN_SAT_COLOR, it?.data)
    })
  }

  // Если сняли "Show satellite names", дополнительные режимы сбрасываются
  watch(showNames, (v) => {
    if (!v) {
      hoverOnly.value = false
      showDetails.value = false
    }
  })

  watch([showNames, showDetails, hoverOnly, displayName], refreshAllLabels)

  const applyExtraOnMap = (rt: ExtraRuntime, color: string, lat: number, lng: number) => {
    if (!map) return
    const pos = L.latLng(lat, lng)

    if (!rt.marker) {
      const icon = L.divIcon({
        className: 'custom-satellite-icon',
        html: `<div class="satellite-ping-wave colored" style="--sat-color:${color}"></div><div class="satellite-ping-core colored" style="--sat-color:${color}"></div>`,
        iconSize: [30, 30],
        iconAnchor: [15, 15]
      })
      rt.marker = L.marker(pos, { icon, interactive: true, keyboard: false, zIndexOffset: 900 }).addTo(map)
      refreshAllLabels()
      rt.path = L.polyline([], {
        color, weight: 3, opacity: 0.8, dashArray: '5, 5',
        smoothFactor: 2
      }).addTo(map)
    } else {
      rt.marker.setLatLng(pos)
    }

    if (rt.prevLng !== null && Math.abs(lng - rt.prevLng) > 180) {
      rt.path?.setLatLngs([])
    }
    rt.prevLng = lng
    rt.path?.addLatLng(pos)

  }

  const addSatellite = async (r: SatelliteFilterDTO) => {
    if (isAlreadyTracked(r.noradId)) return
    if (limitReached.value) return

    const id = r.noradId
    const color = pickDistinctColor(extraSats.value.map((s) => s.color))
    extraSats.value.push({ noradId: id, name: r.name, color, data: null, status: 'pending' })
    showAdd.value = false
    addQuery.value = ''
    addResults.value = []
    addMessage.value = ''

    const rt: ExtraRuntime = { hub: null, marker: null, path: null, prevLng: null, heartbeat: null, disposed: false }
    extraRuntime.set(id, rt)

    const getItem = () => extraSats.value.find((s) => s.noradId === id)

    const resetBeat = () => {
      if (rt.heartbeat) clearTimeout(rt.heartbeat)
      rt.heartbeat = window.setTimeout(() => {
        const it = getItem()
        if (it && it.status === 'success') it.status = 'pending'
      }, 4000)
    }

    const hub = new signalR.HubConnectionBuilder()
      .withUrl(`${window.location.origin}/ws/satellite`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()
    rt.hub = hub

    hub.on('ReceivePosition', (data: SGP4DataDTO) => {
      const it = getItem()
      if (!it) return
      it.status = 'success'

      // Таблица, точка, траектория и подпись обновляются вместе
      scheduleMapUpdate(() => {
        // Спутник мог быть удалён, пока обновление ждало в очереди
        if (rt.disposed) return
        const cur = getItem()
        if (!cur) return
        cur.data = data
        applyExtraOnMap(rt, color, data.latitude, data.longitude)
        refreshLabelContent(rt.marker, cur.name, color, data)
      })
      resetBeat()
    })

    hub.onreconnecting(() => {
      const it = getItem()
      if (it) { it.status = 'pending'; it.data = null }
    })

    hub.onclose(() => {
      const it = getItem()
      if (it) { it.status = 'error'; it.data = null }
    })

    try {
      await hub.start()
      await hub.invoke('WatchSatellite', id)
      resetBeat()
    } catch (err) {
      console.warn('[SignalR] Не удалось подключить доп. спутник:', err)
      const it = getItem()
      if (it) it.status = 'error'
    }
  }

  const disposeExtra = async (id: number) => {
    const rt = extraRuntime.get(id)
    if (!rt) return
    rt.disposed = true
    if (rt.heartbeat) clearTimeout(rt.heartbeat)
    rt.marker?.remove()
    rt.path?.remove()
    if (rt.hub) {
      try {
        rt.hub.off('ReceivePosition')
        await rt.hub.stop()
      } catch (err) {
        console.warn('[SignalR] Ошибка остановки доп. спутника:', err)
      }
    }
    extraRuntime.delete(id)
  }

  const removeExtraSat = async (id: number) => {
    extraSats.value = extraSats.value.filter((s) => s.noradId !== id)
    await disposeExtra(id)
  }

  const stopAllExtras = async () => {
    const ids = Array.from(extraRuntime.keys())
    extraSats.value = []
    await Promise.all(ids.map((id) => disposeExtra(id)))
  }

  onBeforeRouteLeave(async (to, from, next) => {
    await stopSignalR()
    next()
  })

  watch(
    () => route.params.satellite_id,
    (newId) => {
      if (newId) {
        const parsedId = parseInt(newId as string, 10)
        if (!isNaN(parsedId)) {
          startSatelliteTracking(parsedId)
        }
      }
    },
    { immediate: true }
  )

  // Обновляем title и description по displayName: имя приходит из GET-запроса,
  watch(
    displayName,
    (newName) => {
      if (newName) {
        // 1. Динамически меняем заголовок вкладки браузера
        document.title = `${newName} Satellite (NORAD ${noradId.value}) Live Tracking & Location | OrbitX`

        // 2. Обновляем мета-описание для поисковых роботов Google/Яндекс
        const metaDesc = document.querySelector('meta[name="description"]')
        if (metaDesc) {
          metaDesc.setAttribute('content',
            `Live tracking of ${newName} satellite (NORAD ID ${noradId.value}) on an interactive world map. View real-time coordinates, altitude, and SGP4 orbit tracking on OrbitX.`
          )
        }
      }
    },
    { immediate: true }
  )

  onMounted(() => {
    window.addEventListener('online', handleBrowserOnline)
    if (MAP_FLUSH_INTERVAL_MS > 0) {
      mapFlushTimer = window.setInterval(flushMapUpdates, MAP_FLUSH_INTERVAL_MS)
    }
    document.addEventListener('click', handleDocumentClick)
    window.addEventListener('offline', handleBrowserOffline)
  })

  onUnmounted(() => {
    window.removeEventListener('online', handleBrowserOnline)
    if (mapFlushTimer) { clearInterval(mapFlushTimer); mapFlushTimer = null }
    document.removeEventListener('click', handleDocumentClick)
    window.removeEventListener('offline', handleBrowserOffline)
    resizeObserver?.disconnect()
    stopSignalR()
    if (map) {
      map.remove()
    }
  })
</script>

<style scoped>
  .modeling-section {
    background-color: #1a1a1a;
    min-height: calc(100vh - 80px);
    padding: 40px 0;
  }

  /* ОСНОВНОЙ LAYOUT */
  .modeling-layout {
    display: flex;
    flex-direction: column;
    gap: 16px;
  }

  .map-column {
    width: 100%;
    min-width: 0;
  }

  .map-container-box {
    background-color: #141414;
    border: 1px solid #222222;
    border-radius: 16px;
    position: relative;
    overflow: hidden;
    width: 100%;
    /* высота адаптируется к экрану: не меньше 300px, не больше 760px */
    height: clamp(300px, 65vh, 760px);
    height: clamp(300px, 65dvh, 760px);
  }

  .real-map-element {
    width: 100%;
    height: 100%;
    opacity: 1 !important;
    visibility: visible !important;
  }

  /* Плашка загрузки карты — поверх карты, не сдвигает layout */
  .map-status-toast {
    position: absolute;
    left: 60px;
    top: 12px;
    z-index: 1000;
    background-color: rgba(20, 20, 20, 0.9);
    border: 1px solid rgba(234, 117, 162, 0.3);
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.4);
    padding: 8px 16px;
    border-radius: 30px;
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .toast-text {
    margin: 0;
    font-size: 12px;
    font-weight: 600;
    color: #e2e8f0;
    white-space: nowrap;
  }

  .toast-spinner {
    width: 14px;
    height: 14px;
    border: 2px solid rgba(234, 117, 162, 0.2);
    border-top-color: #ea75a2;
    border-radius: 50%;
    animation: toast-spin 0.8s linear infinite;
  }

  @keyframes toast-spin {
    to {
      transform: rotate(360deg);
    }
  }

  .extra-bars {
    order: 1;
    display: flex;
    flex-direction: column;
    gap: 16px;
  }

  /* Панель между картой и блоками спутников */
  .map-toolbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 16px;
    flex-wrap: wrap;
    position: relative;
    z-index: 1100;
  }

  .map-options {
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
  }

  /* Тёмный блок под каждый чекбокс — в стиле остальных карточек */
  .map-option {
    display: inline-flex;
    align-items: center;
    gap: 10px;
    height: 44px;
    padding: 0 16px;
    background-color: #141414;
    border: 1px solid #222222;
    border-radius: 12px;
    color: #e2e8f0;
    font-size: 14px;
    font-weight: 600;
    font-family: "Exo 2", sans-serif;
    cursor: pointer;
    user-select: none;
    transition: border-color 0.2s ease, background-color 0.2s ease;
  }

    .map-option:hover:not(.map-option-disabled) {
      border-color: #2d2d2d;
      background-color: #1a1a1a;
    }

    .map-option:has(input:checked) {
      border-color: rgba(234, 117, 162, 0.45);
    }

    /* Свой тёмный чекбокс с розовым акцентом */
    .map-option input {
      appearance: none;
      -webkit-appearance: none;
      width: 18px;
      height: 18px;
      margin: 0;
      flex-shrink: 0;
      background-color: #1e1e1e;
      border: 2px solid #3a3a3a;
      border-radius: 5px;
      cursor: pointer;
      display: grid;
      place-content: center;
      transition: background-color 0.2s ease, border-color 0.2s ease;
    }

      .map-option input::after {
        content: "";
        width: 8px;
        height: 4px;
        border-left: 2px solid #ffffff;
        border-bottom: 2px solid #ffffff;
        transform: rotate(-45deg) scale(0);
        margin-top: -2px;
        transition: transform 0.15s ease;
      }

      .map-option input:checked {
        background-color: #ea75a2;
        border-color: #ea75a2;
      }

        .map-option input:checked::after {
          transform: rotate(-45deg) scale(1);
        }

  .map-option-disabled {
    opacity: 0.45;
    cursor: not-allowed;
  }

    .map-option-disabled input {
      cursor: not-allowed;
    }

  /* Кнопка + выпадающий поиск добавления спутника */
  .add-sat-wrap {
    position: relative;
    z-index: 1100;
    margin-left: auto;
  }

  .add-sat-btn {
    height: 40px;
    padding: 0 18px;
    background-color: #ea75a2;
    border: 1px solid #ea75a2;
    border-radius: 8px;
    color: #ffffff;
    font-size: 14px;
    font-weight: 600;
    font-family: "Exo 2", sans-serif;
    cursor: pointer;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.4);
    transition: all 0.2s ease;
    touch-action: manipulation;
  }

    .add-sat-btn:hover,
    .add-sat-btn:active {
      background-color: #ec4899;
      border-color: #ec4899;
    }

  .add-sat-menu {
    position: absolute;
    right: 0;
    bottom: calc(100% + 8px);
    z-index: 1100;
    width: 320px;
    max-width: calc(100vw - 48px);
    background-color: #141414;
    border: 1px solid #222222;
    border-radius: 12px;
    padding: 10px;
    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.5);
  }

  .add-sat-search-row {
    display: flex;
    gap: 8px;
    align-items: center;
  }

  .add-sat-search-btn {
    height: 40px;
    padding: 0 16px;
    flex-shrink: 0;
    background-color: #ea75a2;
    color: #ffffff;
    border: none;
    border-radius: 8px;
    font-size: 14px;
    font-weight: 700;
    font-family: "Exo 2", sans-serif;
    cursor: pointer;
    transition: background-color 0.2s ease;
  }

    .add-sat-search-btn:hover:not(:disabled),
    .add-sat-search-btn:active:not(:disabled) {
      background-color: #f08db4;
    }

    .add-sat-search-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

  .add-sat-input {
    flex: 1;
    min-width: 0;
    width: 100%;
    height: 40px;
    background-color: #1e1e1e;
    border: 2px solid #2d2d2d;
    border-radius: 8px;
    padding: 0 12px;
    color: #ffffff;
    font-size: 14px;
    outline: none;
    font-family: "Exo 2", sans-serif;
    box-sizing: border-box;
  }

    .add-sat-input:focus {
      border-color: #ea75a2;
    }

  .add-sat-list {
    margin-top: 8px;
    max-height: 220px;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
    gap: 4px;
  }

  .add-sat-hint {
    color: #64748b;
    font-size: 13px;
    padding: 10px 6px;
    text-align: center;
  }

  .add-sat-item {
    display: flex;
    gap: 12px;
    align-items: center;
    text-align: left;
    width: 100%;
    padding: 8px 10px;
    background-color: #1e1e1e;
    border: 1px solid #2d2d2d;
    border-radius: 8px;
    color: #e2e8f0;
    font-size: 13px;
    font-family: "Exo 2", sans-serif;
    cursor: pointer;
    transition: all 0.2s ease;
  }

    .add-sat-item:hover:not(:disabled) {
      border-color: #ea75a2;
    }

    .add-sat-item:disabled {
      opacity: 0.4;
      cursor: default;
    }

    .add-sat-item:has(input:checked) {
      border-color: #ea75a2;
      background-color: #261a20;
    }

  .add-sat-item-disabled {
    opacity: 0.4;
    cursor: default;
  }

  /* Тёмный чекбокс с розовым акцентом */
  .add-sat-check {
    appearance: none;
    -webkit-appearance: none;
    width: 18px;
    height: 18px;
    margin: 0;
    flex-shrink: 0;
    background-color: #141414;
    border: 2px solid #3a3a3a;
    border-radius: 5px;
    cursor: pointer;
    display: grid;
    place-content: center;
    transition: background-color 0.2s ease, border-color 0.2s ease;
  }

    .add-sat-check::after {
      content: "";
      width: 8px;
      height: 4px;
      border-left: 2px solid #ffffff;
      border-bottom: 2px solid #ffffff;
      transform: rotate(-45deg) scale(0);
      margin-top: -2px;
      transition: transform 0.15s ease;
    }

    .add-sat-check:checked {
      background-color: #ea75a2;
      border-color: #ea75a2;
    }

      .add-sat-check:checked::after {
        transform: rotate(-45deg) scale(1);
      }

    .add-sat-check:disabled {
      cursor: default;
    }

  .add-sat-confirm-btn {
    width: 100%;
    height: 40px;
    margin-top: 8px;
    background-color: #ea75a2;
    color: #ffffff;
    border: none;
    border-radius: 8px;
    font-size: 14px;
    font-weight: 700;
    font-family: "Exo 2", sans-serif;
    cursor: pointer;
    transition: background-color 0.2s ease;
  }

    .add-sat-confirm-btn:hover,
    .add-sat-confirm-btn:active {
      background-color: #f08db4;
    }

  .add-sat-item-id {
    color: #94a3b8;
    min-width: 56px;
  }

  .add-sat-item-name {
    font-weight: 600;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  /* ТЕЛЕФОН */
  @media (max-width: 560px) {
    .map-toolbar {
      flex-direction: column;
      align-items: stretch;
    }

    .add-sat-wrap {
      margin-left: 0;
    }

    .add-sat-btn {
      width: 100%;
    }

    .modeling-section {
      padding: 16px 0;
    }

    .map-container-box {
      height: clamp(260px, 55dvh, 460px);
      border-radius: 12px;
    }
  }
</style>

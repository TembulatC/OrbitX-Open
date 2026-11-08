<template>
  <section class="modeling-section">
    <div class="container">

      <!-- ВЕРХНИЙ БЛОК: ИНФОРМАЦИЯ О ТЕКУЩЕМ СПУТНИКЕ -->
      <div class="modeling-header-block">
        <div class="header-block-flex-wrapper">

          <h1 class="modeling-subtitle" style="margin: 0;">
            Current device:
            <span class="accent-text name-accent" v-if="displayName">{{ displayName }}</span>
            <span class="accent-text" v-else>Defining the object...</span>
            | NORAD ID: <span class="accent-id">{{ noradId }}</span>
          </h1>

          <!-- ИНДИКАТОР ПОДКЛЮЧЕНИЙ -->
          <div class="sync-status-badge" :class="(syncStatus === 'error' || syncStatus === 'db_error') ? 'badge-error' : `badge-${syncStatus}`">
            <span class="status-dot" :class="(syncStatus === 'error' || syncStatus === 'db_error') ? 'dot-error' : `dot-${syncStatus}`"></span>
            <p class="status-badge-text">
              <template v-if="syncStatus === 'pending'">
                Connecting...
              </template>
              <template v-else-if="syncStatus === 'success'">
                Tracking is enabled
              </template>
              <template v-else-if="syncStatus === 'reconnecting'">
                Connection lost. Please restart tracking
              </template>
              <template v-else-if="syncStatus === 'error'">
                Connection error. The server is temporarily unavailable.
              </template>
              <template v-else-if="syncStatus === 'db_error'">
                Connection error. The server is temporarily unavailable.
              </template>
            </p>
          </div>

        </div>
      </div>

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
                <button v-for="r in addResults"
                        :key="r.noradId"
                        type="button"
                        class="add-sat-item"
                        :disabled="isAlreadyTracked(r.noradId)"
                        @click="addSatellite(r)">
                  <span class="add-sat-item-id">{{ r.noradId }}</span>
                  <span class="add-sat-item-name">{{ r.name }}</span>
                </button>
              </div>
            </div>
            <button type="button" class="add-sat-btn" :disabled="limitReached" :style="limitReached ? 'opacity:0.5;cursor:not-allowed' : ''" @click.stop="toggleAddMenu">+ Add Satellite ({{ extraSats.length }}/{{ MAX_EXTRA_SATS }})</button>
          </div>
        </div>

        <!-- ДОПОЛНИТЕЛЬНЫЕ СПУТНИКИ -->
        <div class="extra-bars">
          <div class="telemetry-bar" v-for="s in extraSats" :key="s.noradId">
            <div class="sat-name-row extra-name-row">
              <span class="sat-name" :style="{ color: s.color }" :title="s.name">{{ s.name }}</span>
              <button type="button" class="extra-remove" title="Remove satellite" @click="removeExtraSat(s.noradId)">×</button>
            </div>

            <div class="telemetry-card">
              <span class="telemetry-label">NORAD ID</span>
              <span class="telemetry-value norad-row">
                {{ s.noradId }}
                <span class="marker-dot" :style="{ '--sat-color': s.color }"><span class="marker-dot-wave colored"></span><span class="marker-dot-core colored"></span></span>
              </span>
            </div>

            <div class="telemetry-card">
              <span class="telemetry-label">Latitude</span>
              <span class="telemetry-value" v-if="s.data">{{ s.data.latitude?.toFixed(6) }}°</span>
              <span class="telemetry-value loading-value" v-else>--.------</span>
            </div>

            <div class="telemetry-card">
              <span class="telemetry-label">Longitude</span>
              <span class="telemetry-value" v-if="s.data">{{ s.data.longitude?.toFixed(6) }}°</span>
              <span class="telemetry-value loading-value" v-else>--.------</span>
            </div>

            <div class="telemetry-card">
              <span class="telemetry-label">Altitude</span>
              <span class="telemetry-value" :style="{ color: s.color }" v-if="s.data">{{ s.data.altitude?.toFixed(2) }} km</span>
              <span class="telemetry-value loading-value" v-else>---.--</span>
            </div>

            <div class="telemetry-card">
              <span class="telemetry-label">Marker</span>
              <span class="telemetry-value marker-value">
                <span class="frequency-indicator"
                      :class="s.status === 'success' ? 'indicator-success' : 'indicator-error blink-dot'"></span>
                {{ s.status === 'success' ? '~1 / sec' : '~0 / sec' }}
              </span>
            </div>
          </div>
        </div>

        <!-- ГОРИЗОНТАЛЬНАЯ ПОЛОСА ТЕЛЕМЕТРИИ -->
        <div class="telemetry-bar">

          <!-- НАЗВАНИЕ СПУТНИКА: внутри полосы, над карточкой NORAD ID -->
          <div class="sat-name-row">
            <span class="sat-name" :title="displayName || ''">{{ displayName || 'Defining the object...' }}</span>
          </div>

          <!-- NORAD ID -->
          <div class="telemetry-card">

            <span class="telemetry-label">NORAD ID</span>
            <span class="telemetry-value norad-row">
              {{ noradId || '-----' }}
              <span class="marker-dot"><span class="marker-dot-wave"></span><span class="marker-dot-core"></span></span>
            </span>
          </div>

          <!-- Широта -->
          <div class="telemetry-card">
            <span class="telemetry-label">Latitude</span>
            <span class="telemetry-value" v-if="satelliteData">{{ satelliteData.latitude?.toFixed(6) }}°</span>
            <span class="telemetry-value loading-value" v-else>--.------</span>
          </div>

          <!-- Долгота -->
          <div class="telemetry-card">
            <span class="telemetry-label">Longitude</span>
            <span class="telemetry-value" v-if="satelliteData">{{ satelliteData.longitude?.toFixed(6) }}°</span>
            <span class="telemetry-value loading-value" v-else>--.------</span>
          </div>

          <!-- Высота -->
          <div class="telemetry-card">
            <span class="telemetry-label">Altitude</span>
            <span class="telemetry-value value-alt" v-if="satelliteData">{{ satelliteData.altitude?.toFixed(2) }} km</span>
            <span class="telemetry-value loading-value" v-else>---.--</span>
          </div>

          <!-- Частота обновления -->
          <div class="telemetry-card">
            <span class="telemetry-label">Marker</span>
            <span class="telemetry-value marker-value">
              <span class="frequency-indicator"
                    :class="syncStatus === 'success' ? 'indicator-success' : 'indicator-error blink-dot'"></span>
              {{ syncStatus === 'success' ? '~1 / sec' : '~0 / sec' }}
            </span>
          </div>

        </div>

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

  interface SGP4DataDTO {
    noradId: number
    name: string
    longitude: number
    latitude: number
    altitude: number
  }

  const route = useRoute()

  const noradId = ref<number>(0)
  const satelliteData = shallowRef<SGP4DataDTO | null>(null)

  // Имя спутника из обычного GET-запроса (GetDataById).
  // Приходит сразу при загрузке страницы, не зависит от WebSocket и не пропадает при его обрыве
  const satelliteName = ref<string | null>(null)

  // Итоговое имя для отображения: сначала из сокета (если уже пришло), иначе из GET-запроса
  const displayName = computed(() => satelliteData.value?.name ?? satelliteName.value)

  const syncStatus = ref<'pending' | 'success' | 'reconnecting' | 'error' | 'db_error'>('pending')
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

  // Ответ GetDataById: список SatellitesFilterDTO (поля noradId и name)
  interface SatelliteFilterDTO {
    noradId: number
    name: string
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

  // ── Пакетное обновление карты ──
  // Позиции приходят по разным соединениям в разные моменты секунды. Чтобы все точки на карте
  // показывали согласованный момент времени и карта не перерисовывалась десятки раз в секунду,
  // обновления копятся в очереди и применяются разом. Ни одна точка не теряется: каждая
  // полученная координата попадает и на маркер, и в траекторию, просто с задержкой до одного такта.
  const MAP_FLUSH_INTERVAL_MS = 1000
  const pendingMapUpdates: Array<() => void> = []
  let mapFlushTimer: number | null = null

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

  const updateSatelliteOnMap = (lat: number, lng: number) => {
    pendingMapUpdates.push(() => applySatelliteOnMap(lat, lng))
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
      satelliteData.value = data

      if (syncStatus.value !== 'success') {
        syncStatus.value = 'success'
      }

      updateSatelliteOnMap(data.latitude, data.longitude)
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
  interface ExtraSat {
    noradId: number
    name: string
    color: string
    data: SGP4DataDTO | null
    status: 'pending' | 'success' | 'error'
  }

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
  const showNames = ref(false)   // чекбокс 1: показывать названия
  const hoverOnly = ref(false)   // чекбокс 2: только при наведении (доступен после первого)

  const MAIN_SAT_COLOR = '#ea75a2'

  const applyLabel = (marker: L.Marker | null | undefined, name: string | null | undefined, color: string) => {
    if (!marker) return
    marker.unbindTooltip()
    if (!showNames.value || !name) return

    const el = document.createElement('span')
    el.textContent = name
    el.style.color = color

    marker.bindTooltip(el, {
      permanent: !hoverOnly.value,
      direction: 'right',
      offset: [14, 0],
      className: 'sat-name-tooltip',
      opacity: 1
    })
  }

  const refreshAllLabels = () => {
    applyLabel(satelliteMarker, displayName.value, MAIN_SAT_COLOR)
    extraRuntime.forEach((rt, id) => {
      const it = extraSats.value.find((s) => s.noradId === id)
      applyLabel(rt.marker, it?.name, it?.color ?? MAIN_SAT_COLOR)
    })
  }

  // Если сняли "Show satellite names", режим "только при наведении" сбрасывается
  watch(showNames, (v) => {
    if (!v) hoverOnly.value = false
  })

  watch([showNames, hoverOnly, displayName], refreshAllLabels)

  // Оттенок основного спутника (#ea75a2 ≈ hue 338) — новые цвета не должны быть на него похожи
  const MAIN_SAT_HUE = 338

  // Кратчайшее расстояние между двумя оттенками на цветовом круге (0–180°)
  const hueDistance = (a: number, b: number): number => {
    const d = Math.abs(a - b) % 360
    return d > 180 ? 360 - d : d
  }

  const hueOf = (color: string): number => {
    const m = /hsl\(\s*(\d+(?:\.\d+)?)/.exec(color)
    return m && m[1] !== undefined ? parseFloat(m[1]) : MAIN_SAT_HUE
  }

  // Яркий цвет, максимально далёкий по оттенку от уже занятых (включая основной спутник).
  // Из всех кандидатов берётся тот, у которого расстояние до ближайшего занятого оттенка максимально.
  // Освободившиеся при удалении спутника оттенки автоматически становятся доступны снова.
  const randomColor = (): string => {
    const usedHues = [MAIN_SAT_HUE, ...extraSats.value.map((s) => hueOf(s.color))]

    let bestHue = 0
    let bestDist = -1
    for (let h = 0; h < 360; h += 2) {
      const d = Math.min(...usedHues.map((u) => hueDistance(h, u)))
      // Небольшой случайный разброс, чтобы при равных расстояниях цвета не повторялись по шаблону
      if (d + Math.random() * 0.5 > bestDist) {
        bestDist = d
        bestHue = h
      }
    }

    const sat = 75 + Math.floor(Math.random() * 26) // 75–100%
    const lig = 52 + Math.floor(Math.random() * 14) // 52–65%
    return `hsl(${bestHue}, ${sat}%, ${lig}%)`
  }

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

  const updateExtraOnMap = (rt: ExtraRuntime, color: string, lat: number, lng: number) => {
    pendingMapUpdates.push(() => {
      // Спутник мог быть удалён, пока обновление ждало в очереди
      if (!rt.disposed) applyExtraOnMap(rt, color, lat, lng)
    })
  }

  const addSatellite = async (r: SatelliteFilterDTO) => {
    if (isAlreadyTracked(r.noradId)) return
    if (limitReached.value) return

    const id = r.noradId
    const color = randomColor()
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
      it.data = data
      it.status = 'success'
      updateExtraOnMap(rt, color, data.latitude, data.longitude)
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
    mapFlushTimer = window.setInterval(flushMapUpdates, MAP_FLUSH_INTERVAL_MS)
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

  .modeling-header-block {
    background-color: #141414;
    border: 1px solid #222222;
    border-radius: 16px;
    padding: 24px;
    margin-bottom: 30px;
  }

  .header-block-flex-wrapper {
    display: flex;
    justify-content: space-between;
    align-items: center;
    width: 100%;
  }

  .modeling-subtitle {
    color: #94a3b8;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.5px;
  }

  .accent-text {
    color: #ffffff;
    text-transform: none;
  }

  .name-accent {
    color: #ea75a2;
  }

  .accent-id {
    color: #ea75a2;
    font-family: monospace;
    font-size: 20px;
    font-weight: 700;
    background-color: rgba(234, 117, 162, 0.05);
    padding: 2px 8px;
    border-radius: 4px;
    border: 1px solid rgba(234, 117, 162, 0.15);
  }

  .sync-status-badge {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 6px 14px;
    border-radius: 20px;
    min-height: 32px;
    transition: all 0.3s ease;
  }

  .badge-pending {
    background-color: rgba(148, 163, 184, 0.02);
    border: 1px solid rgba(148, 163, 184, 0.12);
  }

    .badge-pending .status-badge-text {
      color: #94a3b8;
    }

  .badge-success {
    background-color: rgba(34, 197, 94, 0.03);
    border: 1px solid rgba(34, 197, 94, 0.15);
  }

    .badge-success .status-badge-text {
      color: #22c55e;
    }

  .badge-reconnecting {
    background-color: rgba(234, 179, 8, 0.03);
    border: 1px solid rgba(234, 179, 8, 0.15);
  }

    .badge-reconnecting .status-badge-text {
      color: #eab308;
    }

  .badge-error {
    background-color: rgba(239, 68, 68, 0.03);
    border: 1px solid rgba(239, 68, 68, 0.15);
  }

    .badge-error .status-badge-text {
      color: #ef4444;
    }

  .status-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
  }

  .dot-pending {
    background-color: #94a3b8;
  }

  .dot-success {
    background-color: #22c55e;
  }

  .dot-reconnecting {
    background-color: #eab308;
  }

  .dot-error {
    background-color: #ef4444;
  }

  .status-badge-text {
    font-size: 13px;
    font-weight: 600;
    margin: 0;
    white-space: nowrap;
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

  .telemetry-bar {
    display: grid;
    grid-template-columns: 0.8fr 1.2fr 1.2fr 1fr 1fr;
    gap: 12px;
    background-color: #141414;
    border: 1px solid #222222;
    border-radius: 16px;
    padding: 14px;
  }

  .telemetry-card {
    background-color: #1e1e1e;
    border: 1px solid #2d2d2d;
    border-radius: 12px;
    padding: 12px 16px;
    display: flex;
    flex-direction: column;
    gap: 6px;
    min-width: 0;
  }

  .telemetry-label {
    font-size: 11px;
    font-weight: 700;
    color: #64748b;
    text-transform: uppercase;
    letter-spacing: 0.5px;
  }

  .telemetry-value {
    color: #ffffff;
    font-size: clamp(14px, 1.5vw, 22px);
    font-weight: 700;
    font-family: monospace;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .sat-name-row {
    grid-column: 1 / -1;
    padding: 0 4px;
    min-width: 0;
  }

  .sat-name {
    display: block;
    color: #ea75a2;
    font-size: 16px;
    font-weight: 700;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .norad-row {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  /* Точка-копия маркера на карте: показывает, какой точке принадлежит спутник */
  .marker-dot {
    position: relative;
    width: 18px;
    height: 18px;
    flex-shrink: 0;
  }

  .marker-dot-core {
    position: absolute;
    top: 50%;
    left: 50%;
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background-color: #ea75a2;
    box-shadow: 0 0 8px #ea75a2;
    transform: translate(-50%, -50%);
  }

  .marker-dot-wave {
    position: absolute;
    top: 50%;
    left: 50%;
    width: 18px;
    height: 18px;
    border: 2px solid #ea75a2;
    border-radius: 50%;
    transform: translate(-50%, -50%) scale(0.75);
    opacity: 0.35;
  }


  .marker-dot-core.colored {
    background-color: var(--sat-color);
    box-shadow: 0 0 8px var(--sat-color);
  }

  .marker-dot-wave.colored {
    border-color: var(--sat-color);
  }

  .extra-bars {
    order: 1;
    display: flex;
    flex-direction: column;
    gap: 16px;
  }

  .extra-name-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 10px;
  }

  .extra-remove {
    background: none;
    border: none;
    color: #64748b;
    font-size: 20px;
    line-height: 1;
    cursor: pointer;
    padding: 4px 8px;
    touch-action: manipulation;
    transition: color 0.2s ease;
  }

    .extra-remove:hover {
      color: #ffffff;
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

  .marker-value {
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .value-alt {
    color: #ea75a2;
  }

  .loading-value {
    color: #3a3a3a;
  }

  .frequency-indicator {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    flex-shrink: 0;
    transition: background-color 0.3s ease;
  }

  .indicator-success {
    background-color: #22c55e;
  }

  .indicator-error {
    background-color: #ef4444;
  }


  /* ПЛАНШЕТ: 3 + 2 ячейки */
  @media (max-width: 992px) {
    .header-block-flex-wrapper {
      flex-direction: column;
      align-items: flex-start;
      gap: 12px;
    }

    .sync-status-badge {
      max-width: 100%;
    }

    .status-badge-text {
      white-space: normal;
    }

    .telemetry-bar {
      grid-template-columns: repeat(6, 1fr);
    }

    .sat-name-row {
      grid-column: 1 / -1;
    }

    .telemetry-card {
      grid-column: span 2;
    }

      .telemetry-card:nth-child(5),
      .telemetry-card:nth-child(6) {
        grid-column: span 3;
      }
  }

  /* ТЕЛЕФОН: 2 колонки */
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

    .modeling-header-block {
      padding: 14px;
      margin-bottom: 16px;
    }

    .modeling-subtitle {
      font-size: 14px;
    }

    .accent-id {
      font-size: 16px;
    }

    .map-container-box {
      height: clamp(260px, 55dvh, 460px);
      border-radius: 12px;
    }

    .telemetry-bar {
      grid-template-columns: 1fr 1fr;
      padding: 10px;
      gap: 8px;
    }

    .telemetry-card,
    .telemetry-card:nth-child(5),
    .telemetry-card:nth-child(6) {
      grid-column: auto;
      padding: 10px 12px;
    }

      /* NORAD ID на всю ширину */
      .telemetry-card:nth-child(2) {
        grid-column: 1 / -1;
      }

    .telemetry-value {
      font-size: 15px;
    }
  }
</style>

<!-- Отдельный неизолированный тег для стилизации элементов Leaflet -->
<style>
  @import "leaflet/dist/leaflet.css";

  /* Базовый цвет подложки — черный */
  .leaflet-container {
    background-color: #000000 !important;
  }

  /* Один фильтр на весь слой тайлов вместо отдельного фильтра на каждой плитке — заметно дешевле */
  .leaflet-tile-pane {
    filter: invert(100%) hue-rotate(180deg) brightness(95%) contrast(90%) saturate(30%);
  }

  /* Сокрытие строки копирайта */
  .leaflet-control-attribution {
    display: none !important;
  }

  /* Тёмные кнопки зума */
  .leaflet-control-zoom a {
    color: #ffffff !important;
    display: flex !important;
    align-items: center !important;
    justify-content: center !important;
    text-decoration: none !important;
    background-color: #141414 !important;
    border: 1px solid #222222 !important;
    border-bottom: none !important;
    touch-action: manipulation; /* Быстрый отклик на тап без задержки под двойной тап-зум */
  }

    .leaflet-control-zoom a:last-child {
      border-bottom: 1px solid #222222 !important;
    }

    .leaflet-control-zoom a:hover,
    .leaflet-control-zoom a:active {
      background-color: #1e1e1e !important;
    }

  /* На телефонах увеличиваем кнопки +/- зума */
  @media (max-width: 768px) {
    .leaflet-control-zoom a {
      width: 40px !important;
      height: 40px !important;
      line-height: 40px !important;
      font-size: 20px !important;
    }
  }

  /* Цветной маркер дополнительного спутника */
  .satellite-ping-core.colored {
    background-color: var(--sat-color);
    box-shadow: 0 0 10px var(--sat-color);
  }

  .satellite-ping-wave.colored {
    border-color: var(--sat-color);
  }

  /* Подпись с названием спутника */
  .sat-name-tooltip {
    background-color: #141414;
    border: 1px solid #2d2d2d;
    border-radius: 6px;
    padding: 2px 8px;
    font-family: "Exo 2", sans-serif;
    font-size: 12px;
    font-weight: 700;
    white-space: nowrap;
    box-shadow: 0 4px 14px rgba(0, 0, 0, 0.5);
  }

    .sat-name-tooltip::before {
      display: none;
    }

  /* Маркер спутника */
  .custom-satellite-icon {
    background: transparent !important;
    border: none !important;
    position: absolute;
  }

  /* Сама точка: цвет как у линии траектории */
  .satellite-ping-core {
    width: 12px;
    height: 12px;
    background-color: #ea75a2;
    border-radius: 50%;
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    z-index: 5;
    box-shadow: 0 0 10px #ea75a2;
  }

  /* Расходящаяся волна вокруг точки */
  .satellite-ping-wave {
    width: 30px;
    height: 30px;
    border: 2px solid #ea75a2;
    border-radius: 50%;
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%) scale(0.7);
    opacity: 0.35;
    pointer-events: none;
  }
</style>

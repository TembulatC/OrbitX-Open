import * as signalR from '@microsoft/signalr'
import type { SGP4DataDTO } from '../types/satellite'

export type HubState = 'connecting' | 'connected' | 'reconnecting' | 'closed'

// Имена событий и методов хаба на сервере
const POSITION_EVENT = 'ReceivePosition'
const WATCH_METHOD = 'WatchSatellite'     // (int noradId, string category)
const UNWATCH_METHOD = 'UnwatchSatellite' // (int noradId, string category)

interface Subscription {
  category: string
  onPosition: (data: SGP4DataDTO) => void
}

// Одно соединение SignalR на весь клиент.
// Каждый спутник — это подписка внутри этого соединения, а не отдельный вебсокет.
// Входящие позиции раздаются по noradId из самого сообщения.
export const createSatelliteHub = (url: string) => {
  const subscriptions = new Map<number, Subscription>()
  const stateListeners = new Set<(state: HubState) => void>()

  let connection: signalR.HubConnection | null = null
  let starting: Promise<void> | null = null

  const emit = (state: HubState) => stateListeners.forEach((cb) => cb(state))

  const ensureConnection = (): signalR.HubConnection => {
    if (connection) return connection

    const c = new signalR.HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    // Сервер может прислать как одну позицию, так и массив позиций
    // Приводим к массиву и раздаём каждую позицию тому, кто подписан на этот noradId
    c.on(POSITION_EVENT, (payload: SGP4DataDTO | SGP4DataDTO[]) => {
      const items = Array.isArray(payload) ? payload : [payload]
      for (const data of items) {
        if (data) subscriptions.get(data.noradId)?.onPosition(data)
      }
    })

    c.onreconnecting(() => {
      if (connection === c) emit('reconnecting')
    })

    // После переподключения у соединения новый ID и сервер забывает подписки, поэтому подписываемся на все спутники заново (с их категориями)
    c.onreconnected(async () => {
      if (connection !== c) return
      for (const [id, sub] of subscriptions) {
        try {
          await c.invoke(WATCH_METHOD, id, sub.category)
        } catch (err) {
          console.warn(`[SignalR] Не удалось восстановить подписку на ${id}:`, err)
        }
      }
      emit('connected')
    })

    c.onclose(() => {
      if (connection === c) emit('closed')
    })

    connection = c
    return c
  }

  // Открывает соединение, если оно ещё не открыто. Одновременные вызовы делят один запуск
  const connect = async (): Promise<void> => {
    const c = ensureConnection()
    if (starting) return starting
    if (c.state === signalR.HubConnectionState.Disconnected) {
      emit('connecting')
      starting = c
        .start()
        .then(() => emit('connected'))
        .finally(() => { starting = null })
      return starting
    }
  }

  // Подписаться на спутник (в текущем соединении)
  // Вызывается при каждом добавлении спутника, в том числе при заходе на страницу (первый спутник)
  const watch = async (id: number, category: string, onPosition: (data: SGP4DataDTO) => void) => {
    subscriptions.set(id, { category, onPosition })
    try {
      const c = ensureConnection()
      await connect()
      // Если соединение сейчас переподключается, подписка восстановится в onreconnected
      if (c.state === signalR.HubConnectionState.Connected) {
        await c.invoke(WATCH_METHOD, id, category)
      }
    } catch (err) {
      // Подписка не удалась: не храним её, иначе она тихо восстановилась бы после переподключения
      subscriptions.delete(id)
      throw err
    }
  }

  // Пользователь нажал крестик: всегда сообщаем серверу, соединение остаётся открытым
  const unwatch = async (id: number) => {
    const sub = subscriptions.get(id)
    subscriptions.delete(id)
    const c = connection
    if (!sub || !c || c.state !== signalR.HubConnectionState.Connected) return
    try {
      await c.invoke(UNWATCH_METHOD, id, sub.category)
    } catch (err) {
      console.warn(`[SignalR] Не удалось отписаться от ${id}:`, err)
    }
  }

  // Полностью закрыть соединение. Сервер сам вызовет OnDisconnectedAsync и снимет все подписки
  const disconnect = async () => {
    const c = connection
    subscriptions.clear()
    connection = null
    starting = null
    if (!c) return
    try {
      c.off(POSITION_EVENT)
      await c.stop()
    } catch (err) {
      console.warn('[SignalR] Ошибка при остановке соединения:', err)
    }
  }

  const onStateChange = (cb: (state: HubState) => void) => {
    stateListeners.add(cb)
    return () => stateListeners.delete(cb)
  }

  return { watch, unwatch, disconnect, onStateChange }
}

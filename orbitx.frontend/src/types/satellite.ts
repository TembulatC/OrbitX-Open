// Данные позиции спутника, приходящие по SignalR (событие ReceivePosition)
export interface SGP4DataDTO {
  noradId: number
  name: string
  longitude: number
  latitude: number
  altitude: number
}

// Элемент ответа GetDataById / GetDataByName
export interface SatelliteFilterDTO {
  noradId: number
  name: string
}

// Состояние подключения основного спутника
export type SyncStatus = 'pending' | 'success' | 'reconnecting' | 'error' | 'db_error'

// Дополнительный спутник, добавленный пользователем
export interface ExtraSat {
  noradId: number
  name: string
  color: string
  data: SGP4DataDTO | null
  status: 'pending' | 'success' | 'error'
}

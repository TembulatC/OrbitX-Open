<template>
  <div class="telemetry-bar">
    <!-- Название спутника -->
    <div class="sat-name-row" :class="{ 'extra-name-row': removable }">
      <span class="sat-name" :style="color ? { color } : undefined" :title="name || ''">{{ name || 'Defining the object...' }}</span>
      <button v-if="removable" type="button" class="extra-remove" title="Remove satellite" @click="emit('remove')">×</button>
    </div>

    <!-- NORAD ID -->
    <div class="telemetry-card">
      <span class="telemetry-label">NORAD ID</span>
      <span class="telemetry-value norad-row">
        {{ noradId || '-----' }}
        <span class="marker-dot" :style="color ? { '--sat-color': color } : undefined">
          <span class="marker-dot-wave" :class="{ colored: !!color }"></span>
          <span class="marker-dot-core" :class="{ colored: !!color }"></span>
        </span>
      </span>
    </div>

    <!-- Широта -->
    <div class="telemetry-card">
      <span class="telemetry-label">Latitude</span>
      <span class="telemetry-value" v-if="data">{{ data.latitude?.toFixed(6) }}°</span>
      <span class="telemetry-value loading-value" v-else>--.------</span>
    </div>

    <!-- Долгота -->
    <div class="telemetry-card">
      <span class="telemetry-label">Longitude</span>
      <span class="telemetry-value" v-if="data">{{ data.longitude?.toFixed(6) }}°</span>
      <span class="telemetry-value loading-value" v-else>--.------</span>
    </div>

    <!-- Высота -->
    <div class="telemetry-card">
      <span class="telemetry-label">Altitude</span>
      <span class="telemetry-value"
            :class="{ 'value-alt': !color }"
            :style="color ? { color } : undefined"
            v-if="data">{{ data.altitude?.toFixed(2) }} km</span>
      <span class="telemetry-value loading-value" v-else>---.--</span>
    </div>

    <!-- Частота обновления -->
    <div class="telemetry-card">
      <span class="telemetry-label">Marker</span>
      <span class="telemetry-value marker-value">
        <span class="frequency-indicator" :class="isLive ? 'indicator-success' : 'indicator-error'"></span>
        {{ isLive ? '~1 / sec' : '~0 / sec' }}
      </span>
    </div>
  </div>
</template>

<script setup lang="ts">
  import type { SGP4DataDTO } from '../../../types/satellite'

  // Полоса телеметрии одного спутника. Без color выглядит как основной спутник с color — как дополнительный (цвет точки/линии), а с removable показывает кнопку удаления.
  defineProps<{
    name: string | null
    noradId: number | null
    data: SGP4DataDTO | null
    isLive: boolean
    color?: string
    removable?: boolean
  }>()

  const emit = defineEmits<{
    (e: 'remove'): void
  }>()
</script>

<style scoped>
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

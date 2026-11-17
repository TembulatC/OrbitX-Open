<template>
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
</template>

<script setup lang="ts">
  import type { SyncStatus } from '../../../types/satellite'

  // Верхняя плашка: какой спутник отслеживается и в каком состоянии соединение
  defineProps<{
    displayName: string | null
    noradId: number
    syncStatus: SyncStatus
  }>()
</script>

<style scoped>
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
  }

  @media (max-width: 560px) {
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
  }
</style>

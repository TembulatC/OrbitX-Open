<template>
  <section class="donate-section">
    <div class="container donate-container">

      <!-- ЗАГОЛОВОК И ПОДЗАГОЛОВОК -->
      <div class="donate-cta">
        <h1 class="donate-title">Support <span class="accent-text">OrbitX</span></h1>
        <p class="donate-subtitle">
          Any amount helps the project stay connected and continue to grow.
        </p>
      </div>

      <!-- ДИСКЛЕЙМЕР О ПРОЕКТЕ И НАЗНАЧЕНИИ ДОНАТОВ -->
      <div class="disclaimer-box">
        <p>
          OrbitX is an independent, nonprofit space monitoring platform created to promote astronomy and support students, scientists, and space enthusiasts.
        </p>
        <p>
          The platform has no investors and no intrusive advertising. The project is driven solely by the creator's enthusiasm and the support of the community.
        </p>
        <p>All funds transferred are voluntary donations made without compensation. They are used for:</p>

        <ul class="disclaimer-list">
          <li>Support for the project's creator and motivation for further development;</li>
          <li>Payment for servers and computing resources;</li>
          <li>Development of future phases of the roadmap (telemetry, geomagnetic tracking, and a smooth 3D globe).</li>
        </ul>

        <p class="disclaimer-note">
          By making a donation, you are supporting an independent developer. The income is officially reported in accordance with Russian Federation law governing self-employed individuals.
        </p>
      </div>

      <!-- КНОПКА ОТКРЫТИЯ ФОРМЫ ПЕРЕВОДА -->
      <div class="donate-action">
        <button type="button" class="btn-donate" @click="isDonateOpen = true">
          Support Project
        </button>
      </div>

    </div>

    <!-- МОДАЛЬНОЕ ОКНО С ФОРМОЙ ПЕРЕВОДА (ВИДЖЕТ ЮMONEY) -->
    <div v-if="isDonateOpen" class="modal-overlay" @click.self="isDonateOpen = false">
      <div class="modal-box donate-modal-box">
        <div class="modal-header">
          <h3 class="modal-title">Donation to Support the Project</h3>
          <button type="button" @click="isDonateOpen = false" class="modal-close-btn" aria-label="Close">×</button>
        </div>

        <div class="modal-body">
          <div class="donate-iframe-wrapper">
            <iframe src="https://yoomoney.ru/quickpay/fundraise/widget?billNumber=1KHM2LRVO2K.260926&" width="500" height="480" frameborder="0" allowtransparency="true" scrolling="no"></iframe>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
  import { ref, watch } from 'vue'

  // Управляет видимостью модального окна с виджетом перевода
  const isDonateOpen = ref(false)

  watch(isDonateOpen, (isOpen) => {
    if (isOpen) {
      const scrollbarWidth = window.innerWidth - document.documentElement.clientWidth
      document.body.style.overflow = 'hidden'
      document.body.style.paddingRight = `${scrollbarWidth}px`
    } else {
      document.body.style.overflow = ''
      document.body.style.paddingRight = ''
    }
  })
</script>

<style scoped>
  .donate-section {
    background-color: #1a1a1a;
    min-height: calc(100vh - 80px);
    padding: 80px 0;
  }

  .donate-container {
    max-width: 800px;
    margin: 0 auto;
  }

  /* Блок дисклеймера */
  .disclaimer-box {
    background-color: #141414;
    border: 1px solid #222222;
    border-radius: 16px;
    padding: 32px;
    color: #94a3b8;
    font-size: 15px;
    line-height: 1.7;
  }

    .disclaimer-box p {
      margin-bottom: 16px;
    }

      .disclaimer-box p:last-child {
        margin-bottom: 0;
      }

  .disclaimer-list {
    list-style: none;
    margin: 0 0 16px 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 10px;
  }

    .disclaimer-list li {
      position: relative;
      padding-left: 22px;
    }

      .disclaimer-list li::before {
        content: '';
        position: absolute;
        left: 0;
        top: 9px;
        width: 6px;
        height: 6px;
        border-radius: 50%;
        background-color: #ea75a2;
      }

  .disclaimer-note {
    color: #64748b;
    font-size: 13px;
    font-style: italic;
    border-top: 1px solid #222222;
    padding-top: 16px;
  }

  .donate-cta {
    text-align: center;
    margin-bottom: 40px;
  }

  .donate-title {
    font-size: clamp(28px, 5vw, 40px);
    font-weight: 800;
    color: #ffffff;
    margin-bottom: 16px;
  }

  .accent-text {
    color: #ea75a2;
  }

  .donate-subtitle {
    font-size: clamp(15px, 2.2vw, 17px);
    color: #94a3b8;
    line-height: 1.6;
    margin-bottom: 0;
  }

  /* Обертка кнопки внизу страницы */
  .donate-action {
    margin-top: 40px;
    text-align: center;
  }

  .btn-donate {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    padding: 16px 40px;
    background-color: #ea75a2;
    border: 1px solid #ea75a2;
    border-radius: 10px;
    color: #ffffff;
    font-size: 16px;
    font-weight: 700;
    font-family: 'Exo 2', sans-serif;
    cursor: pointer;
    transition: all 0.2s ease;
    touch-action: manipulation;
  }

    .btn-donate:hover,
    .btn-donate:active {
      background-color: #ec4899;
      border-color: #ec4899;
    }

    .btn-donate:active {
      transform: scale(0.97);
    }

  .modal-overlay {
    position: fixed;
    inset: 0;
    background-color: rgba(0, 0, 0, 0.6);
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 24px calc(20px + env(safe-area-inset-right, 0px)) calc(24px + env(safe-area-inset-bottom, 0px)) calc(20px + env(safe-area-inset-left, 0px));
    z-index: 1000;
  }

  .modal-box {
    width: 100%;
    max-height: 100%;
    overflow-y: auto;
    -webkit-overflow-scrolling: touch;
    background-color: #141414;
    border: 1px solid #222222;
    border-radius: 16px;
    padding: 28px;
    box-shadow: 0 20px 40px rgba(0, 0, 0, 0.6);
  }

  .donate-modal-box {
    max-width: 560px;
  }

  .modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
    border-bottom: 1px solid #222222;
    padding-bottom: 12px;
  }

  .modal-title {
    color: #ffffff;
    font-size: 18px;
    font-weight: 700;
  }

  .modal-close-btn {
    background: none;
    border: none;
    color: #64748b;
    font-size: 24px;
    line-height: 1;
    cursor: pointer;
    padding: 8px;
    margin: -8px -8px -8px 0;
    touch-action: manipulation;
    transition: color 0.2s;
  }

    .modal-close-btn:hover,
    .modal-close-btn:active {
      color: #ffffff;
    }

  /* Виджет ЮMoney */
  .donate-iframe-wrapper {
    display: block;
    width: max-content;
    max-width: 100%;
    margin: 0 auto;
    transform: translateX(13px);
    overflow-x: auto;
    -webkit-overflow-scrolling: touch;
    border-radius: 12px;
  }

  /* Адаптация под планшеты и мобильные */
  @media (max-width: 768px) {
    .donate-section {
      padding: 48px 0;
    }

    .disclaimer-box {
      padding: 22px 20px;
      font-size: 14px;
    }

    .donate-cta {
      margin-bottom: 28px;
    }

    .donate-action {
      margin-top: 28px;
    }

    .btn-donate {
      width: 100%;
    }

    .modal-box {
      padding: 20px;
      border-radius: 16px;
    }
  }

  @media (max-width: 480px) {
    .donate-section {
      padding: 36px 0;
    }

    .disclaimer-box {
      padding: 18px 16px;
    }
  }
</style>

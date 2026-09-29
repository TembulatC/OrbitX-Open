<template>
  <section class="not-found-section">
    <div class="container not-found-container">

      <!-- Декоративный "потерянный спутник" — просто CSS, без картинок -->
      <div class="lost-satellite" aria-hidden="true">
        <div class="satellite-body">
          <div class="satellite-panel satellite-panel-left"></div>
          <div class="satellite-core"></div>
          <div class="satellite-panel satellite-panel-right"></div>
          <div class="satellite-antenna"></div>
        </div>
        <div class="orbit-ring"></div>
        <span class="star star-1"></span>
        <span class="star star-2"></span>
        <span class="star star-3"></span>
        <span class="star star-4"></span>
      </div>

      <p class="error-code">404</p>
      <h1 class="not-found-title">Сигнал потерян</h1>
      <p class="not-found-subtitle">
        Похоже, этот объект сошёл с орбиты — такой страницы не существует
        или она была перемещена.
      </p>

      <div class="not-found-actions">
        <router-link to="/" class="btn-home">На главную</router-link>
        <router-link to="/satellites_modeling" class="btn-catalog">К каталогу спутников</router-link>
      </div>

    </div>
  </section>
</template>

<style scoped>
  .not-found-section {
    background-color: #1a1a1a;
    min-height: calc(100vh - 80px);
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 60px 0;
  }

  .not-found-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    text-align: center;
    max-width: 560px;
  }

  .lost-satellite {
    position: relative;
    width: 180px;
    height: 180px;
    margin-bottom: 28px;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .orbit-ring {
    position: absolute;
    inset: 0;
    border: 2px dashed rgba(234, 117, 162, 0.35);
    border-radius: 50%;
    animation: spin 18s linear infinite;
  }

  .satellite-body {
    position: relative;
    display: flex;
    align-items: center;
    gap: 4px;
    animation: drift 4s ease-in-out infinite;
    transform: rotate(-18deg);
  }

  .satellite-core {
    width: 26px;
    height: 26px;
    background-color: #e2e8f0;
    border-radius: 6px;
    box-shadow: 0 0 18px rgba(234, 117, 162, 0.5);
    z-index: 1;
  }

  .satellite-panel {
    width: 34px;
    height: 16px;
    background: linear-gradient(135deg, #ea75a2, #6d8fea);
    border: 1px solid rgba(255, 255, 255, 0.25);
    border-radius: 2px;
  }

  .satellite-antenna {
    position: absolute;
    top: -14px;
    left: 50%;
    width: 2px;
    height: 14px;
    background-color: #94a3b8;
  }

    .satellite-antenna::before {
      content: '';
      position: absolute;
      top: -3px;
      left: -2px;
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background-color: #ea75a2;
    }

  .star {
    position: absolute;
    width: 3px;
    height: 3px;
    border-radius: 50%;
    background-color: #ffffff;
    opacity: 0.6;
    animation: twinkle 2.4s ease-in-out infinite;
  }

  .star-1 {
    top: 10%;
    left: 15%;
    animation-delay: 0s;
  }

  .star-2 {
    top: 20%;
    right: 10%;
    animation-delay: 0.6s;
  }

  .star-3 {
    bottom: 15%;
    left: 8%;
    animation-delay: 1.2s;
  }

  .star-4 {
    bottom: 25%;
    right: 15%;
    animation-delay: 1.8s;
  }

  @keyframes spin {
    from {
      transform: rotate(0deg);
    }

    to {
      transform: rotate(360deg);
    }
  }

  @keyframes drift {
    0%, 100% {
      transform: rotate(-18deg) translateY(0);
    }

    50% {
      transform: rotate(-14deg) translateY(-8px);
    }
  }

  @keyframes twinkle {
    0%, 100% {
      opacity: 0.2;
    }

    50% {
      opacity: 0.9;
    }
  }

  .error-code {
    font-size: clamp(56px, 12vw, 88px);
    font-weight: 800;
    line-height: 1;
    color: transparent;
    background: linear-gradient(135deg, #ea75a2, #6d8fea);
    -webkit-background-clip: text;
    background-clip: text;
    margin-bottom: 8px;
  }

  .not-found-title {
    font-size: clamp(22px, 4vw, 30px);
    font-weight: 700;
    color: #ffffff;
    margin-bottom: 14px;
  }

  .not-found-subtitle {
    font-size: clamp(14px, 2vw, 16px);
    color: #94a3b8;
    line-height: 1.6;
    margin-bottom: 36px;
  }

  .not-found-actions {
    display: flex;
    gap: 16px;
    flex-wrap: wrap;
    justify-content: center;
  }

  .btn-home,
  .btn-catalog {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    padding: 14px 28px;
    border-radius: 10px;
    font-size: 15px;
    font-weight: 600;
    text-decoration: none;
    font-family: 'Exo 2', sans-serif;
    transition: all 0.2s ease;
    touch-action: manipulation;
  }

  .btn-home {
    background-color: #ea75a2;
    border: 1px solid #ea75a2;
    color: #ffffff;
  }

    .btn-home:hover,
    .btn-home:active {
      background-color: #ec4899;
      border-color: #ec4899;
    }

  .btn-catalog {
    background-color: transparent;
    border: 2px solid #2d2d2d;
    color: #ffffff;
  }

    .btn-catalog:hover,
    .btn-catalog:active {
      border-color: #ea75a2;
      color: #ea75a2;
    }

    .btn-home:active,
    .btn-catalog:active {
      transform: scale(0.97);
    }

  /* Адаптация под мобильные */
  @media (max-width: 480px) {
    .not-found-section {
      padding: 40px 0;
    }

    .lost-satellite {
      width: 140px;
      height: 140px;
      margin-bottom: 20px;
    }

    .not-found-actions {
      flex-direction: column;
      width: 100%;
    }

    .btn-home,
    .btn-catalog {
      width: 100%;
    }
  }

  /* Отключаем декоративную анимацию */
  @media (prefers-reduced-motion: reduce) {
    .orbit-ring,
    .satellite-body,
    .star {
      animation: none;
    }
  }
</style>

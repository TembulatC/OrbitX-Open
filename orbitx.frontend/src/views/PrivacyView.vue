<template>
  <div class="privacy-container">
    <h1 class="privacy-main-title">Privacy Policy</h1>

    <div class="privacy-content">

      <!-- Раздел 1 -->
      <div class="privacy-section">
        <h3 class="privacy-section-title">
          <span class="brand-pink-accent">1.</span> General Provisions
        </h3>
        <p class="privacy-text">
          The OrbitX platform (hereinafter referred to as the “Service”) ensures complete transparency in its data processing procedures. This document outlines the Service’s policy regarding the collection, processing, and storage of information. We highly value the trust of our users and guarantee the absolute security of their digital space.
        </p>
      </div>

      <!-- Раздел 2 -->
      <div class="privacy-section">
        <h3 class="privacy-section-title">
          <span class="brand-pink-accent">2.</span> Collection and Processing of Space Data
        </h3>
        <p class="privacy-text">
          The OrbitX server core interacts exclusively with open, publicly available, and declassified sources of ballistic information. We perform real-time processing of orbital elements in the
          <span class="highlight-text-white">OMM (Orbit Mean-Elements Message)</span> format, which complies with the
          <span class="highlight-text-white">CCSDS</span> international standard. All ballistic telemetry, as well as the calculation of position vectors and geodetic coordinates (latitude, longitude, altitude), are modeled in real time using the open-source
          <span class="highlight-text-white">SGP4</span> algorithm.
        </p>
      </div>

      <!-- Раздел 3 -->
      <div class="privacy-section">
        <h3 class="privacy-section-title">
          <span class="brand-pink-accent">3.</span> No Collection of Personal Data
        </h3>
        <p class="privacy-text">
          The service adheres to a strict data minimization policy. Using the radar and viewing live satellite telemetry do not require creating an account or entering personal information. The service does not collect, request, or store your personal data, including your name, email address, phone numbers, or payment information. We do not store users’ IP addresses or their search history in our database.
        </p>
      </div>

      <!-- Раздел 4 -->
      <div class="privacy-section">
        <h3 class="privacy-section-title">
          <span class="brand-pink-accent">4.</span> Web Sockets and Network Protocols
        </h3>
        <p class="privacy-text">
          To provide a live stream of coordinates every second, the Service uses the asynchronous
          <span class="brand-pink-accent font-bold">WebSockets</span> protocol. This connection is used exclusively to transmit calculated ballistic vectors to your screen. Streaming socket sessions are anonymous, temporary, and are automatically terminated when the browser tab is closed.
        </p>
      </div>

      <!-- Раздел 5 -->
      <div class="privacy-section privacy-section-last">
        <h3 class="privacy-section-title">
          <span class="brand-pink-accent">5.</span> Policy Changes
        </h3>
        <p class="privacy-text">
          Since OrbitX is constantly evolving, this policy is subject to change. The most current version is always available on this page.
        </p>
      </div>

    </div>
  </div>
</template>

<style scoped>
  .privacy-container {
    padding: 120px 20px; /* Добавили боковые отступы */
    max-width: 800px;
    color: #94a3b8; /* Спокойный серый цвет для основного текста */
    font-family: 'Exo 2', sans-serif;
    margin: 0 auto; /* Выравнивание по центру экрана */
  }

  .privacy-main-title {
    color: #ffffff;
    margin-bottom: 32px;
    font-weight: 700;
    font-size: clamp(26px, 5vw, 36px);
  }

  .privacy-content {
    line-height: 1.8;
    font-size: 16px;
  }

  .privacy-section {
    margin-bottom: 28px;
  }

  .privacy-section-last {
    margin-bottom: 12px;
  }

  .privacy-section-title {
    color: #ffffff;
    margin-bottom: 8px;
    font-weight: 600;
  }

  .brand-pink-accent {
    color: #ea75a2;
  }

  .font-bold {
    font-weight: bold;
  }

  .privacy-text {
    margin: 0;
  }

  .highlight-text-white {
    color: #ffffff;
    font-weight: bold;
  }

  /* Адаптация под планшеты и мобильные — уменьшаем "воздух" сверху/снизу */
  @media (max-width: 768px) {
    .privacy-container {
      padding: 72px 20px;
    }

    .privacy-content {
      font-size: 15px;
      line-height: 1.7;
    }
  }

  @media (max-width: 480px) {
    .privacy-container {
      padding: 56px 16px;
    }
  }
</style>

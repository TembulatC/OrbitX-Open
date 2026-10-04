<template>
  <div class="terms-container">
    <h1 class="terms-main-title">Terms of Use</h1>

    <div class="terms-content">

      <!-- Раздел 1 -->
      <div class="terms-section">
        <h3 class="terms-section-title">
          <span class="accent-number">1.</span> Intended Use of the Platform
        </h3>
        <p class="terms-text">
          Use of the OrbitX platform is permitted solely for informational,
          <span class="highlight-text-white">personal</span>, research, and educational purposes. The service provides free access to the results of SGP4-tracking of orbital constellations based on open-source ballistic data.
        </p>
      </div>

      <!-- Раздел 2 -->
      <div class="terms-section">
        <h3 class="terms-section-title">
          <span class="accent-number">2.</span> Prohibition on Automated Data Collection (Scraping)
        </h3>
        <p class="terms-text">
          Automated data collection, information aggregation, page parsing, or scraping of ballistic elements without prior approval from the project administration is <span class="highlight-text-white">strictly prohibited</span>.
          Violation of this rule is considered unauthorized use of the service’s infrastructure resources.
        </p>
      </div>

      <!-- Раздел 3 -->
      <div class="terms-section">
        <h3 class="terms-section-title">
          <span class="accent-number">3.</span> Restrictions on Load Testing and APIs
        </h3>
        <p class="terms-text">
          It is prohibited to take any actions intended to destabilize the server infrastructure (including DoS/DDoS attacks, the intentional execution of unauthorized load-testing scripts for WebSocket connections, and erratic API calls).
          <span class="highlight-text-white">Rate limiting mechanisms</span> are built into the Service. If abnormal traffic is detected, access to the Service from a specific network address is automatically blocked.
        </p>
      </div>

      <!-- Раздел 4 -->
      <div class="terms-section">
        <h3 class="terms-section-title">
          <span class="accent-number">4.</span> Disclaimer
        </h3>
        <p class="terms-text">
          Ballistic data and satellite coordinates are generated based on mathematical modeling and the OMM CCSDS public catalogs.
          The Service Administration assumes no responsibility for the absolute accuracy or continuity of the data stream, or for any material or technical risks associated with the use of the transmitted telemetry in mission-critical industrial or navigation systems.
        </p>
      </div>

      <!-- Раздел 5 -->
      <div class="terms-section">
        <h3 class="terms-section-title">
          <span class="accent-number">5.</span> Intellectual Property
        </h3>
        <p class="terms-text">
          The OrbitX architectural solution, including the worker code, custom web interface design, logos, and graphic elements, is the intellectual property of the project’s developers.
          Citing or displaying the codebase in research repositories is permitted only with a reference to the original source.
        </p>
      </div>

      <!-- Раздел 6 -->
      <div class="terms-section terms-section-last">
        <h3 class="terms-section-title">
          <span class="accent-number">6.</span> Feedback and Collaboration
        </h3>
        <p class="terms-text">
          If you need OrbitX data for scientific research or integration into third-party projects, please contact us via the Support form or by email to discuss specific terms.
          We are open to collaborating with third-party developers and scientific organizations.
        </p>
      </div>

      <!-- Динамическая оговорка об изменении правил -->
      <div class="terms-disclaimer">
        The Administration reserves the right to modify these Terms at any time without prior notice to users. The current version is always available on this page.
      </div>

    </div>
  </div>
</template>

<style scoped>
  .terms-container {
    padding: 120px 20px; /* Добавили боковые отступы */
    max-width: 800px;
    color: #94a3b8;
    font-family: 'Exo 2', sans-serif;
    margin: 0 auto; /* Центрирование контейнера на странице */
  }

  .terms-main-title {
    color: #ffffff;
    margin-bottom: 32px;
    font-weight: 700;
    font-size: clamp(26px, 5vw, 36px);
  }

  .terms-content {
    line-height: 1.8;
    font-size: 16px;
  }

  .terms-section {
    margin-bottom: 28px;
  }

  .terms-section-last {
    margin-bottom: 32px;
  }

  .terms-section-title {
    color: #ffffff;
    margin-bottom: 8px;
    font-weight: 600;
  }

  .accent-number {
    color: #ea75a2;
  }

  .terms-text {
    margin: 0;
  }

  .highlight-text-white {
    color: #ffffff;
    font-weight: bold;
  }

  .terms-disclaimer {
    border-top: 1px solid #1e293b;
    padding-top: 20px;
    font-style: italic;
    color: #64748b;
    font-size: 14px;
  }

  /* Адаптация под планшеты и мобильные — уменьшаем "воздух" сверху/снизу */
  @media (max-width: 768px) {
    .terms-container {
      padding: 72px 20px;
    }

    .terms-content {
      font-size: 15px;
      line-height: 1.7;
    }
  }

  @media (max-width: 480px) {
    .terms-container {
      padding: 56px 16px;
    }
  }
</style>

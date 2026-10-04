import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import SatelliteCatalogModelingView from '../views/satellites/SatelliteCatalogModelingView.vue'
import PrivacyView from '../views/PrivacyView.vue'
import TermsView from '../views/TermsView.vue'

const routes = [
  {
    path: '/',
    name: 'home',
    component: HomeView
  },
  {
    path: '/satellites-modeling',
    name: 'satellites-catalog',
    component: SatelliteCatalogModelingView
  },
  {
    path: '/satellites-modeling/:satellite_id',
    name: 'satellites-modeling',
    component: () => import('../views/satellites/SatelliteModelingView.vue')
  },
  {
    path: '/privacy',
    name: 'privacy',
    component: PrivacyView
  },
  {
    path: '/terms',
    name: 'terms',
    component: TermsView
  },
  {
    path: '/donate',
    name: 'donate',
    component: () => import('../views/DonateView.vue')
  },
  {
    // Ловит любой несуществующий адрес и показывает страницу 404
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('../views/NotFoundView.vue')
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.afterEach((to) => {
  let title = 'OrbitX — Real-time online satellite monitoring, simulation, and tracking'
  let description = 'An interactive radar for tracking satellite flight paths in real time. Online calculation of orbital coordinates using the SGP4 ballistic model.'

  switch (to.name) {
    case 'home':
      title = 'OrbitX — Real-time online satellite monitoring, simulation, and tracking'
      description = 'An interactive radar for tracking satellite flight paths in real time. Online calculation of orbital coordinates using the SGP4 ballistic model.'
      break
    case 'satellites-catalog':
      title = 'Earth Satellite Catalog: SGP4 Orbit Tracking | OrbitX'
      description = 'A list of artificial Earth satellites, filtered by category. Search for spacecraft by NORAD ID and view a real-time interactive flight map.'
      break
    case 'donate':
      title = 'Support the OrbitX Project: Donations and Service Development'
      description = 'You can support the independent OrbitX service. We would appreciate any voluntary donations or contributions via YooMoney to help us develop our online platform.'
      break
    case 'privacy':
      title = 'Privacy Policy | OrbitX Service'
      description = 'Privacy Policy and Personal Data Protection Policy for OrbitX Platform Users. Rules for the Collection, Processing, and Secure Storage of Information.'
      break
    case 'terms':
      title = 'Terms of Use | OrbitX Service'
      description = 'OrbitX Terms of Service and Rules of Use. Rights and Obligations of the Parties, Disclaimer, and Contact Information.'
      break
    case 'not-found':
      title = 'Signal Lost (Error 404) | OrbitX'
      description = 'The requested orbital coordinate or page was not found in the OrbitX database.'
      break
    case 'satellites-modeling':
      // Проверяем: если в URL передан ID спутника,
      // мы стопаем выполнение роутера. На этой странице теги настроит сам компонент трекера.
      if (to.params.satellite_id) {
        return
      }
      break
  }

  // Применяем изменения к тегам в DOM-дереве
  document.title = title

  const metaDesc = document.querySelector('meta[name="description"]')
  if (metaDesc) {
    metaDesc.setAttribute('content', description)
  }
})

export default router

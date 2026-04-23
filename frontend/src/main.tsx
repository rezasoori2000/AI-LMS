import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'

// Initialize i18n before rendering — must be imported first
import '@/i18n'
import '@/index.css'

import App from '@/app/App'

const rootElement = document.getElementById('root')
if (!rootElement) {
  throw new Error('Root element #root not found in index.html')
}

createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>
)

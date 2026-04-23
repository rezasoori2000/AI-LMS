import { RouterProvider } from 'react-router-dom'
import { AppProviders } from './providers'
import { router } from '@/routes'

/**
 * App root — composes providers + router.
 * Add new global providers inside AppProviders, not here.
 */
export default function App() {
  return (
    <AppProviders>
      <RouterProvider router={router} />
    </AppProviders>
  )
}

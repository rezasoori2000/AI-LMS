import { type ReactNode } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { AuthProvider } from '@/context/AuthContext'

/**
 * Shared QueryClient instance.
 * Configured with sensible defaults for an educational platform:
 * - staleTime: 5 min — lesson/curriculum data doesn't change frequently
 * - retry: 1 — don't hammer the server on failure
 */
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
})

interface AppProvidersProps {
  children: ReactNode
}

/**
 * AppProviders wraps the app with all global context providers.
 * Add new providers here in order of dependency (outermost = least dependent).
 *
 * Current providers:
 * - QueryClientProvider (TanStack Query — server state)
 * - AuthProvider       (Section 3 Part 4 — auth state + session)
 *
 * Future providers to add here:
 * - TenantProvider (Phase 3)
 * - ThemeProvider (Phase 4 — tenant branding)
 */
export function AppProviders({ children }: AppProvidersProps) {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        {children}
        {import.meta.env.DEV && <ReactQueryDevtools initialIsOpen={false} />}
      </AuthProvider>
    </QueryClientProvider>
  )
}

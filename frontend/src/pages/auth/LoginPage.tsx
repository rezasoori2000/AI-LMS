import { useTranslation } from 'react-i18next'
import { useNavigate, useLocation } from 'react-router-dom'
import { LoginForm }       from '@/components/auth/LoginForm'
import { useAuth }         from '@/context/AuthContext'
import type { AuthApiResponse } from '@/types'

/**
 * LoginPage — renders the login form inside AuthLayout's <Outlet />.
 *
 * After a successful login:
 * 1. Calls AuthContext.signIn() to persist the token and set user state.
 * 2. Navigates to the page the user originally tried to visit (`state.from`),
 *    or falls back to '/' when there is no return destination.
 */
export default function LoginPage() {
  const { t }      = useTranslation()
  const { signIn } = useAuth()
  const navigate   = useNavigate()
  const location   = useLocation()

  function handleSuccess(response: AuthApiResponse) {
    signIn(response)
    // Navigate back to the page that triggered the redirect, or home
    const from = (location.state as { from?: string } | null)?.from ?? '/'
    navigate(from, { replace: true })
  }

  return (
    <>
      <h2 className="mb-6 text-xl font-semibold text-content-primary text-center">
        {t('auth.login')}
      </h2>
      <LoginForm onSuccess={handleSuccess} />
    </>
  )
}

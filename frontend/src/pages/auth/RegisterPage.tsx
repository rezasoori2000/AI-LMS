import { useTranslation } from 'react-i18next'
import { useNavigate }    from 'react-router-dom'
import { RegisterForm }   from '@/components/auth/RegisterForm'
import { useAuth }        from '@/context/AuthContext'
import type { AuthApiResponse } from '@/types'

/**
 * RegisterPage — renders the registration form inside AuthLayout's <Outlet />.
 *
 * After a successful registration:
 * 1. Calls AuthContext.signIn() to persist the token and set user state.
 * 2. Navigates to '/' — new users always start at the home/dashboard.
 */
export default function RegisterPage() {
  const { t }      = useTranslation()
  const { signIn } = useAuth()
  const navigate   = useNavigate()

  function handleSuccess(response: AuthApiResponse) {
    signIn(response)
    navigate('/', { replace: true })
  }

  return (
    <>
      <h2 className="mb-6 text-xl font-semibold text-content-primary text-center">
        {t('auth.register')}
      </h2>
      <RegisterForm onSuccess={handleSuccess} />
    </>
  )
}

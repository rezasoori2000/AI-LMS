import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'

import { Input }          from '@/components/ui/Input'
import { Button }         from '@/components/ui/Button'
import { InlineFeedback } from '@/components/feedback/InlineFeedback'
import { loginUser }      from '@/services/auth.service'
import { mapAuthError }   from '@/utils/mapAuthError'
import type { LoginPayload, AuthApiResponse } from '@/types'

// ── Validation ────────────────────────────────────────────────────────────────

interface LoginErrors {
  email?:    string
  password?: string
}

function validate(form: LoginPayload, t: (k: string) => string): LoginErrors {
  const errors: LoginErrors = {}

  if (!form.email.trim()) {
    errors.email = t('auth.validation.emailRequired')
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) {
    errors.email = t('auth.validation.emailInvalid')
  }

  if (!form.password) {
    errors.password = t('auth.validation.passwordRequired')
  }

  return errors
}

// ── Props ─────────────────────────────────────────────────────────────────────

interface LoginFormProps {
  /**
   * Called after a successful login with the API response.
   * Section 3 Part 4 (AuthContext) will use this to store the token and
   * redirect the user to their dashboard.
   */
  onSuccess?: (response: AuthApiResponse) => void
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * LoginForm — handles the login form state, validation, API call, and UX feedback.
 *
 * Deliberately has no awareness of routing or token storage so it stays
 * testable and reusable. The parent page passes an onSuccess callback that
 * will wire in AuthContext in Part 4.
 *
 * Localization: all visible strings come from the 'auth' i18n namespace.
 * RTL: no directional CSS — inherits document dir from the root <html> element.
 */
export function LoginForm({ onSuccess }: LoginFormProps) {
  const { t } = useTranslation()

  const [form, setForm] = useState<LoginPayload>({ email: '', password: '' })
  const [errors, setErrors]       = useState<LoginErrors>({})
  const [submitting, setSubmitting] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)
  const [success, setSuccess]         = useState(false)

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const { name, value } = e.target
    setForm(prev => ({ ...prev, [name]: value }))
    // Clear per-field error on change so the user sees immediate feedback
    setErrors(prev => ({ ...prev, [name]: undefined }))
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setServerError(null)
    setSuccess(false)

    const fieldErrors = validate(form, t)
    if (Object.keys(fieldErrors).length > 0) {
      setErrors(fieldErrors)
      return
    }

    setSubmitting(true)
    try {
      const response = await loginUser(form)
      setSuccess(true)
      onSuccess?.(response)
    } catch (err) {
      setServerError(mapAuthError(err, 'login', t))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="space-y-4">
      <Input
        label={t('auth.email')}
        name="email"
        type="email"
        autoComplete="email"
        value={form.email}
        onChange={handleChange}
        error={errors.email}
        required
        disabled={submitting}
      />

      <Input
        label={t('auth.password')}
        name="password"
        type="password"
        autoComplete="current-password"
        value={form.password}
        onChange={handleChange}
        error={errors.password}
        required
        disabled={submitting}
      />

      {serverError && (
        <InlineFeedback intent="error" message={serverError} />
      )}

      {success && (
        <InlineFeedback intent="success" message={t('auth.loginSuccess')} />
      )}

      <Button
        type="submit"
        variant="primary"
        size="md"
        isLoading={submitting}
        className="w-full"
      >
        {submitting ? t('auth.loggingIn') : t('auth.login')}
      </Button>

      <p className="text-center text-sm text-content-secondary">
        {t('auth.noAccount')}{' '}
        <Link to="/auth/register" className="text-brand-600 hover:underline font-medium">
          {t('auth.register')}
        </Link>
      </p>
    </form>
  )
}

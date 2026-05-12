import { useState, useId, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'

import { Input }          from '@/components/ui/Input'
import { Button }         from '@/components/ui/Button'
import { InlineFeedback } from '@/components/feedback/InlineFeedback'
import { registerUser }   from '@/services/auth.service'
import { mapAuthError }   from '@/utils/mapAuthError'
import {
  BackendUserRole,
  REGISTER_ROLE_OPTIONS,
  type RegisterPayload,
  type AuthApiResponse,
  type BackendUserRoleValue,
} from '@/types'

// ── Validation ────────────────────────────────────────────────────────────────

interface RegisterErrors {
  firstName?:       string
  lastName?:        string
  email?:           string
  password?:        string
  confirmPassword?: string
  role?:            string
}

interface RegisterFormValues {
  firstName:       string
  lastName:        string
  email:           string
  password:        string
  confirmPassword: string
  role:            BackendUserRoleValue | ''
}

function validate(form: RegisterFormValues, t: (k: string) => string): RegisterErrors {
  const errors: RegisterErrors = {}

  if (!form.firstName.trim()) errors.firstName = t('auth.validation.firstNameRequired')
  if (!form.lastName.trim())  errors.lastName  = t('auth.validation.lastNameRequired')

  if (!form.email.trim()) {
    errors.email = t('auth.validation.emailRequired')
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) {
    errors.email = t('auth.validation.emailInvalid')
  }

  if (!form.password) {
    errors.password = t('auth.validation.passwordRequired')
  } else if (form.password.length < 8) {
    errors.password = t('auth.validation.passwordMinLength')
  }

  if (!form.confirmPassword) {
    errors.confirmPassword = t('auth.validation.confirmPasswordRequired')
  } else if (form.password !== form.confirmPassword) {
    errors.confirmPassword = t('auth.validation.passwordsNoMatch')
  }

  if (form.role === '') errors.role = t('auth.validation.roleRequired')

  return errors
}

// ── Props ─────────────────────────────────────────────────────────────────────

interface RegisterFormProps {
  /**
   * Called after a successful registration with the API response.
   * Section 3 Part 4 (AuthContext) will use this to store the token and
   * redirect the user to their dashboard.
   */
  onSuccess?: (response: AuthApiResponse) => void
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * RegisterForm — handles register form state, validation, API call, and UX feedback.
 *
 * Role dropdown limited to Student / Parent for self-registration (Phase 1).
 * Backend enforces the same restriction in Phase 3+ via invite-only for admin/teacher.
 *
 * Localization: all visible strings from 'auth' i18n namespace.
 * RTL: inherits document dir; no directional CSS used.
 */
export function RegisterForm({ onSuccess }: RegisterFormProps) {
  const { t } = useTranslation()
  const roleSelectId = useId()

  const [form, setForm] = useState<RegisterFormValues>({
    firstName:       '',
    lastName:        '',
    email:           '',
    password:        '',
    confirmPassword: '',
    role:            BackendUserRole.Student,
  })
  const [errors, setErrors]             = useState<RegisterErrors>({})
  const [submitting, setSubmitting]     = useState(false)
  const [serverError, setServerError]   = useState<string | null>(null)
  const [success, setSuccess]           = useState(false)

  function handleChange(e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) {
    const { name, value } = e.target
    setForm(prev => ({ ...prev, [name]: value }))
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

    const payload: RegisterPayload = {
      email:     form.email.trim(),
      password:  form.password,
      firstName: form.firstName.trim(),
      lastName:  form.lastName.trim(),
      role:      form.role as BackendUserRoleValue,
      tenantId:  null,
    }

    setSubmitting(true)
    try {
      const response = await registerUser(payload)
      setSuccess(true)
      onSuccess?.(response)
    } catch (err) {
      setServerError(mapAuthError(err, 'register', t))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="space-y-4">
      {/* Name row */}
      <div className="grid grid-cols-2 gap-3">
        <Input
          label={t('auth.firstName')}
          name="firstName"
          type="text"
          autoComplete="given-name"
          value={form.firstName}
          onChange={handleChange}
          error={errors.firstName}
          required
          disabled={submitting}
        />
        <Input
          label={t('auth.lastName')}
          name="lastName"
          type="text"
          autoComplete="family-name"
          value={form.lastName}
          onChange={handleChange}
          error={errors.lastName}
          required
          disabled={submitting}
        />
      </div>

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

      {/* Role select */}
      <div className="w-full">
        <label
          htmlFor={roleSelectId}
          className="mb-1 block text-sm font-medium text-content-primary"
        >
          {t('auth.role')}
          <span aria-hidden="true" className="ms-0.5 text-error"> *</span>
        </label>
        <select
          id={roleSelectId}
          name="role"
          value={form.role}
          onChange={handleChange}
          required
          disabled={submitting}
          aria-invalid={!!errors.role || undefined}
          className={[
            'w-full rounded-md border bg-surface px-3 py-2 text-sm text-content-primary',
            'transition-colors duration-150',
            'focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-1',
            'disabled:cursor-not-allowed disabled:opacity-60',
            errors.role
              ? 'border-error focus-visible:ring-error'
              : 'border-stroke hover:border-stroke-strong focus-visible:ring-brand-500',
          ].join(' ')}
        >
          {REGISTER_ROLE_OPTIONS.map(({ value, labelKey }) => (
            <option key={value} value={value}>
              {t(labelKey)}
            </option>
          ))}
        </select>
        {errors.role && (
          <p role="alert" className="mt-1 text-xs text-error-dark">
            {errors.role}
          </p>
        )}
      </div>

      <Input
        label={t('auth.password')}
        name="password"
        type="password"
        autoComplete="new-password"
        value={form.password}
        onChange={handleChange}
        error={errors.password}
        required
        disabled={submitting}
      />

      <Input
        label={t('auth.confirmPassword')}
        name="confirmPassword"
        type="password"
        autoComplete="new-password"
        value={form.confirmPassword}
        onChange={handleChange}
        error={errors.confirmPassword}
        required
        disabled={submitting}
      />

      {serverError && (
        <InlineFeedback intent="error" message={serverError} />
      )}

      {success && (
        <InlineFeedback intent="success" message={t('auth.registerSuccess')} />
      )}

      <Button
        type="submit"
        variant="primary"
        size="md"
        isLoading={submitting}
        className="w-full"
      >
        {submitting ? t('auth.registering') : t('auth.register')}
      </Button>

      <p className="text-center text-sm text-content-secondary">
        {t('auth.alreadyHaveAccount')}{' '}
        <Link to="/auth/login" className="text-brand-600 hover:underline font-medium">
          {t('auth.login')}
        </Link>
      </p>
    </form>
  )
}

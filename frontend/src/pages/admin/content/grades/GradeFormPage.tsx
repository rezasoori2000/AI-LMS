import { useState, useEffect, type FormEvent } from 'react'
import { useNavigate, useParams }   from 'react-router-dom'
import { useTranslation }           from 'react-i18next'
import { PageContainer }            from '@/components/layout/PageContainer'
import { Button, Input, SectionCard } from '@/components/ui'
import { InlineFeedback }           from '@/components/feedback'
import { mapApiError }              from '@/utils/mapApiError'
import {
  useGrade,
  useCreateGrade,
  useUpdateGrade,
} from '@/features/admin/content/hooks/useGrades'
import type { GradeFormState } from '@/types/content'

// ── Validation ────────────────────────────────────────────────────────────────

interface GradeErrors {
  name?:  string
  level?: string
}

function validate(form: GradeFormState, t: (k: string) => string): GradeErrors {
  const errors: GradeErrors = {}

  if (!form.name.trim()) {
    errors.name = t('admin.content.validation.nameRequired')
  }

  if (!form.level.trim()) {
    errors.level = t('admin.content.validation.levelRequired')
  } else if (!/^\d+$/.test(form.level.trim()) || parseInt(form.level, 10) < 1) {
    errors.level = t('admin.content.validation.levelInvalid')
  }

  return errors
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * GradeFormPage — create or edit a grade.
 *
 * Operates in two modes based on whether :id param is present:
 *   - No id  → create mode (POST /api/admin/grades)
 *   - With id → edit mode (GET + PUT /api/admin/grades/:id)
 *
 * Phase 2: add tenant selector for SuperAdmin users.
 */
export default function GradeFormPage() {
  const { t }      = useTranslation()
  const navigate   = useNavigate()
  const { id }     = useParams<{ id: string }>()
  const isEditMode = Boolean(id)

  const [form, setForm]           = useState<GradeFormState>({ name: '', level: '' })
  const [errors, setErrors]       = useState<GradeErrors>({})
  const [serverError, setServerError] = useState<string | null>(null)

  // ── Load existing data in edit mode ──────────────────────────────────────
  const { data: existing, isLoading: isLoadingExisting } = useGrade(id)

  useEffect(() => {
    if (existing) {
      setForm({ name: existing.name, level: String(existing.level) })
    }
  }, [existing])

  // ── Mutations ─────────────────────────────────────────────────────────────
  const createMutation = useCreateGrade()
  const updateMutation = useUpdateGrade(id ?? '')

  const isPending = createMutation.isPending || updateMutation.isPending

  // ── Handlers ──────────────────────────────────────────────────────────────
  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const { name, value } = e.target
    setForm(prev => ({ ...prev, [name]: value }))
    setErrors(prev => ({ ...prev, [name]: undefined }))
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setServerError(null)

    const fieldErrors = validate(form, t)
    if (Object.keys(fieldErrors).length > 0) {
      setErrors(fieldErrors)
      return
    }

    const payload = { name: form.name.trim(), level: parseInt(form.level, 10) }

    try {
      if (isEditMode) {
        await updateMutation.mutateAsync(payload)
      } else {
        await createMutation.mutateAsync(payload)
      }
      navigate('/admin/content/grades')
    } catch (err) {
      setServerError(mapApiError(err))
    }
  }

  // ── Render ────────────────────────────────────────────────────────────────
  const pageTitle = isEditMode
    ? t('admin.content.grades.editTitle')
    : t('admin.content.grades.createTitle')

  if (isEditMode && isLoadingExisting) {
    return (
      <PageContainer title={pageTitle}>
        <p className="text-sm text-content-secondary">{t('common.loading')}</p>
      </PageContainer>
    )
  }

  return (
    <PageContainer
      title={pageTitle}
      actions={
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate('/admin/content/grades')}
        >
          {t('common.back')}
        </Button>
      }
    >
      <SectionCard title={pageTitle} className="max-w-lg">
        <form onSubmit={handleSubmit} noValidate className="space-y-4">
          <Input
            label={t('admin.content.grades.name')}
            name="name"
            value={form.name}
            onChange={handleChange}
            required
            error={errors.name}
            autoComplete="off"
          />

          <Input
            label={t('admin.content.grades.level')}
            name="level"
            type="number"
            min={1}
            value={form.level}
            onChange={handleChange}
            required
            error={errors.level}
            helperText={t('admin.content.grades.levelHelper')}
          />

          {serverError && (
            <InlineFeedback intent="error" message={serverError} />
          )}

          <div className="flex justify-end gap-3 pt-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => navigate('/admin/content/grades')}
            >
              {t('common.cancel')}
            </Button>
            <Button type="submit" isLoading={isPending}>
              {isPending ? t('common.saving') : t('common.save')}
            </Button>
          </div>
        </form>
      </SectionCard>
    </PageContainer>
  )
}

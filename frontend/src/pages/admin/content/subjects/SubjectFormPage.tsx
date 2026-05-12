import { useState, useEffect, type FormEvent } from 'react'
import { useNavigate, useParams }   from 'react-router-dom'
import { useTranslation }           from 'react-i18next'
import { PageContainer }            from '@/components/layout/PageContainer'
import { Button, Input, SectionCard } from '@/components/ui'
import { InlineFeedback }           from '@/components/feedback'
import { mapApiError }              from '@/utils/mapApiError'
import {
  useSubject,
  useCreateSubject,
  useUpdateSubject,
} from '@/features/admin/content/hooks/useSubjects'
import type { SubjectFormState } from '@/types/content'

// ── Validation ────────────────────────────────────────────────────────────────

interface SubjectErrors {
  name?:        string
  slug?:        string
}

const SLUG_PATTERN = /^[a-z0-9]+(?:-[a-z0-9]+)*$/

function validate(form: SubjectFormState, t: (k: string) => string): SubjectErrors {
  const errors: SubjectErrors = {}

  if (!form.name.trim()) {
    errors.name = t('admin.content.validation.nameRequired')
  }

  if (!form.slug.trim()) {
    errors.slug = t('admin.content.validation.slugRequired')
  } else if (!SLUG_PATTERN.test(form.slug.trim())) {
    errors.slug = t('admin.content.validation.slugInvalid')
  }

  return errors
}

// ── Slug auto-generation ──────────────────────────────────────────────────────

function generateSlug(name: string): string {
  return name
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9\s-]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '')
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * SubjectFormPage — create or edit a subject.
 *
 * The slug field is auto-populated from the name when creating a new subject
 * (user may override). In edit mode the slug is pre-populated from the API
 * and can be changed.
 *
 * Note: the description field exists on SubjectDto and SubjectFormState but is
 * intentionally omitted from this form to keep the Phase 1 UI minimal. Adding
 * it requires only a Textarea field — no backend changes needed (Phase 2).
 */
export default function SubjectFormPage() {
  const { t }      = useTranslation()
  const navigate   = useNavigate()
  const { id }     = useParams<{ id: string }>()
  const isEditMode = Boolean(id)

  const [form, setForm]     = useState<SubjectFormState>({ name: '', slug: '', description: '' })
  const [slugTouched, setSlugTouched] = useState(false)
  const [errors, setErrors] = useState<SubjectErrors>({})
  const [serverError, setServerError] = useState<string | null>(null)

  const { data: existing, isLoading: isLoadingExisting } = useSubject(id)

  useEffect(() => {
    if (existing) {
      setForm({
        name:        existing.name,
        slug:        existing.slug,
        description: existing.description ?? '',
      })
      setSlugTouched(true) // Don't auto-overwrite slug when editing
    }
  }, [existing])

  const createMutation = useCreateSubject()
  const updateMutation = useUpdateSubject(id ?? '')
  const isPending      = createMutation.isPending || updateMutation.isPending

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const { name, value } = e.target

    setForm(prev => {
      const next = { ...prev, [name]: value }
      // Auto-generate slug from name only while the user hasn't edited it manually
      if (name === 'name' && !slugTouched && !isEditMode) {
        next.slug = generateSlug(value)
      }
      return next
    })
    setErrors(prev => ({ ...prev, [name]: undefined }))
  }

  function handleSlugChange(e: React.ChangeEvent<HTMLInputElement>) {
    setSlugTouched(true)
    setForm(prev => ({ ...prev, slug: e.target.value }))
    setErrors(prev => ({ ...prev, slug: undefined }))
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setServerError(null)

    const fieldErrors = validate(form, t)
    if (Object.keys(fieldErrors).length > 0) {
      setErrors(fieldErrors)
      return
    }

    const payload = {
      name:        form.name.trim(),
      slug:        form.slug.trim(),
      description: form.description.trim() || undefined,
    }

    try {
      if (isEditMode) {
        await updateMutation.mutateAsync(payload)
      } else {
        await createMutation.mutateAsync(payload)
      }
      navigate('/admin/content/subjects')
    } catch (err) {
      setServerError(mapApiError(err))
    }
  }

  const pageTitle = isEditMode
    ? t('admin.content.subjects.editTitle')
    : t('admin.content.subjects.createTitle')

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
        <Button variant="ghost" size="sm" onClick={() => navigate('/admin/content/subjects')}>
          {t('common.back')}
        </Button>
      }
    >
      <SectionCard title={pageTitle} className="max-w-lg">
        <form onSubmit={handleSubmit} noValidate className="space-y-4">
          <Input
            label={t('admin.content.subjects.name')}
            name="name"
            value={form.name}
            onChange={handleChange}
            required
            error={errors.name}
            autoComplete="off"
          />

          <Input
            label={t('admin.content.subjects.slug')}
            name="slug"
            value={form.slug}
            onChange={handleSlugChange}
            required
            error={errors.slug}
            helperText={t('admin.content.subjects.slugHelper')}
            autoComplete="off"
          />

          <Input
            label={t('admin.content.subjects.descriptionLabel')}
            name="description"
            value={form.description}
            onChange={handleChange}
            autoComplete="off"
          />

          {serverError && (
            <InlineFeedback intent="error" message={serverError} />
          )}

          <div className="flex justify-end gap-3 pt-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => navigate('/admin/content/subjects')}
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

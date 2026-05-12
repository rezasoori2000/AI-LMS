import { useState, useEffect, type FormEvent, type ChangeEvent } from 'react'
import { useNavigate, useParams }   from 'react-router-dom'
import { useTranslation }           from 'react-i18next'
import { PageContainer }            from '@/components/layout/PageContainer'
import { Button, Input, SectionCard } from '@/components/ui'
import { InlineFeedback }           from '@/components/feedback'
import { mapApiError }              from '@/utils/mapApiError'
import {
  useChapter,
  useCreateChapter,
  useUpdateChapter,
} from '@/features/admin/content/hooks/useChapters'
import { useGrades }   from '@/features/admin/content/hooks/useGrades'
import { useSubjects } from '@/features/admin/content/hooks/useSubjects'
import type { ChapterFormState } from '@/types/content'

// ── Validation ────────────────────────────────────────────────────────────────

interface ChapterErrors {
  title?:     string
  order?:     string
  subjectId?: string
  gradeId?:   string
}

function validate(form: ChapterFormState, t: (k: string) => string): ChapterErrors {
  const errors: ChapterErrors = {}

  if (!form.title.trim()) {
    errors.title = t('admin.content.validation.titleRequired')
  }
  if (!form.order.trim()) {
    errors.order = t('admin.content.validation.orderRequired')
  } else if (!/^\d+$/.test(form.order.trim()) || parseInt(form.order, 10) < 1) {
    errors.order = t('admin.content.validation.orderInvalid')
  }
  if (!form.subjectId) {
    errors.subjectId = t('admin.content.validation.subjectRequired')
  }
  if (!form.gradeId) {
    errors.gradeId = t('admin.content.validation.gradeRequired')
  }

  return errors
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * ChapterFormPage — create or edit a chapter.
 *
 * Subject and grade are fixed after creation (backend constraint).
 * In edit mode the selects are disabled so the user sees the value
 * but cannot change it.
 *
 * Phase 2: replace <select> elements with searchable ComboBox when
 *   subject/grade list counts grow.
 */
export default function ChapterFormPage() {
  const { t }      = useTranslation()
  const navigate   = useNavigate()
  const { id }     = useParams<{ id: string }>()
  const isEditMode = Boolean(id)

  const [form, setForm]     = useState<ChapterFormState>({
    subjectId:   '',
    gradeId:     '',
    title:       '',
    description: '',
    order:       '',
  })
  const [errors, setErrors]           = useState<ChapterErrors>({})
  const [serverError, setServerError] = useState<string | null>(null)

  const { data: existing, isLoading: isLoadingExisting } = useChapter(id)
  const { data: grades   = [] } = useGrades()
  const { data: subjects = [] } = useSubjects()

  useEffect(() => {
    if (existing) {
      setForm({
        subjectId:   existing.subjectId,
        gradeId:     existing.gradeId,
        title:       existing.title,
        description: existing.description ?? '',
        order:       String(existing.order),
      })
    }
  }, [existing])

  const createMutation = useCreateChapter()
  const updateMutation = useUpdateChapter(id ?? '')
  const isPending      = createMutation.isPending || updateMutation.isPending

  function handleChange(e: ChangeEvent<HTMLInputElement | HTMLSelectElement>) {
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

    try {
      if (isEditMode) {
        await updateMutation.mutateAsync({
          title:       form.title.trim(),
          order:       parseInt(form.order, 10),
          description: form.description.trim() || undefined,
        })
      } else {
        await createMutation.mutateAsync({
          subjectId:   form.subjectId,
          gradeId:     form.gradeId,
          title:       form.title.trim(),
          order:       parseInt(form.order, 10),
          description: form.description.trim() || undefined,
        })
      }
      navigate('/admin/content/chapters')
    } catch (err) {
      setServerError(mapApiError(err))
    }
  }

  const pageTitle = isEditMode
    ? t('admin.content.chapters.editTitle')
    : t('admin.content.chapters.createTitle')

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
        <Button variant="ghost" size="sm" onClick={() => navigate('/admin/content/chapters')}>
          {t('common.back')}
        </Button>
      }
    >
      <SectionCard title={pageTitle} className="max-w-lg">
        <form onSubmit={handleSubmit} noValidate className="space-y-4">
          {/* Subject select */}
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-content-primary">
              {t('admin.content.chapters.subject')}
              <span aria-hidden="true" className="ms-1 text-error">*</span>
            </label>
            <select
              name="subjectId"
              value={form.subjectId}
              onChange={handleChange}
              disabled={isEditMode}
              required
              className="block w-full rounded-md border border-stroke bg-surface px-3 py-2 text-sm text-content-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 disabled:opacity-50"
            >
              <option value="">— {t('admin.content.chapters.selectSubject')} —</option>
              {subjects.map(s => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
            {errors.subjectId && (
              <p role="alert" className="text-xs text-error">{errors.subjectId}</p>
            )}
          </div>

          {/* Grade select */}
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-content-primary">
              {t('admin.content.chapters.grade')}
              <span aria-hidden="true" className="ms-1 text-error">*</span>
            </label>
            <select
              name="gradeId"
              value={form.gradeId}
              onChange={handleChange}
              disabled={isEditMode}
              required
              className="block w-full rounded-md border border-stroke bg-surface px-3 py-2 text-sm text-content-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 disabled:opacity-50"
            >
              <option value="">— {t('admin.content.chapters.selectGrade')} —</option>
              {grades.map(g => (
                <option key={g.id} value={g.id}>{g.name}</option>
              ))}
            </select>
            {errors.gradeId && (
              <p role="alert" className="text-xs text-error">{errors.gradeId}</p>
            )}
          </div>

          <Input
            label={t('admin.content.chapters.titleField')}
            name="title"
            value={form.title}
            onChange={handleChange}
            required
            error={errors.title}
            autoComplete="off"
          />

          <Input
            label={t('admin.content.chapters.order')}
            name="order"
            type="number"
            min={1}
            value={form.order}
            onChange={handleChange}
            required
            error={errors.order}
            helperText={t('admin.content.chapters.orderHelper')}
          />

          <Input
            label={t('admin.content.chapters.descriptionLabel')}
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
              onClick={() => navigate('/admin/content/chapters')}
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

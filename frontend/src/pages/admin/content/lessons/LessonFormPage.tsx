import { useState, useEffect, type FormEvent, type ChangeEvent } from 'react'
import { useNavigate, useParams }   from 'react-router-dom'
import { useTranslation }           from 'react-i18next'
import { PageContainer }            from '@/components/layout/PageContainer'
import { Button, Input, SectionCard, Textarea } from '@/components/ui'
import { InlineFeedback }           from '@/components/feedback'
import { mapApiError }              from '@/utils/mapApiError'
import {
  useLesson,
  useCreateLesson,
  useUpdateLesson,
} from '@/features/admin/content/hooks/useLessons'
import { useChapters } from '@/features/admin/content/hooks/useChapters'
import type { LessonFormState } from '@/types/content'

// ── Validation ────────────────────────────────────────────────────────────────

interface LessonErrors {
  chapterId?:        string
  title?:            string
  order?:            string
  estimatedMinutes?: string
}

function validate(form: LessonFormState, t: (k: string) => string): LessonErrors {
  const errors: LessonErrors = {}

  if (!form.chapterId) {
    errors.chapterId = t('admin.content.validation.chapterRequired')
  }
  if (!form.title.trim()) {
    errors.title = t('admin.content.validation.titleRequired')
  }
  if (!form.order.trim()) {
    errors.order = t('admin.content.validation.orderRequired')
  } else if (!/^\d+$/.test(form.order.trim()) || parseInt(form.order, 10) < 1) {
    errors.order = t('admin.content.validation.orderInvalid')
  }
  if (form.estimatedMinutes.trim()) {
    if (!/^\d+$/.test(form.estimatedMinutes.trim()) || parseInt(form.estimatedMinutes, 10) < 1) {
      errors.estimatedMinutes = t('admin.content.validation.estimatedMinutesInvalid')
    }
  }

  return errors
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * LessonFormPage — create or edit a lesson.
 *
 * Chapter is fixed after creation (backend constraint).
 * Content is stored as plain Markdown text.
 *
 * Phase 3: swap the Textarea for a proper Markdown editor (CodeMirror or
 *   similar) when rich lesson authoring is in scope.
 * Phase 3: add video/media attachment upload field.
 */
export default function LessonFormPage() {
  const { t }      = useTranslation()
  const navigate   = useNavigate()
  const { id }     = useParams<{ id: string }>()
  const isEditMode = Boolean(id)

  const [form, setForm]     = useState<LessonFormState>({
    chapterId:        '',
    title:            '',
    content:          '',
    order:            '',
    estimatedMinutes: '',
  })
  const [errors, setErrors]           = useState<LessonErrors>({})
  const [serverError, setServerError] = useState<string | null>(null)

  const { data: existing, isLoading: isLoadingExisting } = useLesson(id)
  const { data: chapters = [] } = useChapters()

  useEffect(() => {
    if (existing) {
      setForm({
        chapterId:        existing.chapterId,
        title:            existing.title,
        content:          existing.content ?? '',
        order:            String(existing.order),
        estimatedMinutes: existing.estimatedMinutes != null ? String(existing.estimatedMinutes) : '',
      })
    }
  }, [existing])

  const createMutation = useCreateLesson()
  const updateMutation = useUpdateLesson(id ?? '')
  const isPending      = createMutation.isPending || updateMutation.isPending

  function handleChange(e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) {
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

    const minutes = form.estimatedMinutes.trim()
      ? parseInt(form.estimatedMinutes, 10)
      : undefined

    try {
      if (isEditMode) {
        await updateMutation.mutateAsync({
          title:            form.title.trim(),
          order:            parseInt(form.order, 10),
          content:          form.content.trim() || undefined,
          estimatedMinutes: minutes,
        })
      } else {
        await createMutation.mutateAsync({
          chapterId:        form.chapterId,
          title:            form.title.trim(),
          order:            parseInt(form.order, 10),
          content:          form.content.trim() || undefined,
          estimatedMinutes: minutes,
        })
      }
      navigate('/admin/content/lessons')
    } catch (err) {
      setServerError(mapApiError(err))
    }
  }

  const pageTitle = isEditMode
    ? t('admin.content.lessons.editTitle')
    : t('admin.content.lessons.createTitle')

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
        <Button variant="ghost" size="sm" onClick={() => navigate('/admin/content/lessons')}>
          {t('common.back')}
        </Button>
      }
    >
      <SectionCard title={pageTitle} className="max-w-2xl">
        <form onSubmit={handleSubmit} noValidate className="space-y-4">
          {/* Chapter select */}
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-content-primary">
              {t('admin.content.lessons.chapter')}
              <span aria-hidden="true" className="ms-1 text-error">*</span>
            </label>
            <select
              name="chapterId"
              value={form.chapterId}
              onChange={handleChange}
              disabled={isEditMode}
              required
              className="block w-full rounded-md border border-stroke bg-surface px-3 py-2 text-sm text-content-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 disabled:opacity-50"
            >
              <option value="">— {t('admin.content.lessons.selectChapter')} —</option>
              {chapters.map(c => (
                <option key={c.id} value={c.id}>{c.title}</option>
              ))}
            </select>
            {errors.chapterId && (
              <p role="alert" className="text-xs text-error">{errors.chapterId}</p>
            )}
          </div>

          <Input
            label={t('admin.content.lessons.titleField')}
            name="title"
            value={form.title}
            onChange={handleChange}
            required
            error={errors.title}
            autoComplete="off"
          />

          <div className="grid grid-cols-2 gap-4">
            <Input
              label={t('admin.content.lessons.order')}
              name="order"
              type="number"
              min={1}
              value={form.order}
              onChange={handleChange}
              required
              error={errors.order}
              helperText={t('admin.content.lessons.orderHelper')}
            />
            <Input
              label={t('admin.content.lessons.estimatedMinutes')}
              name="estimatedMinutes"
              type="number"
              min={1}
              value={form.estimatedMinutes}
              onChange={handleChange}
              error={errors.estimatedMinutes}
            />
          </div>

          <Textarea
            label={t('admin.content.lessons.content')}
            name="content"
            value={form.content}
            onChange={handleChange}
            rows={10}
            helperText={t('admin.content.lessons.contentHelper')}
          />

          {serverError && (
            <InlineFeedback intent="error" message={serverError} />
          )}

          <div className="flex justify-end gap-3 pt-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => navigate('/admin/content/lessons')}
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

import { useState, useEffect, type FormEvent, type ChangeEvent } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { useTranslation }                          from 'react-i18next'
import { PageContainer }                           from '@/components/layout/PageContainer'
import { Button, Input, SectionCard }              from '@/components/ui'
import { InlineFeedback }                          from '@/components/feedback'
import { mapApiError }                             from '@/utils/mapApiError'
import {
  useQuestion,
  useCreateQuestion,
  useUpdateQuestion,
} from '@/features/admin/content/hooks/useQuestions'
import { useLessons } from '@/features/admin/content/hooks/useLessons'
import type {
  QuestionFormState,
  QuestionType,
  DifficultyLevel,
} from '@/types/content'

// ── Constants ─────────────────────────────────────────────────────────────────

const QUESTION_TYPES: QuestionType[]   = ['MultipleChoice', 'TrueFalse', 'ShortAnswer']
const DIFFICULTY_LEVELS: DifficultyLevel[] = ['Easy', 'Medium', 'Hard']
const OPTION_LABELS = ['A', 'B', 'C', 'D']

const INITIAL_FORM: QuestionFormState = {
  lessonId:           '',
  text:               '',
  type:               '',
  difficulty:         '',
  options:            ['', '', '', ''],
  correctAnswerIndex: '',
  correctAnswerBool:  '',
  correctAnswerText:  '',
}

// ── Validation ────────────────────────────────────────────────────────────────

interface QuestionErrors {
  text?:              string
  type?:              string
  difficulty?:        string
  options?:           string
  correctAnswer?:     string
}

function validate(
  form: QuestionFormState,
  t: (k: string) => string,
): QuestionErrors {
  const errors: QuestionErrors = {}

  if (!form.text.trim()) {
    errors.text = t('admin.content.validation.questionTextRequired')
  }
  if (!form.type) {
    errors.type = t('admin.content.validation.questionTypeRequired')
  }
  if (!form.difficulty) {
    errors.difficulty = t('admin.content.validation.questionDifficultyRequired')
  }

  if (form.type === 'MultipleChoice') {
    if (form.options.some(o => !o.trim())) {
      errors.options = t('admin.content.validation.questionOptionsRequired')
    }
    if (!form.correctAnswerIndex) {
      errors.correctAnswer = t('admin.content.validation.questionCorrectAnswerRequired')
    }
  }

  if (form.type === 'TrueFalse' && !form.correctAnswerBool) {
    errors.correctAnswer = t('admin.content.validation.questionCorrectAnswerRequired')
  }

  if (form.type === 'ShortAnswer' && !form.correctAnswerText.trim()) {
    errors.correctAnswer = t('admin.content.validation.questionCorrectAnswerRequired')
  }

  return errors
}

// ── Payload builder ───────────────────────────────────────────────────────────

function buildCorrectAnswer(form: QuestionFormState): string {
  switch (form.type) {
    case 'MultipleChoice': return form.correctAnswerIndex
    case 'TrueFalse':      return form.correctAnswerBool
    case 'ShortAnswer':    return form.correctAnswerText.trim()
    default:               return ''
  }
}

function buildOptionsJson(form: QuestionFormState): string | null {
  return form.type === 'MultipleChoice'
    ? JSON.stringify(form.options.map(o => o.trim()))
    : null
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * QuestionFormPage — create or edit a question.
 *
 * Type-specific fields are conditionally shown based on the selected
 * question type:
 *   MultipleChoice — 4 option inputs + "Correct option" select
 *   TrueFalse      — "Correct answer" True/False select
 *   ShortAnswer    — "Sample answer" textarea (reference for AI validation)
 *
 * Accepts a ?lessonId= query param to pre-fill the lesson assignment on
 * create.  Used by the LessonsPage "Questions" shortcut.
 *
 * Phase 2: replace the lesson select with a searchable ComboBox when the
 *   lesson list grows beyond ~30 items.
 * Phase 3: add explanation field (shown to students after attempting).
 * Phase 3: add tag multi-select for categorisation and filtered practice.
 */
export default function QuestionFormPage() {
  const { t }       = useTranslation()
  const navigate    = useNavigate()
  const { id }      = useParams<{ id: string }>()
  const [searchParams] = useSearchParams()
  const isEditMode  = Boolean(id)

  const [form, setForm]     = useState<QuestionFormState>({
    ...INITIAL_FORM,
    // Pre-fill lessonId from the query param on the create route
    lessonId: !isEditMode ? (searchParams.get('lessonId') ?? '') : '',
  })
  const [errors, setErrors]           = useState<QuestionErrors>({})
  const [serverError, setServerError] = useState<string | null>(null)

  const { data: existing, isLoading: isLoadingExisting } = useQuestion(id)
  const { data: lessons = [] } = useLessons()

  // Populate form from the existing record when editing
  useEffect(() => {
    if (!existing) return
    let options: [string, string, string, string] = ['', '', '', '']
    let correctAnswerIndex = ''
    let correctAnswerBool  = ''
    let correctAnswerText  = ''

    if (existing.type === 'MultipleChoice' && existing.optionsJson) {
      try {
        const parsed = JSON.parse(existing.optionsJson) as string[]
        options = [
          parsed[0] ?? '',
          parsed[1] ?? '',
          parsed[2] ?? '',
          parsed[3] ?? '',
        ]
      } catch {
        // malformed JSON — leave slots empty
      }
      correctAnswerIndex = existing.correctAnswer
    } else if (existing.type === 'TrueFalse') {
      correctAnswerBool = existing.correctAnswer
    } else {
      correctAnswerText = existing.correctAnswer
    }

    setForm({
      lessonId:           existing.lessonId ?? '',
      text:               existing.text,
      type:               existing.type,
      difficulty:         existing.difficulty,
      options,
      correctAnswerIndex,
      correctAnswerBool,
      correctAnswerText,
    })
  }, [existing])

  const createMutation = useCreateQuestion()
  const updateMutation = useUpdateQuestion(id ?? '')
  const isPending      = createMutation.isPending || updateMutation.isPending

  function handleChange(
    e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>,
  ) {
    const { name, value } = e.target
    setForm(prev => ({ ...prev, [name]: value }))
    setErrors(prev => ({ ...prev, [name]: undefined }))
  }

  function handleTypeChange(e: ChangeEvent<HTMLSelectElement>) {
    // Reset all type-specific fields when the type is changed to prevent
    // stale answer data from a different type being sent to the server.
    setForm(prev => ({
      ...prev,
      type:               e.target.value as QuestionType | '',
      options:            ['', '', '', ''],
      correctAnswerIndex: '',
      correctAnswerBool:  '',
      correctAnswerText:  '',
    }))
    setErrors(prev => ({ ...prev, type: undefined, options: undefined, correctAnswer: undefined }))
  }

  function handleOptionChange(index: number, value: string) {
    setForm(prev => {
      const next: [string, string, string, string] = [...prev.options] as [string, string, string, string]
      next[index] = value
      return { ...prev, options: next }
    })
    setErrors(prev => ({ ...prev, options: undefined }))
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setServerError(null)

    const fieldErrors = validate(form, t)
    if (Object.keys(fieldErrors).length > 0) {
      setErrors(fieldErrors)
      return
    }

    const correctAnswer = buildCorrectAnswer(form)
    const optionsJson   = buildOptionsJson(form)

    try {
      if (isEditMode) {
        await updateMutation.mutateAsync({
          text:          form.text.trim(),
          type:          form.type as QuestionType,
          difficulty:    form.difficulty as DifficultyLevel,
          correctAnswer,
          optionsJson,
          lessonId:      form.lessonId || null,
        })
      } else {
        const payload = {
          text:          form.text.trim(),
          type:          form.type as QuestionType,
          difficulty:    form.difficulty as DifficultyLevel,
          correctAnswer,
          ...(optionsJson    ? { optionsJson }              : {}),
          ...(form.lessonId  ? { lessonId: form.lessonId } : {}),
        }
        await createMutation.mutateAsync(payload)
      }

      // Navigate back: if we came from a lesson context, return there
      const returnLessonId = form.lessonId
      if (returnLessonId && !isEditMode) {
        navigate(`/admin/content/questions?lessonId=${returnLessonId}`)
      } else {
        navigate('/admin/content/questions')
      }
    } catch (err) {
      setServerError(mapApiError(err))
    }
  }

  const pageTitle = isEditMode
    ? t('admin.content.questions.editTitle')
    : t('admin.content.questions.createTitle')

  if (isEditMode && isLoadingExisting) {
    return (
      <PageContainer title={pageTitle}>
        <p className="text-sm text-content-secondary">{t('common.loading')}</p>
      </PageContainer>
    )
  }

  const selectClass =
    'block w-full rounded-md border border-stroke bg-surface px-3 py-2 text-sm text-content-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 disabled:opacity-50'

  return (
    <PageContainer
      title={pageTitle}
      actions={
        <Button variant="ghost" size="sm" onClick={() => navigate('/admin/content/questions')}>
          {t('common.back')}
        </Button>
      }
    >
      <SectionCard title={pageTitle} className="max-w-2xl">
        <form onSubmit={handleSubmit} noValidate className="space-y-4">

          {/* ── Lesson assignment (optional) ────────────────────────────── */}
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-content-primary">
              {t('admin.content.questions.lesson')}
            </label>
            <select
              name="lessonId"
              value={form.lessonId}
              onChange={handleChange}
              className={selectClass}
            >
              <option value="">{t('admin.content.questions.noLesson')}</option>
              {lessons.map(l => (
                <option key={l.id} value={l.id}>{l.title}</option>
              ))}
            </select>
            <p className="text-xs text-content-muted">
              {t('admin.content.questions.lessonHelper')}
            </p>
          </div>

          {/* ── Question type ────────────────────────────────────────────── */}
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-content-primary">
              {t('admin.content.questions.typeLabel')}
              <span aria-hidden="true" className="ms-1 text-error">*</span>
            </label>
            <select
              name="type"
              value={form.type}
              onChange={handleTypeChange}
              required
              className={selectClass}
            >
              <option value="">— {t('admin.content.questions.selectType')} —</option>
              {QUESTION_TYPES.map(qt => (
                <option key={qt} value={qt}>
                  {t(`admin.content.questions.types.${qt}`)}
                </option>
              ))}
            </select>
            {errors.type && (
              <p role="alert" className="text-xs text-error">{errors.type}</p>
            )}
          </div>

          {/* ── Difficulty ───────────────────────────────────────────────── */}
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-content-primary">
              {t('admin.content.questions.difficultyLabel')}
              <span aria-hidden="true" className="ms-1 text-error">*</span>
            </label>
            <select
              name="difficulty"
              value={form.difficulty}
              onChange={handleChange}
              required
              className={selectClass}
            >
              <option value="">— {t('admin.content.questions.selectDifficulty')} —</option>
              {DIFFICULTY_LEVELS.map(d => (
                <option key={d} value={d}>
                  {t(`admin.content.questions.difficulty.${d}`)}
                </option>
              ))}
            </select>
            {errors.difficulty && (
              <p role="alert" className="text-xs text-error">{errors.difficulty}</p>
            )}
          </div>

          {/* ── Question text ────────────────────────────────────────────── */}
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-content-primary">
              {t('admin.content.questions.textLabel')}
              <span aria-hidden="true" className="ms-1 text-error">*</span>
            </label>
            <textarea
              name="text"
              value={form.text}
              onChange={handleChange}
              rows={3}
              required
              className="block w-full rounded-md border border-stroke bg-surface px-3 py-2 text-sm text-content-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 resize-none"
              placeholder={t('admin.content.questions.textPlaceholder')}
            />
            {errors.text && (
              <p role="alert" className="text-xs text-error">{errors.text}</p>
            )}
          </div>

          {/* ── Type-specific fields ─────────────────────────────────────── */}

          {/* MultipleChoice: 4 option inputs + correct option select */}
          {form.type === 'MultipleChoice' && (
            <div className="space-y-3 rounded-md border border-stroke bg-surface-raised p-4">
              <p className="text-sm font-medium text-content-primary">
                {t('admin.content.questions.options')}
                <span aria-hidden="true" className="ms-1 text-error">*</span>
              </p>
              {form.options.map((opt, i) => (
                <Input
                  key={i}
                  label={`${t('admin.content.questions.option')} ${OPTION_LABELS[i]}`}
                  value={opt}
                  onChange={e => handleOptionChange(i, e.target.value)}
                  required
                  autoComplete="off"
                />
              ))}
              {errors.options && (
                <p role="alert" className="text-xs text-error">{errors.options}</p>
              )}

              <div className="flex flex-col gap-1">
                <label className="text-sm font-medium text-content-primary">
                  {t('admin.content.questions.correctOption')}
                  <span aria-hidden="true" className="ms-1 text-error">*</span>
                </label>
                <select
                  name="correctAnswerIndex"
                  value={form.correctAnswerIndex}
                  onChange={handleChange}
                  className={selectClass}
                >
                  <option value="">— {t('admin.content.questions.selectCorrect')} —</option>
                  {OPTION_LABELS.map((label, i) => (
                    <option key={i} value={String(i)}>
                      {t('admin.content.questions.option')} {label}
                      {form.options[i] ? ` — ${form.options[i]}` : ''}
                    </option>
                  ))}
                </select>
                {errors.correctAnswer && (
                  <p role="alert" className="text-xs text-error">{errors.correctAnswer}</p>
                )}
              </div>
            </div>
          )}

          {/* TrueFalse: True/False select */}
          {form.type === 'TrueFalse' && (
            <div className="flex flex-col gap-1">
              <label className="text-sm font-medium text-content-primary">
                {t('admin.content.questions.correctAnswer')}
                <span aria-hidden="true" className="ms-1 text-error">*</span>
              </label>
              <select
                name="correctAnswerBool"
                value={form.correctAnswerBool}
                onChange={handleChange}
                className={selectClass}
              >
                <option value="">— {t('admin.content.questions.selectCorrect')} —</option>
                <option value="true">{t('common.true')}</option>
                <option value="false">{t('common.false')}</option>
              </select>
              {errors.correctAnswer && (
                <p role="alert" className="text-xs text-error">{errors.correctAnswer}</p>
              )}
            </div>
          )}

          {/* ShortAnswer: sample/reference answer */}
          {form.type === 'ShortAnswer' && (
            <Input
              label={t('admin.content.questions.sampleAnswer')}
              name="correctAnswerText"
              value={form.correctAnswerText}
              onChange={handleChange}
              required
              error={errors.correctAnswer}
              helperText={t('admin.content.questions.sampleAnswerHelper')}
              autoComplete="off"
            />
          )}

          {/* ── Server error ─────────────────────────────────────────────── */}
          {serverError && (
            <InlineFeedback intent="error" message={serverError} />
          )}

          {/* ── Submit ───────────────────────────────────────────────────── */}
          <div className="flex items-center justify-end gap-3 pt-2">
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => navigate('/admin/content/questions')}
            >
              {t('common.cancel')}
            </Button>
            <Button type="submit" size="sm" disabled={isPending}>
              {isPending ? t('common.saving') : t('common.save')}
            </Button>
          </div>

        </form>
      </SectionCard>
    </PageContainer>
  )
}

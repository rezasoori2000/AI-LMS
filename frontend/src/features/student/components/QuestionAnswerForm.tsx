import { Textarea } from '@/components/ui'
import type { QuestionForStudentDto } from '@/types/student'

interface QuestionAnswerFormProps {
  questions: QuestionForStudentDto[]
  answers: Record<string, string>
  onAnswerChange: (questionId: string, answer: string) => void
}

export function QuestionAnswerForm({
  questions,
  answers,
  onAnswerChange,
}: QuestionAnswerFormProps) {
  return (
    <div className="space-y-6">
      {questions.map((q, index) => {
        const current = answers[q.questionId] ?? ''

        return (
          <div key={q.questionId} className="rounded-md border border-stroke bg-surface-raised p-4">
            <p className="text-xs font-medium uppercase tracking-wide text-content-muted">
              Question {index + 1}
            </p>
            <p className="mt-1 text-sm font-medium text-content-primary">{q.text}</p>

            {q.type === 'MultipleChoice' && (
              <div className="mt-3 space-y-2">
                {(q.options ?? []).map((option, optionIndex) => {
                  const value = String(optionIndex)
                  const inputId = `${q.questionId}-${value}`

                  return (
                    <label
                      key={inputId}
                      htmlFor={inputId}
                      className="flex cursor-pointer items-start gap-3 rounded-md border border-stroke bg-surface px-3 py-2"
                    >
                      <input
                        id={inputId}
                        type="radio"
                        name={q.questionId}
                        value={value}
                        checked={current === value}
                        onChange={e => onAnswerChange(q.questionId, e.target.value)}
                        className="mt-1"
                      />
                      <span className="text-sm text-content-primary">{option}</span>
                    </label>
                  )
                })}
              </div>
            )}

            {q.type === 'TrueFalse' && (
              <div className="mt-3 flex flex-wrap gap-3">
                {[
                  { label: 'True', value: 'true' },
                  { label: 'False', value: 'false' },
                ].map(opt => {
                  const inputId = `${q.questionId}-${opt.value}`

                  return (
                    <label
                      key={inputId}
                      htmlFor={inputId}
                      className="flex cursor-pointer items-center gap-2 rounded-md border border-stroke bg-surface px-3 py-2"
                    >
                      <input
                        id={inputId}
                        type="radio"
                        name={q.questionId}
                        value={opt.value}
                        checked={current === opt.value}
                        onChange={e => onAnswerChange(q.questionId, e.target.value)}
                      />
                      <span className="text-sm text-content-primary">{opt.label}</span>
                    </label>
                  )
                })}
              </div>
            )}

            {q.type === 'ShortAnswer' && (
              <div className="mt-3">
                <Textarea
                  label="Your answer"
                  value={current}
                  onChange={e => onAnswerChange(q.questionId, e.target.value)}
                  rows={3}
                  placeholder="Type your answer"
                />
              </div>
            )}
          </div>
        )
      })}
    </div>
  )
}
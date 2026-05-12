import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getQuestions,
  getQuestion,
  createQuestion,
  updateQuestion,
  deleteQuestion,
} from '@/services/content.service'
import type { CreateQuestionPayload, UpdateQuestionPayload } from '@/types/content'

export const questionKeys = {
  all:    ['admin', 'questions'] as const,
  list:   (lessonId?: string) => ['admin', 'questions', 'list', lessonId ?? ''] as const,
  detail: (id: string) => ['admin', 'questions', id] as const,
}

// ── Queries ───────────────────────────────────────────────────────────────────

export function useQuestions(lessonId?: string) {
  return useQuery({
    queryKey: questionKeys.list(lessonId),
    queryFn:  () => getQuestions(lessonId),
  })
}

export function useQuestion(id: string | undefined) {
  return useQuery({
    queryKey: questionKeys.detail(id ?? ''),
    queryFn:  () => getQuestion(id!),
    enabled:  Boolean(id),
  })
}

// ── Mutations ─────────────────────────────────────────────────────────────────

export function useCreateQuestion() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (payload: CreateQuestionPayload) => createQuestion(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: questionKeys.all })
    },
  })
}

export function useUpdateQuestion(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (payload: UpdateQuestionPayload) => updateQuestion(id, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: questionKeys.all })
      qc.invalidateQueries({ queryKey: questionKeys.detail(id) })
    },
  })
}

export function useDeleteQuestion() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteQuestion(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: questionKeys.all })
    },
  })
}

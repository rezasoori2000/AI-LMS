import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getLessons,
  getLesson,
  createLesson,
  updateLesson,
  deleteLesson,
} from '@/services/content.service'
import type { CreateLessonPayload, UpdateLessonPayload } from '@/types/content'

export const lessonKeys = {
  all:    ['admin', 'lessons'] as const,
  list:   (chapterId?: string) => ['admin', 'lessons', 'list', chapterId ?? ''] as const,
  detail: (id: string) => ['admin', 'lessons', id] as const,
}

// ── Queries ───────────────────────────────────────────────────────────────────

export function useLessons(chapterId?: string) {
  return useQuery({
    queryKey: lessonKeys.list(chapterId),
    queryFn:  () => getLessons(chapterId),
  })
}

export function useLesson(id: string | undefined) {
  return useQuery({
    queryKey: lessonKeys.detail(id ?? ''),
    queryFn:  () => getLesson(id!),
    enabled:  Boolean(id),
  })
}

// ── Mutations ─────────────────────────────────────────────────────────────────

export function useCreateLesson() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (payload: CreateLessonPayload) => createLesson(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: lessonKeys.all })
    },
  })
}

export function useUpdateLesson(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (payload: UpdateLessonPayload) => updateLesson(id, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: lessonKeys.all })
      qc.invalidateQueries({ queryKey: lessonKeys.detail(id) })
    },
  })
}

export function useDeleteLesson() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteLesson(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: lessonKeys.all })
    },
  })
}

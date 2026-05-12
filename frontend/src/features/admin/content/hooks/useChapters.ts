import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getChapters,
  getChapter,
  createChapter,
  updateChapter,
  deleteChapter,
  type ChapterListFilters,
} from '@/services/content.service'
import type { CreateChapterPayload, UpdateChapterPayload } from '@/types/content'

export const chapterKeys = {
  all:     ['admin', 'chapters'] as const,
  list:    (filters?: ChapterListFilters) => ['admin', 'chapters', 'list', filters ?? {}] as const,
  detail:  (id: string) => ['admin', 'chapters', id] as const,
}

// ── Queries ───────────────────────────────────────────────────────────────────

export function useChapters(filters?: ChapterListFilters) {
  return useQuery({
    queryKey: chapterKeys.list(filters),
    queryFn:  () => getChapters(filters),
  })
}

export function useChapter(id: string | undefined) {
  return useQuery({
    queryKey: chapterKeys.detail(id ?? ''),
    queryFn:  () => getChapter(id!),
    enabled:  Boolean(id),
  })
}

// ── Mutations ─────────────────────────────────────────────────────────────────

export function useCreateChapter() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (payload: CreateChapterPayload) => createChapter(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: chapterKeys.all })
    },
  })
}

export function useUpdateChapter(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (payload: UpdateChapterPayload) => updateChapter(id, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: chapterKeys.all })
      qc.invalidateQueries({ queryKey: chapterKeys.detail(id) })
    },
  })
}

export function useDeleteChapter() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteChapter(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: chapterKeys.all })
    },
  })
}

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getSubjects,
  getSubject,
  createSubject,
  updateSubject,
  deleteSubject,
} from '@/services/content.service'
import type { CreateSubjectPayload, UpdateSubjectPayload } from '@/types/content'

export const subjectKeys = {
  all:    ['admin', 'subjects'] as const,
  detail: (id: string) => ['admin', 'subjects', id] as const,
}

// ── Queries ───────────────────────────────────────────────────────────────────

export function useSubjects() {
  return useQuery({
    queryKey: subjectKeys.all,
    queryFn:  () => getSubjects(),
  })
}

export function useSubject(id: string | undefined) {
  return useQuery({
    queryKey: subjectKeys.detail(id ?? ''),
    queryFn:  () => getSubject(id!),
    enabled:  Boolean(id),
  })
}

// ── Mutations ─────────────────────────────────────────────────────────────────

export function useCreateSubject() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (payload: CreateSubjectPayload) => createSubject(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: subjectKeys.all })
    },
  })
}

export function useUpdateSubject(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (payload: UpdateSubjectPayload) => updateSubject(id, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: subjectKeys.all })
      qc.invalidateQueries({ queryKey: subjectKeys.detail(id) })
    },
  })
}

export function useDeleteSubject() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteSubject(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: subjectKeys.all })
    },
  })
}

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getGrades,
  getGrade,
  createGrade,
  updateGrade,
  deleteGrade,
} from '@/services/content.service'
import type { CreateGradePayload, UpdateGradePayload } from '@/types/content'

// Stable query key factory — centralised so invalidations always match.
export const gradeKeys = {
  all:    ['admin', 'grades'] as const,
  detail: (id: string) => ['admin', 'grades', id] as const,
}

// ── Queries ───────────────────────────────────────────────────────────────────

export function useGrades() {
  return useQuery({
    queryKey: gradeKeys.all,
    queryFn:  () => getGrades(),
  })
}

export function useGrade(id: string | undefined) {
  return useQuery({
    queryKey: gradeKeys.detail(id ?? ''),
    queryFn:  () => getGrade(id!),
    enabled:  Boolean(id),
  })
}

// ── Mutations ─────────────────────────────────────────────────────────────────

export function useCreateGrade() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (payload: CreateGradePayload) => createGrade(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: gradeKeys.all })
    },
  })
}

export function useUpdateGrade(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (payload: UpdateGradePayload) => updateGrade(id, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: gradeKeys.all })
      qc.invalidateQueries({ queryKey: gradeKeys.detail(id) })
    },
  })
}

export function useDeleteGrade() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteGrade(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: gradeKeys.all })
    },
  })
}

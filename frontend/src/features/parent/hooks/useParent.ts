import { useQuery } from '@tanstack/react-query'
import { getMyChildren, getChildDetail } from '@/services/parent.service'

// Stable query key factory — centralised so invalidations always match.
export const parentKeys = {
  children:    ['parent', 'children']                              as const,
  childDetail: (studentId: string) => ['parent', 'children', studentId] as const,
}

/**
 * Fetches all students linked to the calling parent.
 * Returns an empty array when no students are linked — never throws on 200.
 */
export function useMyChildren() {
  return useQuery({
    queryKey: parentKeys.children,
    queryFn:  getMyChildren,
  })
}

/**
 * Fetches the full detail (summary + enrollments) for one linked child.
 * Disabled when studentId is undefined or empty — avoids stale/phantom requests.
 */
export function useChildDetail(studentId: string | undefined) {
  return useQuery({
    queryKey: parentKeys.childDetail(studentId ?? ''),
    queryFn:  () => getChildDetail(studentId!),
    enabled:  Boolean(studentId),
  })
}

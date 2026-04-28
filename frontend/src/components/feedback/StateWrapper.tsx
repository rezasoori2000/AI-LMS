import type { ReactNode } from 'react'
import { LoadingState } from './LoadingState'
import { ErrorState }   from './ErrorState'
import { EmptyState }   from './EmptyState'

// ── Props ─────────────────────────────────────────────────────────────────────

interface StateWrapperProps {
  // ── Trigger flags ────────────────────────────────────────────────────────
  /** Render LoadingState while true. Highest priority. */
  isLoading?: boolean
  /** Render ErrorState while true. Evaluated after isLoading. */
  isError?: boolean
  /**
   * Render EmptyState while true. Evaluated after isLoading + isError.
   * Caller computes this: isEmpty={!data?.length}
   */
  isEmpty?: boolean

  // ── LoadingState props ───────────────────────────────────────────────────
  loadingMessage?: string
  loadingSize?: 'sm' | 'md' | 'lg'

  // ── ErrorState props ─────────────────────────────────────────────────────
  errorTitle?: string
  /** Error object or string. Falls back to a generic message when null/undefined. */
  error?: Error | string | null
  onRetry?: () => void

  // ── EmptyState props ─────────────────────────────────────────────────────
  emptyTitle?: string
  emptyDescription?: string
  emptyAction?: ReactNode

  // ── Layout ───────────────────────────────────────────────────────────────
  /** Applied to the placeholder element when a state is shown. Not applied to children. */
  stateClassName?: string

  children: ReactNode
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function resolveErrorMessage(error?: Error | string | null): string {
  if (!error) return 'An unexpected error occurred. Please try again.'
  if (typeof error === 'string') return error
  return error.message || 'An unexpected error occurred. Please try again.'
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * StateWrapper — declarative async-state orchestration.
 *
 * Renders the correct feedback component based on loading → error → empty
 * priority, and falls through to children when all flags are false.
 *
 * Designed to map directly to TanStack Query return values:
 *
 *   const { isLoading, isError, error, data } = useQuery(...)
 *
 *   <StateWrapper
 *     isLoading={isLoading}
 *     isError={isError}
 *     error={error}
 *     isEmpty={!data?.length}
 *     onRetry={refetch}
 *     emptyTitle="No courses yet"
 *     emptyDescription="Courses you create will appear here."
 *   >
 *     <CourseList data={data} />
 *   </StateWrapper>
 *
 * Placement:
 * - Page-level: wrap PageContainer's children to replace the full page.
 * - Section-level: wrap SectionCard's children to replace only that section's body.
 * - Prefer section-level when multiple independent data regions exist on one page
 *   so other sections remain usable while one loads or fails.
 *
 * Phase 2 note:
 * - For `aria-busy` pattern (region stays mounted while loading), replace
 *   the isLoading return with a wrapper div with aria-busy="true" containing
 *   both the spinner and a visually-hidden copy of children. This is a richer
 *   accessibility pattern but requires structural changes — defer until real
 *   data is wired.
 *
 * Does not render a wrapper element around children.
 */
export function StateWrapper({
  isLoading = false,
  isError   = false,
  isEmpty   = false,

  loadingMessage,
  loadingSize,

  errorTitle,
  error,
  onRetry,

  emptyTitle       = 'Nothing here yet',
  emptyDescription,
  emptyAction,

  stateClassName,
  children,
}: StateWrapperProps) {
  if (isLoading) {
    return (
      <LoadingState
        message={loadingMessage}
        size={loadingSize}
        className={stateClassName}
      />
    )
  }

  if (isError) {
    return (
      <ErrorState
        title={errorTitle}
        message={resolveErrorMessage(error)}
        onRetry={onRetry}
        className={stateClassName}
      />
    )
  }

  if (isEmpty) {
    return (
      <EmptyState
        title={emptyTitle}
        description={emptyDescription}
        action={emptyAction}
        className={stateClassName}
      />
    )
  }

  return <>{children}</>
}

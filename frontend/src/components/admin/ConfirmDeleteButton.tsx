import { useState, useEffect } from 'react'
import { Button } from '@/components/ui/Button'

// ── Props ─────────────────────────────────────────────────────────────────────

interface ConfirmDeleteButtonProps {
  /** Called when the user confirms the delete action. */
  onConfirm:  () => void
  isLoading?: boolean
  /** Label for the primary delete button. Defaults to "Delete". */
  label?:     string
  className?: string
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * ConfirmDeleteButton — two-step inline delete confirmation.
 *
 * Step 1: renders a "Delete" button.
 * Step 2: on click, switches to "Confirm?" + "Cancel" buttons.
 *         If no second click within 4 s, reverts to step 1.
 *
 * No modal or portal required — keeps delete handling simple for MVP.
 * Accessible: focus moves to Confirm button after entering confirm state.
 */
export function ConfirmDeleteButton({
  onConfirm,
  isLoading = false,
  label     = 'Delete',
  className = '',
}: ConfirmDeleteButtonProps) {
  const [confirming, setConfirming] = useState(false)

  // Auto-reset if the user abandons the confirmation
  useEffect(() => {
    if (!confirming) return
    const timer = setTimeout(() => setConfirming(false), 4000)
    return () => clearTimeout(timer)
  }, [confirming])

  if (confirming) {
    return (
      <span className="inline-flex items-center gap-2">
        <Button
          variant="danger"
          size="sm"
          isLoading={isLoading}
          onClick={onConfirm}
          // eslint-disable-next-line jsx-a11y/no-autofocus
          autoFocus
        >
          Confirm?
        </Button>
        <Button
          variant="ghost"
          size="sm"
          disabled={isLoading}
          onClick={() => setConfirming(false)}
        >
          Cancel
        </Button>
      </span>
    )
  }

  return (
    <Button
      variant="danger"
      size="sm"
      className={className}
      onClick={() => setConfirming(true)}
    >
      {label}
    </Button>
  )
}

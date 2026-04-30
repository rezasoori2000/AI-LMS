/**
 * feedback.test.tsx
 *
 * Unit tests for all five shared feedback/state components:
 *   LoadingState, ErrorState, EmptyState, InlineFeedback, StateWrapper
 *
 * i18next is mocked so that t(key) returns the key string. Assertions on
 * default translated strings therefore assert against the i18n key, giving
 * us confidence that the component calls t() rather than using a hardcoded
 * English fallback.
 */
import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'

// Must be declared before the component imports so Vitest hoisting applies.
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}))

import { LoadingState }  from '@/components/feedback/LoadingState'
import { ErrorState }    from '@/components/feedback/ErrorState'
import { EmptyState }    from '@/components/feedback/EmptyState'
import { InlineFeedback } from '@/components/feedback/InlineFeedback'
import { StateWrapper }  from '@/components/feedback/StateWrapper'

// ── LoadingState ──────────────────────────────────────────────────────────────

describe('LoadingState', () => {
  it('renders with role="status"', () => {
    render(<LoadingState />)
    expect(screen.getByRole('status')).toBeInTheDocument()
  })

  it('uses the i18n key as aria-label when no message is passed', () => {
    render(<LoadingState />)
    expect(screen.getByRole('status')).toHaveAttribute('aria-label', 'common.loading')
  })

  it('uses a custom message as aria-label', () => {
    render(<LoadingState message="Loading students…" />)
    expect(screen.getByRole('status')).toHaveAttribute('aria-label', 'Loading students…')
  })

  it('renders a visible caption when a custom message is provided', () => {
    render(<LoadingState message="Loading students…" />)
    expect(screen.getByText('Loading students…')).toBeInTheDocument()
  })

  it('does not render a visible caption when no message is passed', () => {
    // The i18n key is used for aria-label only; it should not appear as DOM text.
    render(<LoadingState />)
    expect(screen.queryByText('common.loading')).not.toBeInTheDocument()
  })
})

// ── ErrorState ────────────────────────────────────────────────────────────────

describe('ErrorState', () => {
  it('renders with role="alert"', () => {
    render(<ErrorState message="Connection failed." />)
    expect(screen.getByRole('alert')).toBeInTheDocument()
  })

  it('uses the i18n key as default title', () => {
    render(<ErrorState message="Connection failed." />)
    expect(screen.getByText('common.somethingWentWrong')).toBeInTheDocument()
  })

  it('renders a custom title', () => {
    render(<ErrorState title="Could not load data" message="Retry later." />)
    expect(screen.getByText('Could not load data')).toBeInTheDocument()
  })

  it('renders the error message', () => {
    render(<ErrorState message="Check your connection." />)
    expect(screen.getByText('Check your connection.')).toBeInTheDocument()
  })

  it('renders a retry button when onRetry is provided', () => {
    render(<ErrorState message="Failed." onRetry={() => {}} />)
    expect(screen.getByRole('button')).toBeInTheDocument()
  })

  it('calls onRetry when the retry button is clicked', () => {
    const handler = vi.fn()
    render(<ErrorState message="Failed." onRetry={handler} />)
    fireEvent.click(screen.getByRole('button'))
    expect(handler).toHaveBeenCalledTimes(1)
  })

  it('does not render a retry button when onRetry is absent', () => {
    render(<ErrorState message="Failed." />)
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })
})

// ── EmptyState ────────────────────────────────────────────────────────────────

describe('EmptyState', () => {
  it('renders with role="status"', () => {
    render(<EmptyState title="No courses yet" />)
    expect(screen.getByRole('status')).toBeInTheDocument()
  })

  it('renders the title', () => {
    render(<EmptyState title="No courses yet" />)
    expect(screen.getByText('No courses yet')).toBeInTheDocument()
  })

  it('renders description when provided', () => {
    render(<EmptyState title="No courses" description="Create your first course." />)
    expect(screen.getByText('Create your first course.')).toBeInTheDocument()
  })

  it('omits the description element when absent', () => {
    const { container } = render(<EmptyState title="No courses" />)
    expect(container.querySelector('p')).toBeNull()
  })

  it('renders the action slot', () => {
    render(
      <EmptyState
        title="No courses"
        action={<button>Create course</button>}
      />,
    )
    expect(screen.getByRole('button', { name: 'Create course' })).toBeInTheDocument()
  })
})

// ── InlineFeedback ────────────────────────────────────────────────────────────

describe('InlineFeedback', () => {
  it('uses role="alert" for error intent', () => {
    render(<InlineFeedback intent="error" message="Form is invalid." />)
    expect(screen.getByRole('alert')).toBeInTheDocument()
  })

  it('uses role="alert" for warning intent', () => {
    render(<InlineFeedback intent="warning" message="Unsaved changes." />)
    expect(screen.getByRole('alert')).toBeInTheDocument()
  })

  it('uses role="status" for success intent', () => {
    render(<InlineFeedback intent="success" message="Changes saved." />)
    expect(screen.getByRole('status')).toBeInTheDocument()
  })

  it('uses role="status" for info intent', () => {
    render(<InlineFeedback intent="info" message="Read-only mode." />)
    expect(screen.getByRole('status')).toBeInTheDocument()
  })

  it('renders the message text', () => {
    render(<InlineFeedback intent="success" message="Your changes were saved." />)
    expect(screen.getByText('Your changes were saved.')).toBeInTheDocument()
  })
})

// ── StateWrapper ──────────────────────────────────────────────────────────────

describe('StateWrapper', () => {
  it('renders children when all flags are false', () => {
    render(<StateWrapper><p>Content loaded</p></StateWrapper>)
    expect(screen.getByText('Content loaded')).toBeInTheDocument()
  })

  it('renders LoadingState and hides children when isLoading=true', () => {
    render(<StateWrapper isLoading><p>Content</p></StateWrapper>)
    expect(screen.getByRole('status')).toBeInTheDocument()
    expect(screen.queryByText('Content')).not.toBeInTheDocument()
  })

  it('renders ErrorState and hides children when isError=true', () => {
    render(
      <StateWrapper isError error="Network error">
        <p>Content</p>
      </StateWrapper>,
    )
    expect(screen.getByRole('alert')).toBeInTheDocument()
    expect(screen.queryByText('Content')).not.toBeInTheDocument()
  })

  it('renders EmptyState and hides children when isEmpty=true', () => {
    render(
      <StateWrapper isEmpty emptyTitle="No items">
        <p>Content</p>
      </StateWrapper>,
    )
    expect(screen.getByRole('status')).toBeInTheDocument()
    expect(screen.queryByText('Content')).not.toBeInTheDocument()
  })

  it('gives loading priority over error when both flags are true', () => {
    render(
      <StateWrapper isLoading isError error="Fail">
        <p>Content</p>
      </StateWrapper>,
    )
    // LoadingState (role="status") renders; ErrorState (role="alert") does not.
    expect(screen.getByRole('status')).toBeInTheDocument()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('passes a custom emptyTitle to EmptyState', () => {
    render(
      <StateWrapper isEmpty emptyTitle="No students enrolled">
        <p>Content</p>
      </StateWrapper>,
    )
    expect(screen.getByText('No students enrolled')).toBeInTheDocument()
  })

  it('uses the i18n key as default emptyTitle', () => {
    render(<StateWrapper isEmpty><p>Content</p></StateWrapper>)
    expect(screen.getByText('common.nothingHereYet')).toBeInTheDocument()
  })

  it('extracts the message from an Error object', () => {
    render(
      <StateWrapper isError error={new Error('Timeout')}>
        <p>Content</p>
      </StateWrapper>,
    )
    expect(screen.getByText('Timeout')).toBeInTheDocument()
  })

  it('uses the i18n fallback when no error detail is given', () => {
    render(<StateWrapper isError><p>Content</p></StateWrapper>)
    expect(screen.getByText('common.anErrorOccurred')).toBeInTheDocument()
  })
})

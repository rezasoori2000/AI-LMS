interface TopbarProps {
  onMenuClick: () => void
  isMenuOpen:  boolean
}

/**
 * Application top bar — always visible, full-width.
 *
 * Contains:
 * - Hamburger button (mobile only, md:hidden) — controls sidebar drawer
 * - Brand name (desktop only — sidebar shows it on mobile)
 * - Right-side action area (placeholder for avatar/notifications in Phase 2)
 *
 * Accessibility:
 * - Hamburger has `aria-controls="sidebar"` and `aria-expanded` tied to state
 * - `role="banner"` on <header> is implicit but explicit here for clarity
 *
 * RTL: padding uses `px-` (symmetric) so no directional fix needed here.
 * Action area uses `ms-auto` (margin-inline-start) so it stays at the
 * trailing edge in both LTR and RTL.
 */
export function Topbar({ onMenuClick, isMenuOpen }: TopbarProps) {
  return (
    <header
      role="banner"
      className="flex h-topbar flex-shrink-0 items-center border-b border-stroke bg-surface px-4 md:px-6"
    >
      {/* Mobile hamburger — hidden on desktop where sidebar is always visible */}
      <button
        type="button"
        aria-label={isMenuOpen ? 'Close navigation menu' : 'Open navigation menu'}
        aria-controls="sidebar"
        aria-expanded={isMenuOpen}
        onClick={onMenuClick}
        className={[
          'me-4 flex items-center justify-center rounded-md p-1.5',
          'text-content-secondary',
          'hover:bg-surface-overlay',
          'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500',
          'transition-colors duration-150',
          'md:hidden', // hidden on md+ — sidebar is always visible there
        ].join(' ')}
      >
        {/* Simple hamburger / close icon */}
        <svg
          className="h-5 w-5"
          aria-hidden="true"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          {isMenuOpen ? (
            /* × close icon */
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M6 18L18 6M6 6l12 12"
            />
          ) : (
            /* ≡ hamburger icon */
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M4 6h16M4 12h16M4 18h16"
            />
          )}
        </svg>
      </button>

      {/* Brand — shown only on desktop (sidebar already shows it on mobile) */}
      <span className="hidden font-semibold text-brand-600 md:block">AI-LMS</span>

      {/* Trailing actions — Phase 2: notifications, language switcher, user menu */}
      <div className="ms-auto flex items-center gap-2">
        <div
          aria-hidden="true"
          title="Sign in — coming in Section 2"
          className="flex h-8 w-8 cursor-default items-center justify-center rounded-full bg-brand-100 text-brand-700 text-xs font-semibold select-none"
        >
          ?
        </div>
      </div>
    </header>
  )
}

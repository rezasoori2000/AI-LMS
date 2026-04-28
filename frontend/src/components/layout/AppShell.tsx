import type { ReactNode } from 'react'
import { Topbar } from './Topbar'
import { Sidebar } from './Sidebar'
import { useSidebar } from '@/hooks/use-sidebar'
import type { NavItem } from '@/types'

interface AppShellProps {
  children:  ReactNode
  navItems:  NavItem[]
}

/**
 * AppShell — the full-page layout frame for authenticated screens.
 *
 * Structure:
 * ┌────────────────────────────┐
 * │   Topbar (h-topbar)       │  fixed height, full width
 * ├──────────┬─────────────────┤
 * │          │                 │
 * │ Sidebar  │  <main>         │  sidebar: fixed on mobile, static on desktop
 * │ (w-sidebar)  (flex-1)      │  main: overflow-y-auto (scrollable content)
 * │          │                 │
 * └──────────┴─────────────────┘
 *
 * RTL awareness:
 * - The outer `flex` row respects the document's writing direction.
 *   When `dir="rtl"` is set on <html>, flex row-direction places the first
 *   child (Sidebar) on the RIGHT and the main content on the LEFT — exactly
 *   what RTL users expect.
 * - No JS direction logic needed here; CSS logical flow handles it.
 *
 * Mobile drawer:
 * - Sidebar uses position:fixed + translate when open.
 * - A semi-transparent overlay backdrop is rendered below the sidebar (z-20)
 *   so tapping outside closes it.
 * - TODO (Phase 2): add body scroll-lock when drawer is open.
 *
 * Skip-nav:
 * - The <main> element has id="main-content" and tabIndex={-1} so a
 *   "Skip to main content" link (to be added to AuthLayout / root) can
 *   send keyboard focus directly past the nav.
 */
export function AppShell({ children, navItems }: AppShellProps) {
  const { isOpen, toggle, close } = useSidebar()

  return (
    <div className="flex h-screen flex-col overflow-hidden bg-surface-raised">
      {/* Top bar */}
      <Topbar onMenuClick={toggle} isMenuOpen={isOpen} />

      {/* Body row: sidebar + scrollable content */}
      <div className="flex flex-1 overflow-hidden">
        {/* Mobile backdrop — tapping it closes the drawer */}
        {isOpen && (
          <div
            aria-hidden="true"
            className="fixed inset-0 z-20 bg-black/40 md:hidden"
            onClick={close}
          />
        )}

        {/* Sidebar */}
        <Sidebar navItems={navItems} isOpen={isOpen} onClose={close} />

        {/* Main content area */}
        <main
          id="main-content"
          tabIndex={-1}
          className="flex-1 overflow-y-auto focus:outline-none"
        >
          {children}
        </main>
      </div>
    </div>
  )
}

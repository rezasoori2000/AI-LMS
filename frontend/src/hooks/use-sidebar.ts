import { useState, useCallback } from 'react'

/**
 * Manages the sidebar's open/closed state.
 *
 * On mobile, this controls whether the slide-over drawer is open.
 * On desktop (md+), the sidebar is always visible and this state is ignored.
 *
 * Kept outside AppShell so any component (e.g. a keyboard shortcut listener)
 * can control the sidebar without prop-drilling.
 */
export function useSidebar() {
  const [isOpen, setIsOpen] = useState(false)

  const open   = useCallback(() => setIsOpen(true),  [])
  const close  = useCallback(() => setIsOpen(false), [])
  const toggle = useCallback(() => setIsOpen((prev) => !prev), [])

  return { isOpen, open, close, toggle }
}

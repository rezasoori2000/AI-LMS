import { useTranslation } from 'react-i18next'
import { NavLink } from 'react-router-dom'
import type { NavItem } from '@/types'

interface SidebarProps {
  navItems: NavItem[]
  isOpen:   boolean
  onClose:  () => void
}

/**
 * Application navigation sidebar.
 *
 * Layout behaviour:
 * - Mobile:  position:fixed slide-over drawer, triggered by the Topbar hamburger.
 *            Starts off-screen (start edge) and translates into view when open.
 * - Desktop (md+): position:static, always visible, no translate needed.
 *
 * RTL safety:
 * - `start-0` = inset-inline-start:0 (left in LTR, right in RTL)
 * - Off-screen idle: `-translate-x-full` (LTR) / `rtl:translate-x-full` (RTL)
 *   Both set `--tw-translate-x` on the same property; the rtl: variant wins in RTL.
 * - `md:translate-x-0` resets the transform once the sidebar is static on desktop.
 *
 * Nav item content:
 * - `ps-3 pe-3` uses logical padding (start/end instead of left/right)
 * - `gap-3 ms-*` for icon+label spacing is direction-independent
 *
 * Phase 2 TODO:
 * - Add icons via lucide-react to each NavItem
 * - Show the real current user's name and role in the footer slot
 * - Add a "Log out" button to the footer
 */
export function Sidebar({ navItems, isOpen, onClose }: SidebarProps) {
  const { t } = useTranslation()

  return (
    <aside
      id="sidebar"
      aria-label={t('nav.home')} // replaced by a proper label once i18n key exists
      className={[
        // Base: fixed full-height panel on mobile
        'fixed inset-y-0 start-0 z-30 flex w-sidebar flex-col',
        'bg-sidebar-bg',
        // Smooth slide animation
        'transition-transform duration-300 ease-in-out',
        // Desktop override: static, always visible, no transform
        'md:static md:z-auto md:translate-x-0',
        // Mobile: hidden (off-start) when closed, visible when open
        isOpen ? 'translate-x-0' : '-translate-x-full rtl:translate-x-full',
      ].join(' ')}
    >
      {/* ── Brand / logo area ────────────────────────────────────────────── */}
      <div className="flex h-topbar flex-shrink-0 items-center px-5 border-b border-white/10">
        <span className="text-base font-bold tracking-tight text-white">AI-LMS</span>
      </div>

      {/* ── Navigation items ─────────────────────────────────────────────── */}
      <nav aria-label="Sidebar navigation" className="flex-1 overflow-y-auto px-3 py-4">
        <ul role="list" className="space-y-0.5">
          {navItems.map((item) => (
            <li key={item.key}>
              {/* Section label — aria-hidden so it doesn't duplicate nav link context for screen readers */}
              {item.sectionTitleKey && (
                <div
                  aria-hidden="true"
                  className="px-3 pt-5 pb-1 text-[10px] font-semibold uppercase tracking-widest text-sidebar-text/50"
                >
                  {t(item.sectionTitleKey)}
                </div>
              )}
              <NavLink
                to={item.href}
                // `end` on the home link prevents it lighting up on every route
                end={item.href === '/'}
                // Close the mobile drawer when the user picks a link
                onClick={onClose}
                className={({ isActive }) =>
                  [
                    'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium',
                    'transition-colors duration-150',
                    isActive
                      ? 'bg-sidebar-active-bg text-sidebar-active-text'
                      : 'text-sidebar-text hover:bg-sidebar-hover-bg hover:text-white',
                  ].join(' ')
                }
              >
                {/* Icon slot — populated in Phase 2 with lucide-react */}
                {item.icon && (
                  <span aria-hidden="true" className="flex-shrink-0 w-4 h-4">
                    {item.icon}
                  </span>
                )}
                <span>{t(item.labelKey)}</span>
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>

      {/* ── User identity footer ─────────────────────────────────────────── */}
      {/* Phase 2: replace static "Guest" text with real user name + avatar  */}
      <div className="flex-shrink-0 border-t border-white/10 p-4">
        <div className="flex items-center gap-3">
          <div
            aria-hidden="true"
            className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full bg-brand-600 text-white text-xs font-semibold"
          >
            ?
          </div>
          <div className="min-w-0">
            <p className="truncate text-sm font-medium text-white">Guest</p>
            <p className="truncate text-xs text-sidebar-text">Not signed in</p>
          </div>
        </div>
      </div>
    </aside>
  )
}

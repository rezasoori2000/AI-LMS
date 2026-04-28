import type { UserRole } from '@/types'

/**
 * Defines who may access a route.
 *
 * This is METADATA ONLY in Phase 1 — no enforcement happens.
 *
 * How Phase 2 will use this:
 *   A root-level router `loader` reads the deepest matched route's handle via
 *   `matchRoutes()`, checks `handle.access`, and redirects when the session user
 *   doesn't qualify.
 *
 *   // Phase 2 loader (pseudocode):
 *   export async function authLoader({ request }) {
 *     const matches = matchRoutes(routes, new URL(request.url).pathname)
 *     const access  = (matches?.at(-1)?.route.handle as RouteHandle)?.access ?? 'public'
 *     if (access === 'public') return null
 *     const session = await getSession()
 *     if (!session) throw redirect(`/auth/login?next=${encodeURIComponent(url.pathname)}`)
 *     if (Array.isArray(access) && !access.includes(session.role)) throw redirect('/403')
 *     return null
 *   }
 *
 * Key insight: changing a route's access requirement is a ONE-FIELD change in
 * the route module file. No component code needs to change.
 */
export type RouteAccess =
  | 'public'          // no session required
  | 'authenticated'   // any logged-in user, regardless of role
  | UserRole[]        // only users whose role is in this array

/**
 * Custom handle object attached to route definitions via React Router v6's
 * built-in `handle` field.  Typed as `unknown` by the router, narrowed by us.
 *
 * Reading in any component (e.g. ComingSoonPage, breadcrumb hook):
 *   const matches = useMatches()
 *   const handle  = matches.at(-1)?.handle as RouteHandle | undefined
 *
 * Convention: every leaf route (index routes and named path segments that render
 * a page element) should have a RouteHandle. Parent grouping routes that have
 * no element and exist only to namespace paths (e.g. the bare `admin` segment)
 * do not need one.
 */
export interface RouteHandle {
  /**
   * i18n key for the page title.
   * Used by: ComingSoonPage header, future breadcrumb, future <title> hook.
   */
  titleKey: string
  /**
   * Access level required to visit this route.
   * Missing access on a handle is treated as 'public' by guards.
   */
  access: RouteAccess
}

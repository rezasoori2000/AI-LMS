/**
 * Barrel export for shell and layout components.
 *
 * Import from here:
 *   import { AppShell, Topbar, Sidebar, PageContainer } from '@/components/layout'
 *
 * These components are intentionally not re-exported from a top-level
 * components barrel because they are large, stateful, or depend on React
 * Router hooks — importing them eagerly from a shared barrel would pull them
 * into every bundle that imports any component.  Import from this barrel only
 * in files that specifically need shell composition (layouts/, integration tests).
 */
export { AppShell }      from './AppShell'
export { Sidebar }       from './Sidebar'
export { Topbar }        from './Topbar'
export { PageContainer } from './PageContainer'

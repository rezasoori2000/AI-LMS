/**
 * Navigation configuration.
 *
 * Nav items are data-driven so the Sidebar component stays dumb:
 * it just renders whatever it receives as props.
 *
 * In Section 2, AppLayout will call:
 *   const items = NAV_ITEMS_BY_ROLE[currentUser.role]
 * and pass them down to AppShell → Sidebar.
 *
 * Phase 2 TODO: add `icon` field using lucide-react icons.
 * Phase 2 TODO: add `requiredPermission` field for granular RBAC.
 */
import type { NavItem, UserRole } from '@/types'

// ── Per-role navigation maps ──────────────────────────────────────────────────
// Sub-routes (e.g. /admin/tenants) will return 404 until those pages are
// built in Phase 2+. The structure is correct; only the pages are pending.

export const NAV_ITEMS_BY_ROLE: Record<UserRole, NavItem[]> = {
  super_admin: [
    { key: 'dashboard', labelKey: 'nav.dashboard', href: '/admin',
      allowedRoles: ['super_admin', 'tenant_admin'] },
    { key: 'tenants',  labelKey: 'nav.tenants',   href: '/admin/tenants',
      sectionTitleKey: 'nav.groups.management',
      allowedRoles: ['super_admin'] },
    { key: 'users',    labelKey: 'nav.users',     href: '/admin/users',
      allowedRoles: ['super_admin', 'tenant_admin'] },
    { key: 'settings', labelKey: 'nav.settings',  href: '/admin/settings',
      allowedRoles: ['super_admin', 'tenant_admin'] },
  ],

  tenant_admin: [
    { key: 'dashboard', labelKey: 'nav.dashboard', href: '/admin',
      allowedRoles: ['tenant_admin'] },
    { key: 'teachers', labelKey: 'nav.teachers',  href: '/admin/teachers',
      sectionTitleKey: 'nav.groups.management',
      allowedRoles: ['tenant_admin'] },
    { key: 'students', labelKey: 'nav.students',  href: '/admin/students',
      allowedRoles: ['tenant_admin'] },
    { key: 'courses',  labelKey: 'nav.courses',   href: '/admin/courses',
      allowedRoles: ['tenant_admin', 'content_editor'] },
    { key: 'settings', labelKey: 'nav.settings',  href: '/admin/settings',
      allowedRoles: ['tenant_admin'] },
  ],

  content_editor: [
    { key: 'dashboard', labelKey: 'nav.dashboard', href: '/teacher',
      allowedRoles: ['content_editor'] },
    { key: 'courses',  labelKey: 'nav.courses',   href: '/teacher/courses',
      sectionTitleKey: 'nav.groups.content',
      allowedRoles: ['content_editor'] },
    { key: 'lessons',  labelKey: 'nav.lessons',   href: '/teacher/lessons',
      allowedRoles: ['content_editor'] },
  ],

  teacher: [
    { key: 'dashboard', labelKey: 'nav.dashboard', href: '/teacher',
      allowedRoles: ['teacher'] },
    { key: 'students', labelKey: 'nav.students',  href: '/teacher/students',
      sectionTitleKey: 'nav.groups.classes',
      allowedRoles: ['teacher'] },
    { key: 'lessons',  labelKey: 'nav.lessons',   href: '/teacher/lessons',
      allowedRoles: ['teacher'] },
    { key: 'progress', labelKey: 'nav.progress',  href: '/teacher/progress',
      allowedRoles: ['teacher'] },
  ],

  parent: [
    { key: 'dashboard', labelKey: 'nav.dashboard', href: '/parent',
      allowedRoles: ['parent'] },
    { key: 'children', labelKey: 'nav.children',  href: '/parent/children',
      sectionTitleKey: 'nav.groups.learning',
      allowedRoles: ['parent'] },
    { key: 'progress', labelKey: 'nav.progress',  href: '/parent/progress',
      allowedRoles: ['parent'] },
  ],

  student: [
    { key: 'dashboard', labelKey: 'nav.dashboard', href: '/student',
      allowedRoles: ['student'] },
    { key: 'lessons',  labelKey: 'nav.lessons',   href: '/student/lessons',
      sectionTitleKey: 'nav.groups.learning',
      allowedRoles: ['student'] },
    { key: 'progress', labelKey: 'nav.progress',  href: '/student/progress',
      allowedRoles: ['student'] },
  ],
}

// ── Default nav (no auth) ─────────────────────────────────────────────────────
// Shown until Section 2 provides a real current user.
// Lets developers navigate to any dashboard during development.
export const DEFAULT_NAV_ITEMS: NavItem[] = [
  { key: 'home',    labelKey: 'nav.home',             href: '/' },
  { key: 'admin',   labelKey: 'nav.adminDashboard',   href: '/admin' },
  { key: 'teacher', labelKey: 'nav.teacherDashboard', href: '/teacher' },
  { key: 'parent',  labelKey: 'nav.parentDashboard',  href: '/parent' },
  { key: 'student', labelKey: 'nav.studentDashboard', href: '/student' },
]

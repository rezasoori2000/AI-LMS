# Routing and Layouts

All routing uses **React Router v6** with `createBrowserRouter`.

---

## Router structure

```
/ (AppLayout — authenticated shell)
├── /                    → HomePage              (dev nav hub; Phase 2 → post-login redirect)
├── /admin               → AdminDashboardPage
│   ├── /admin/tenants   → ComingSoonPage
│   ├── /admin/users     → ComingSoonPage
│   ├── /admin/teachers  → ComingSoonPage
│   ├── /admin/students  → ComingSoonPage
│   ├── /admin/courses   → ComingSoonPage
│   └── /admin/settings  → ComingSoonPage
├── /teacher             → TeacherDashboardPage
│   ├── /teacher/students → ComingSoonPage
│   ├── /teacher/lessons  → ComingSoonPage
│   ├── /teacher/courses  → ComingSoonPage
│   └── /teacher/progress → ComingSoonPage
├── /parent              → ParentDashboardPage
│   ├── /parent/children  → ComingSoonPage
│   └── /parent/progress  → ComingSoonPage
└── /student             → StudentDashboardPage
    ├── /student/lessons  → ComingSoonPage
    └── /student/progress → ComingSoonPage

/auth (AuthLayout — unauthenticated shell)
└── /auth/*              → (login, register, forgot-password — Phase 2)

/403                     → ForbiddenPage         (role guard redirect target — Phase 2)
*   (catch-all)          → NotFoundPage
```

---

## Route modules

Each role has its own module file so the router config stays readable:

```
src/routes/modules/
├── admin.routes.tsx
├── teacher.routes.tsx
├── parent.routes.tsx
└── student.routes.tsx
```

Modules export a `RouteObject[]` array. They are spread into `AppLayout`'s children array in `src/routes/index.tsx`:

```ts
// src/routes/index.tsx
children: [
  ...adminRoutes,
  ...teacherRoutes,
  ...parentRoutes,
  ...studentRoutes,
]
```

---

## RouteHandle

Every route carries a typed `handle` object:

```ts
// src/routes/types.ts
type RouteAccess = 'public' | 'authenticated' | UserRole[]

interface RouteHandle {
  /** i18n key for the page title — e.g. "nav.admin" */
  titleKey: string
  /** Who may access this route. Enforced by auth guard in Phase 2. */
  access: RouteAccess
}
```

`access` is set on every leaf route today. Adding auth guards in Phase 2 requires only a route `loader` — no route-module changes.

---

## Layouts

### `AppLayout` (`src/layouts/AppLayout.tsx`)

- Renders `AppShell` (Sidebar + Topbar).
- Reads `useMatches()` to derive the active nav item.
- Supplies `nav.ts` items to Sidebar, grouped by section.

### `AuthLayout` (`src/layouts/AuthLayout.tsx`)

- Centred, unauthenticated shell.
- Used for login, password reset, invite flows (Phase 2).

---

## Navigation

`src/config/nav.ts` holds the canonical nav item list. Each item has:

```ts
{
  key: string            // unique React list key
  labelKey: string       // i18n key → t(labelKey) renders the label
  href: string           // route path passed to <NavLink to={href}>
  icon?: ReactNode       // Phase 2: lucide-react icon; optional for now
  allowedRoles?: UserRole[]   // documents which roles see this item (metadata-only in Phase 1)
  sectionTitleKey?: string    // i18n key for the section group heading
}
```

Sidebar groups items into sections using `sectionTitleKey`. Role-based filtering is not live yet — all items render. Phase 2 wires `allowedRoles` to the authenticated user.

---

## ComingSoonPage

`src/pages/placeholders/ComingSoonPage.tsx` is used by all sub-routes that are not yet built. It reads the current route's `handle.titleKey` via `useMatches()` and shows a back link to the parent path. Replace by creating the real page and swapping the `element` in the route module.

---

## Deferred to Phase 2

- Auth guard loader (`RouteHandle.access` → redirect to `/auth/login` or `/403`)
- Role-filtered nav (`NAV_ITEMS_BY_ROLE[currentUser.role]`)
- Protected route wrapper for guest routes

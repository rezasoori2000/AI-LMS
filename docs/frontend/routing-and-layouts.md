# Routing and Layouts

All routing uses **React Router v6** with `createBrowserRouter`.

---

## Router structure

```
/ (AppLayout — authenticated shell)
├── /admin               → AdminDashboardPage
│   ├── /admin/users     → ComingSoonPage
│   ├── /admin/tenants   → ComingSoonPage
│   ├── /admin/courses   → ComingSoonPage
│   ├── /admin/content   → ComingSoonPage
│   ├── /admin/settings  → ComingSoonPage
│   └── /admin/reports   → ComingSoonPage
├── /teacher             → TeacherDashboardPage
│   ├── /teacher/classes → ComingSoonPage
│   ├── /teacher/lessons → ComingSoonPage
│   ├── /teacher/assignments → ComingSoonPage
│   └── /teacher/students   → ComingSoonPage
├── /parent              → ParentDashboardPage
│   ├── /parent/children → ComingSoonPage
│   └── /parent/schedule → ComingSoonPage
└── /student             → StudentDashboardPage
    ├── /student/courses  → ComingSoonPage
    └── /student/progress → ComingSoonPage

/ (AuthLayout — unauthenticated)
└── /auth/*              → (reserved, not yet built)

* (catch-all) → NotFoundPage
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

`src/app/nav.ts` holds the canonical nav item list. Each item has:

```ts
{
  key: string           // unique ID
  labelKey: string      // i18n key
  path: string          // route path
  icon: ReactNode       
  allowedRoles: UserRole[]   // documents which roles see this item
  sectionTitleKey?: string   // i18n key for the section group heading
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

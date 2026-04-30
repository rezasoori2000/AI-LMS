# Phase 1 — Section 2 Readiness Checklist

Use this checklist before starting **Phase 1, Section 3** (Authentication and role-based access control).
Every item should be verified in a current checkout of the repo.

---

## Automated checks (run these first)

```bash
cd frontend
npm install
npm run type-check      # must exit 0, 0 TS errors
npm run lint            # must exit 0
  npm run test -- --run   # must pass: 41 tests
npm run build           # 142 modules, no errors
```

Expected build output:
- 143 modules
- JS: ~325 KB / ~101 KB gzip
- CSS: ~20 KB / ~4.8 KB gzip

**Verified ✅ — Phase 1 Section 2 Parts 1–7 complete (2026-04-30)**

---

## App shell

- [ ] `npm run dev` starts at `http://localhost:5173`
- [ ] The default route (`/`) renders `AdminDashboardPage` inside the app shell (Sidebar + Topbar)
- [ ] Sidebar renders grouped navigation sections with headings
- [ ] Topbar renders the page title derived from the active route's `titleKey`
- [ ] An unknown route renders `NotFoundPage` (no app shell)

## Routing

- [ ] `/admin` → `AdminDashboardPage`
- [ ] `/teacher` → `TeacherDashboardPage`
- [ ] `/parent` → `ParentDashboardPage`
- [ ] `/student` → `StudentDashboardPage`
- [ ] `/admin/users` → `ComingSoonPage` (and all other sub-routes)
- [ ] `ComingSoonPage` shows the section title from the route's `titleKey`
- [ ] `ComingSoonPage` back link navigates to the parent dashboard

## Dashboard pages

- [ ] All 4 dashboard pages render without console errors
- [ ] Each page shows a stats row and multiple `SectionCard` sections
- [ ] Each section uses `PlaceholderRow` items (Phase 1 data placeholders)
- [ ] No page has hardcoded English strings outside of `// TODO i18n` comments

## Theme and tokens

- [ ] No raw `gray-*`, `slate-*`, `zinc-*`, `green-*`, `red-*` etc. color classes in component files
- [ ] All color classes reference token names: `text-content-*`, `bg-surface*`, `border-stroke`, `bg-success-light`, etc.
- [ ] `shadow-card` used on cards (not `shadow-sm`)
- [ ] `rounded-lg` used on cards, `rounded-md` on inputs/buttons, `rounded-full` on badges

## RTL/LTR

- [ ] No `ml-*`, `mr-*`, `pl-*`, `pr-*` spacing classes in new component files (use logical: `ms-*`, `me-*`, `ps-*`, `pe-*`)
- [ ] `RTL_LOCALES` has no duplicates outside `src/utils/direction.ts`
- [ ] Setting `document.documentElement.dir = 'rtl'` flips the sidebar and layout (manual browser test)

## Shared UI components

- [ ] `import { Button, Card, Input, StatCard, SectionCard, SkeletonBlock } from '@/components/ui'` resolves
- [ ] `import { StateWrapper, InlineFeedback, LoadingState, ErrorState, EmptyState } from '@/components/feedback'` resolves
- [ ] `StateWrapper` correctly renders `LoadingState` when `isLoading={true}`
- [ ] `StateWrapper` correctly renders `ErrorState` when `isError={true}`
- [ ] `StateWrapper` correctly renders `EmptyState` when `isEmpty={true}`
- [ ] `StateWrapper` renders `children` when all flags are false

## Localization

- [ ] `src/i18n/locales/en.json` has keys for all nav items, section headings, and dashboard content
- [ ] No untranslated strings visible in the EN build
- [ ] `t('nav.admin')` and peer keys resolve without missing-key warnings in the console

## Documentation

- [ ] `docs/frontend/ui-foundation.md` exists
- [ ] `docs/frontend/routing-and-layouts.md` exists
- [ ] `docs/frontend/styling-and-theming.md` exists
- [ ] `docs/frontend/component-conventions.md` exists
- [ ] README Section 2 completion block is present and accurate

---

## Issues that must be fixed before Section 3

None at Section 2 close. The items below are acknowledged deferred work — they are **not** blockers for Section 3 but must be completed by the end of Phase 1, Section 3.

| Item | When |
|---|---|
| Auth guard loader on `AppLayout` route | Section 3 — auth integration |
| Role-filtered nav items | Section 3 — after `currentUser` is available |
| `AuthLayout` login/register pages | Section 3 |
| React Error Boundary at route level | Section 3 |
| Toast/snackbar for transient mutation feedback | First section with mutations |
| `Textarea` + `Select` primitives | First section with forms |
| Dark mode toggle UI | Before Section 2 feature freeze |

---

## What is intentionally not here yet

These are correct omissions — do not add them to Section 2.

- No real API calls — all data is static/placeholder
- No authentication state or JWT handling
- No tenant isolation or scoping
- No form submit handlers
- No TanStack Query `useQuery`/`useMutation` calls
- No user session or role awareness at runtime

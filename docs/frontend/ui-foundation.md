# Frontend UI Foundation

Established in **Phase 1, Section 2**. This document covers the structure, conventions, and extension points for the frontend UI layer.

---

## Folder layout

```
frontend/src/
├── app/               # Root providers and App component
├── assets/            # Static assets
├── components/
│   ├── feedback/      # Async/state UI: EmptyState, LoadingState, ErrorState, InlineFeedback, StateWrapper
│   ├── layout/        # App shell: AppShell, Sidebar, Topbar, PageContainer
│   └── ui/            # Primitives: Button, Badge, Card, Input, StatCard, SectionCard, PlaceholderRow, SkeletonBlock/Text
├── hooks/             # Custom hooks (use-direction, …)
├── i18n/              # i18next setup and locale files
├── layouts/           # Route-level layouts: AppLayout, AuthLayout
├── pages/
│   ├── dashboards/    # Role dashboards: Admin, Teacher, Parent, Student
│   └── placeholders/  # ComingSoonPage (used by all sub-routes not yet built)
├── routes/
│   ├── index.tsx      # Router configuration
│   ├── types.ts       # RouteHandle, RouteAccess types
│   └── modules/       # Route modules per role: admin, teacher, parent, student
├── services/          # API client (pre-wired, not connected yet)
├── store/             # Global state (reserved, empty for now)
├── types/             # Shared TypeScript types
└── utils/             # direction.ts (RTL_LOCALES, isRtl, applyDirection)
```

---

## Component tiers

### `components/ui/` — visual primitives

| Export | Purpose |
|---|---|
| `Button` | Primary, secondary, ghost, danger variants; sm/md/lg sizes |
| `Badge` | Inline status chip: success, warning, error, info, default |
| `Card` / `CardHeader` / `CardBody` | Surface container for content blocks |
| `Input` | Accessible form field with label, helper text, error |
| `StatCard` | Metric tile (label / value / sub-label) |
| `SectionCard` | Card + CardHeader + CardBody composition shortcut |
| `PlaceholderRow` | Accent-dot list row for Phase 1 placeholder content |
| `SkeletonBlock` | Pulsing rectangle for skeleton loading |
| `SkeletonText` | Stacked SkeletonBlock rows for text/list loading |

Import everything from the barrel:
```ts
import { Button, SectionCard, SkeletonText } from '@/components/ui'
```

### `components/feedback/` — async/state components

| Export | Purpose |
|---|---|
| `LoadingState` | Centred spinner; `role="status"` + `aria-label` |
| `EmptyState` | No-data placeholder; `role="status"` |
| `ErrorState` | Error message + optional retry; `role="alert"` |
| `InlineFeedback` | Compact banner for form/mutation feedback |
| `StateWrapper` | Declarative orchestrator — loading → error → empty → children |

Import from the barrel:
```ts
import { StateWrapper, InlineFeedback } from '@/components/feedback'
```

### `components/layout/` — shell

| File | Purpose |
|---|---|
| `AppShell` | Sidebar + Topbar layout frame |
| `Sidebar` | Navigation with section groups and active state |
| `Topbar` | Top bar with title breadcrumb |
| `PageContainer` | `<section>` with accessible heading, max-width, padding |

---

## Page-level vs section-level state

Use **page-level** state when the page has a single data dependency:
```tsx
<StateWrapper isLoading={isLoading} isError={isError} isEmpty={!data?.length}>
  <PageBody data={data} />
</StateWrapper>
```

Use **section-level** state (preferred for dashboards) when multiple independent regions exist:
```tsx
<SectionCard title="Recent Activity">
  <StateWrapper isLoading={isLoading} isError={isError} isEmpty={!items?.length}>
    <ActivityList items={items} />
  </StateWrapper>
</SectionCard>
```
Section-level keeps the rest of the dashboard usable while one region loads or fails.

---

## Skeleton loading convention

Prefer inline skeletons over full-page spinners for section content:
```tsx
<SectionCard title="Upcoming Lessons">
  {isLoading ? <SkeletonText lines={4} /> : <LessonList />}
</SectionCard>
```
For image/chart placeholders use `SkeletonBlock` with an explicit `height`:
```tsx
<SkeletonBlock height="h-48" />
```

---

## `PlaceholderRow` — migration marker

`PlaceholderRow` is an intentional Phase 1 marker. When real data arrives for a section:
1. Delete the `<PlaceholderRow>` items.
2. Replace with an API-driven list component.
3. The `SectionCard` wrapper stays unchanged.

---

## Deferred to Phase 2

- Real API wiring via TanStack Query (`useQuery`, `useMutation`)
- Auth guards on routes
- Dark mode toggle (CSS var switch — zero component changes needed)
- Toast/snackbar for transient mutation feedback
- React Error Boundary at route level
- `Textarea` and `Select` primitives (follow `Input.tsx` pattern)
- `aria-busy` loading pattern for in-place region refreshes

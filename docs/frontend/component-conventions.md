# Component Conventions

Conventions established in Phase 1, Section 2. Follow these when adding new components.

---

## File location

| Component type | Folder |
|---|---|
| Visual primitive (no data fetching concern) | `src/components/ui/` |
| Async/state UI (loading, error, empty, feedback) | `src/components/feedback/` |
| App shell and layout wrappers | `src/components/layout/` |
| Feature-specific components (Phase 2+) | `src/features/{feature}/components/` |

Export all primitives via the folder barrel — never from the component file directly in consumer code.

---

## Naming

- One component per file.
- File name matches the exported function name: `StatCard.tsx` → `export function StatCard`.
- Barrel files are `index.ts`, not `index.tsx`.
- No default exports for components — named exports only.

---

## Props patterns

**Always destructure with defaults at the function signature:**
```ts
export function LoadingState({
  message   = 'Loading…',
  size      = 'md',
  className = '',
}: LoadingStateProps) { … }
```

**`className` on every visual primitive** — allows one-off sizing overrides at the call site without creating a variant prop for every case.

**Logical spacing in JSX** — use `ms-*`, `me-*`, `ps-*`, `pe-*` instead of `ml-*`, `mr-*`, `pl-*`, `pr-*` so the component is RTL-safe by default.

---

## Accessibility checklist for new components

- `role` is set explicitly where Tailwind classes change the visual element semantics (e.g. `<div role="alert">` for errors, `<div role="status">` for loading/empty).
- Interactive elements are native HTML or have `tabIndex`, `onKeyDown`, and `aria-*` as needed.
- Non-decorative text is never hidden only via color (pair color with icon or label).
- Form fields: `useId()` → `id` on `<input>` + `htmlFor` on `<label>`. `aria-describedby` links to helper/error. `aria-invalid` when in error state.
- Decorative SVGs: `aria-hidden="true"`. Meaningful icons: `aria-label` or `<title>` inside the SVG.

---

## Semantic token requirement

Do not use raw Tailwind color classes in new components:

```tsx
// ❌ Wrong
<p className="text-gray-500">...</p>

// ✓ Correct
<p className="text-content-secondary">...</p>
```

Status colors follow the 3-tier pattern:
```tsx
bg-success-light   // tinted background
text-success-dark  // legible text on light bg
border-success     // accent border
```

---

## StateWrapper usage

When a component fetches data, wrap its output in `StateWrapper` rather than writing inline conditionals for loading/error/empty:

```tsx
// Phase 1 placeholder pattern (static content, no real fetching)
<SectionCard title={t('sections.myClasses')}>
  <PlaceholderRow label="Morning Algebra" meta="08:00" accent="brand" />
</SectionCard>

// Phase 2 pattern (real data via TanStack Query)
<SectionCard title={t('sections.myClasses')}>
  <StateWrapper
    isLoading={isLoading}
    isError={isError}
    error={error}
    isEmpty={!classes?.length}
    onRetry={refetch}
    emptyTitle={t('empty.noClasses')}
  >
    <ClassList items={classes!} />
  </StateWrapper>
</SectionCard>
```

---

## i18n conventions

- Every string visible to the user comes from `useTranslation()`.
- Namespace per domain: `dashboards.admin.*`, `dashboards.teacher.*`, `nav.*`, `common.*`.
- Section headings: `dashboards.{role}.sections.{key}` — e.g. `dashboards.teacher.sections.myClasses`.
- Hardcoded English strings in components are only acceptable for development placeholders — flag them with a `// TODO i18n` comment.

---

## When to add a new primitive vs compose existing ones

Add a new primitive when:
- The pattern appears in 3+ different places.
- The accessibility or semantic requirements cannot be met by composing existing primitives.

Compose existing primitives when:
- It's a one-off layout or content variant.
- `SectionCard` + `PlaceholderRow` covers the shape.

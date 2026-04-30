# Styling and Theming

The frontend uses **Tailwind CSS v3** backed by **CSS custom properties** for all design tokens.

---

## Token system overview

All tokens are defined in `:root {}` in `src/index.css`. Tailwind config points to the same CSS vars:

```ts
// tailwind.config.ts
colors: {
  brand: { 500: 'var(--color-brand-500)', … },
  success: { light: 'var(--color-success-light)', DEFAULT: '…', dark: '…' },
  …
}
```

This means:
- **Tenant rebranding**: inject `<style>:root { --color-brand-600: #e11d48; }</style>` — no rebuild.
- **Dark mode**: set `data-theme="dark"` on `<html>` — all surfaces, text, and shadows update via the `[data-theme='dark']` block in `index.css`.
- **RTL font**: `[dir='rtl']` block in `index.css` switches to the RTL-safe font family automatically.

---

## Token categories

### Colors

| Category | Token prefix | Tailwind class prefix |
|---|---|---|
| Brand ramp | `--color-brand-{50,100,200,500,600,700,900}` | `brand-{50…}` |
| Surfaces | `--color-surface[-raised\|-overlay]` | `bg-surface[-raised\|-overlay]` |
| Text | `--color-content-{primary,secondary,muted,inverse}` | `text-content-{…}` |
| Borders | `--color-stroke[-strong]` | `border-stroke[-strong]` |
| Sidebar | `--color-sidebar-{bg,text,hover-bg,active-bg,active-text}` | `bg-sidebar-bg` etc. |
| Status | `--color-{success,warning,error,info}-{light,DEFAULT,dark}` | `bg-success-light text-error-dark` |

### Radius

```
--radius-sm: 4px     rounded-sm
--radius-md: 6px     rounded-md   (default button, input)
--radius-lg: 10px    rounded-lg   (cards)
--radius-xl: 16px    rounded-xl
--radius-full: 9999px rounded-full (badges, avatars)
```

### Shadows

```
--shadow-sm        shadow-sm        (subtle elevation)
--shadow-card      shadow-card      (cards, panels)
--shadow-dropdown  shadow-dropdown  (menus, popovers)
--shadow-modal     shadow-modal     (modals, sheets)
```

### Typography

```
--font-family-sans      Regular LTR sans-serif (inter, system-ui fallback)
--font-family-rtl-sans  RTL-safe alternative (applied automatically via [dir='rtl'])
--font-family-mono      Code/monospace
```

### Layout dimensions

```
--topbar-height: 64px
--sidebar-width: 240px
```

---

## Dark mode

All component code uses semantic token classes (`text-content-primary`, `bg-surface`, etc.). No `dark:` variants in component files are needed. The `[data-theme='dark']` block in `index.css` overrides the relevant CSS vars.

**To toggle dark mode**: `document.documentElement.dataset.theme = 'dark'` (reset with `delete document.documentElement.dataset.theme`). The toggle UI is deferred to Phase 2.

The Tailwind `dark:` variant is registered for rare SVG fill / gradient edge cases.

---

## Status color usage pattern

```tsx
// Badge, InlineFeedback, PlaceholderRow all follow this pattern:
bg-success-light text-success-dark border-success   // success
bg-warning-light text-warning-dark border-warning   // warning
bg-error-light   text-error-dark   border-error     // error
bg-info-light    text-info-dark    border-info       // info
```

---

## RTL/LTR

`src/utils/direction.ts` is the single source of truth:

```ts
export const RTL_LOCALES = new Set<Locale>(['ar', 'fa', 'he', 'ur'])

/** Returns 'ltr' | 'rtl' for any locale string. */
export function getDirection(locale: Locale): Direction

/** Returns true if the locale uses right-to-left layout. */
export function isRtl(locale: Locale): boolean

/** Sets lang + dir attributes on <html>. Called by i18n/index.ts on language change. */
export function applyDocumentDirection(locale: Locale): void
```

`applyDocumentDirection` is called on language change in `src/i18n/index.ts`. Components use `ms-*` / `me-*` / `ps-*` / `pe-*` Tailwind logical properties for directional spacing — **never** `ml-*` / `mr-*` / `pl-*` / `pr-*` in new code.

Adding a new RTL language:
1. Add locale JSON to `src/i18n/locales/`
2. Import it in `src/i18n/index.ts`
3. Add the locale code to `RTL_LOCALES` in `direction.ts`

### `useDirection` hook

```ts
import { useDirection } from '@/hooks/use-direction'
const dir = useDirection() // 'ltr' | 'rtl'
```

Re-evaluates automatically when the i18n language changes. Use only when direction must be read in JavaScript (popover offsets, scroll calculations). For CSS-only changes prefer the `rtl:` Tailwind variant.

### `useTheme` hook

```ts
import { useTheme } from '@/hooks/use-theme'
const { isDark, toggleTheme } = useTheme()
```

Reads the OS colour-scheme preference on first mount, persists to `localStorage`, and writes `data-theme` on `<html>`. The CSS layer in `index.css` handles all visual switching — no component re-renders update colour values.

---

## What not to do

- Do not use raw Tailwind `gray-*`, `slate-*`, or `zinc-*` color classes in components. Use semantic tokens instead.
- Do not use `ml-` / `mr-` / `pl-` / `pr-` for component spacing. Use logical equivalents.
- Do not call `document.documentElement.setAttribute('dir', …)` manually. `applyDocumentDirection` handles this via the i18n layer.

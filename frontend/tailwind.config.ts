import type { Config } from 'tailwindcss'

const config: Config = {
  content: ['./index.html', './src/**/*.{ts,tsx}'],

  // ── Dark mode ────────────────────────────────────────────────────────────
  // Activated by   data-theme="dark" on <html>.
  // Because all colour classes use CSS vars, the visual switch is handled
  // entirely by the [data-theme='dark'] block in index.css without any `dark:`
  // variants in components. The `dark:` variant is registered here for the
  // rare cases where CSS vars cannot do the job (e.g. SVG fill, gradient stops).
  darkMode: ['class', '[data-theme="dark"]'],

  theme: {
    extend: {
      // ── Color tokens ──────────────────────────────────────────────────────
      // Every color points to the matching CSS custom property.
      // This means a tenant can override any visual without a rebuild —
      // they inject one <style> block that sets the CSS vars they need.
      colors: {
        // Brand ramp
        brand: {
          50:  'var(--color-brand-50)',
          100: 'var(--color-brand-100)',
          200: 'var(--color-brand-200)',
          500: 'var(--color-brand-500)',
          600: 'var(--color-brand-600)',
          700: 'var(--color-brand-700)',
          900: 'var(--color-brand-900)',
        },

        // Semantic surface tiers
        // bg-surface | bg-surface-raised | bg-surface-overlay
        surface: {
          DEFAULT: 'var(--color-surface)',
          raised:  'var(--color-surface-raised)',
          overlay: 'var(--color-surface-overlay)',
        },

        // Semantic text tiers
        // text-content-primary | text-content-secondary | text-content-muted
        content: {
          primary:   'var(--color-content-primary)',
          secondary: 'var(--color-content-secondary)',
          muted:     'var(--color-content-muted)',
          inverse:   'var(--color-content-inverse)',
        },

        // Semantic border tiers
        // border-stroke | border-stroke-strong
        stroke: {
          DEFAULT: 'var(--color-stroke)',
          strong:  'var(--color-stroke-strong)',
        },

        // Sidebar tokens — all overridable for light-sidebar tenant themes
        sidebar: {
          bg:           'var(--color-sidebar-bg)',
          text:         'var(--color-sidebar-text)',
          'hover-bg':   'var(--color-sidebar-hover-bg)',
          'active-bg':  'var(--color-sidebar-active-bg)',
          'active-text':'var(--color-sidebar-active-text)',
        },

        // Status tokens — three tiers each (light tint / base / dark text)
        // Usage pattern: bg-success-light text-success-dark border-success
        success: {
          light:   'var(--color-success-light)',
          DEFAULT: 'var(--color-success)',
          dark:    'var(--color-success-dark)',
        },
        warning: {
          light:   'var(--color-warning-light)',
          DEFAULT: 'var(--color-warning)',
          dark:    'var(--color-warning-dark)',
        },
        error: {
          light:   'var(--color-error-light)',
          DEFAULT: 'var(--color-error)',
          dark:    'var(--color-error-dark)',
        },
        info: {
          light:   'var(--color-info-light)',
          DEFAULT: 'var(--color-info)',
          dark:    'var(--color-info-dark)',
        },
      },

      // ── Font families ─────────────────────────────────────────────────────
      // font-sans and font-rtl both delegate to CSS vars so tenant brand
      // fonts (and RTL-specific fonts) can be injected without a rebuild.
      // The base font-family is set globally on html/[dir='rtl'] in index.css,
      // so these classes are needed only for explicit per-component overrides.
      fontFamily: {
        sans: ['var(--font-family-sans)'],
        rtl:  ['var(--font-family-rtl-sans)'],
        mono: ['var(--font-family-mono)'],
      },

      // ── Border radius tokens ──────────────────────────────────────────────
      // Overriding Tailwind's default keys (sm/md/lg/xl/full) so rounded-*
      // utilities reference the same CSS vars as other token consumers.
      // A tenant can change the global corner radius with:
      //   :root { --radius-md: 0px }  ← squared UI
      //   :root { --radius-md: 12px } ← pill-heavy UI
      borderRadius: {
        sm:   'var(--radius-sm)',
        md:   'var(--radius-md)',
        lg:   'var(--radius-lg)',
        xl:   'var(--radius-xl)',
        full: 'var(--radius-full)',
      },

      // ── Shadow tokens ─────────────────────────────────────────────────────
      // Named by use-case. shadow-card, shadow-dropdown, shadow-modal are new.
      // shadow-sm overrides Tailwind's default (same value as Tailwind default).
      // Dark mode overrides these vars in index.css so components need no changes.
      boxShadow: {
        sm:       'var(--shadow-sm)',
        card:     'var(--shadow-card)',
        dropdown: 'var(--shadow-dropdown)',
        modal:    'var(--shadow-modal)',
      },

      // ── Layout dimension utilities ─────────────────────────────────────────
      // h-topbar, w-sidebar reference the same CSS vars as the AppShell CSS
      height: {
        topbar: 'var(--topbar-height)',
      },
      width: {
        sidebar: 'var(--sidebar-width)',
      },
    },
  },
  plugins: [],
}

export default config

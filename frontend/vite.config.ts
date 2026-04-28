import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      // '@' maps to src/ — use in imports: import Foo from '@/components/Foo'
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5173,
    // Proxy API calls to backend during local development
    proxy: {
      '/api': {
        target: process.env.VITE_API_BASE_URL ?? 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/tests/setup.ts'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'lcov', 'html'],
      // Only measure coverage on source files, not test helpers or type stubs
      include: ['src/**/*.{ts,tsx}'],
      exclude: [
        'src/**/*.d.ts',
        'src/tests/**',
        'src/main.tsx',         // entry-point — nothing to unit-test here
        'src/**/*index.ts',     // re-export barrels
      ],
    },
  },
})

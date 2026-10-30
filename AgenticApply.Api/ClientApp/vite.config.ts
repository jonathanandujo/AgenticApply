import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  build: {
    outDir: '../wwwroot',
    emptyOutDir: true,
  },
  server: {
    proxy: {
      '/jobs': 'http://localhost:5000',
      '/stats': 'http://localhost:5000',
      '/settings': 'http://localhost:5000',
      '/agent': 'http://localhost:5000',
    },
  },
})

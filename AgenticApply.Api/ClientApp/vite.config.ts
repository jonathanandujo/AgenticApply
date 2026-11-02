import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { defineConfig, type ProxyOptions } from 'vite'

// https://vite.dev/config/
const apiProxy: ProxyOptions = {
  target: 'http://localhost:5000',
  bypass(request) {
    if (request.headers.accept?.includes('text/html')) {
      return '/index.html'
    }
  },
}

export default defineConfig({
  plugins: [react(), tailwindcss()],
  build: {
    outDir: '../wwwroot',
    emptyOutDir: true,
  },
  server: {
    host: 'localhost',
    port: 5173,
    strictPort: true,
    proxy: {
      '/jobs': apiProxy,
      '/stats': apiProxy,
      '/settings': apiProxy,
      '/agent': apiProxy,
    },
  },
})

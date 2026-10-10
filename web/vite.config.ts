import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Geliştirme sırasında tarayıcı yalnızca Vite'a (5173) konuşur; /api istekleri yerel API'ye iletilir.
    // Böylece geliştirmede CORS ayarına gerek kalmaz. Canlıda VITE_API_URL kullanılır (bkz. src/api.ts).
    proxy: { '/api': 'http://127.0.0.1:5099' },
  },
  test: { environment: 'node' },
})

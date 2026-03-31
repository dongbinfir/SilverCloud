import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@user-api': path.resolve(__dirname, 'src/api/user_api.ts'),
    },
  },
  server: {
    port: 7041,
  },
})

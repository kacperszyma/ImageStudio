import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'
// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  // Load env files (VITE_*) from the repo root, shared with the backend.
  envDir: path.resolve(__dirname, ".."),
  resolve: {
    alias: { "@": path.resolve(__dirname, "./src") },
  },
})

/// <reference types="vitest" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

const api = 'http://localhost:5101';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5181,
    // The API also serves /media (the local stand-in for the CDN origin).
    proxy: { '/api': api, '/media': api, '/health': api },
  },
  test: {
    environment: 'jsdom',
    globals: false,
    css: { include: [/tokens\.css/] },
  },
});

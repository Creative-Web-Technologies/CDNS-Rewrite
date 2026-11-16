import react from '@vitejs/plugin-react'
import svgr from 'vite-plugin-svgr'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [
    svgr({
      // Process files imported as "*.svg?react"
      include: '**/*.svg?react',
      svgrOptions: {
        svgo: true,
        svgoConfig: {
          plugins: [
            {
              name: 'preset-default',
              params: {
                overrides: {
                  // Keep viewBox so nested SVGs size correctly
                  removeViewBox: false,
                  // Preserve IDs (ship-smoke, crane-hoist, etc.) for CSS animation
                  cleanupIds: false,
                },
              },
            },
          ],
        },
      },
    }),
    react(),
  ],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5030',
        changeOrigin: false,
      },
    },
  },
})

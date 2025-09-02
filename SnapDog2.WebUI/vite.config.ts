import path from 'path';
import { defineConfig, loadEnv } from 'vite';

export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, '.', '');
    return {
      base: '/webui/', // Set base path for proxy
      define: {
        'process.env.API_KEY': JSON.stringify(env.GEMINI_API_KEY),
        'process.env.GEMINI_API_KEY': JSON.stringify(env.GEMINI_API_KEY)
      },
      resolve: {
        alias: {
          '@': path.resolve(__dirname, 'src'),
        }
      },
      server: {
        host: '0.0.0.0',
        port: 5173,
        proxy: {
          '/api': {
            target: 'http://app:5555',
            changeOrigin: true,
          },
          '/hubs': {
            target: 'http://app:5555',
            changeOrigin: true,
            ws: true, // Enable WebSocket proxying for SignalR
          },
        },
      },
    };
});

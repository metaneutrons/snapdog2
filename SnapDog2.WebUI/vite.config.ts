import path from 'path';
import { defineConfig } from 'vite';

export default defineConfig({
    base: '/webui/',
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
                ws: true,
            },
        },
    },
});

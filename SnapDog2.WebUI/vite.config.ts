import path from 'path';
import { defineConfig } from 'vite';

export default defineConfig(({ mode }) => ({
    // Development: served at /webui/ via Caddy proxy
    // Production: embedded in app, served at /
    base: mode === 'development' ? '/webui/' : '/',
    resolve: {
        alias: {
            '@': path.resolve(__dirname, 'src'),
        }
    },
    server: {
        host: '0.0.0.0',
        port: 5173,
    },
}));

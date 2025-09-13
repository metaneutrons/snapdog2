# SnapDog2 WebUI

## Configuration

### Base URL Configuration

The WebUI base URL is currently configured via environment variable:
- `VITE_BASE_URL` - Set in docker-compose.yml (default: `/webui/`)

**Future Enhancement**: This should be dynamically configured from `HttpConfig.BaseUrl` in the backend configuration.

### Development

```bash
npm install
npm run dev
```

### Production Build

```bash
npm run build
```

## API Integration

The WebUI connects to the SnapDog2 API via proxy configuration in `vite.config.ts`.

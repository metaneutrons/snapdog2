# SnapDog2 WebUI Architecture

## Development vs Production

### Development (docker-compose.yml + Caddy)

- **WebUI**: Separate container with Vite dev server
- **Routing**: Caddy reverse proxy handles path-based routing
- **Base URL**: `/webui/` (Vite serves with this base)

```
localhost:8000/api   → app container (API)
localhost:8000/hubs  → app container (SignalR)
localhost:8000/webui → frontend container (Vite dev server)
```

### Production (embedded in app)

- **WebUI**: Built and embedded in SnapDog2 app container
- **Routing**: App serves WebUI directly from root
- **Base URL**: `/` (WebUI serves from root)

```
http://snapdog.schmieder.eu/     → app container (WebUI)
http://snapdog.schmieder.eu/api  → app container (API)
http://snapdog.schmieder.eu/hubs → app container (SignalR)
```

## Configuration

### Vite Config

- **Development**: `base: '/webui/'` (matches Caddy routing)
- **Production**: `base: '/'` (embedded in app root)

### HttpConfig.BaseUrl

- Controls the external base URL for the entire application
- Used for generating absolute URLs in production
- Example: `http://snapdog.schmieder.eu`

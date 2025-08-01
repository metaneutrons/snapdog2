# SnapDog2 Development Makefile
# Container-first development workflow

.PHONY: help dev-setup dev-start dev-stop dev-status dev-logs clean test build \
        monitoring-start monitoring-stop

# Default target
help:
	@echo "SnapDog2 Development Commands:"
	@echo ""
	@echo "🏗️  Development:"
	@echo "  dev-setup       - Initial setup (pull images, restore packages)"
	@echo "  dev-start       - Start development container"
	@echo "  dev-stop        - Stop development containers"
	@echo "  dev-status      - Show status of all container"
	@echo "  dev-logs        - Show logs from all container"
	@echo ""

	@echo "📊 Monitoring:"
	@echo "  monitoring-start - Start Prometheus + Grafana"
	@echo "  monitoring-stop  - Stop monitoring services"
	@echo ""
	@echo "🧪 Testing & Building:"
	@echo "  test            - Run tests with services"
	@echo "  build           - Build the application"
	@echo "  clean           - Clean containers and volumes"
	@echo ""
	@echo "🌐 Info:"
	@echo "  urls            - Show all service URLs"

# Development environment setup
dev-setup:
	@echo "🚀 Setting up SnapDog2 development environment..."
	@mkdir -p audio music config/grafana/dashboards config/grafana/datasources
	@echo "📦 Pulling Docker images..."
	@docker compose -f docker-compose.dev.yml --env-file devcontainer/.env pull
	@echo "📦 Restoring .NET packages..."
	@dotnet restore
	@dotnet tool restore
	@echo "✅ Development environment ready!"
	@echo ""
	@echo "Next step:"
	@echo "  make dev-start  # Start full development environment"

# Start full development environment
dev-start:
	@echo "🐳 Starting SnapDog2 development environment..."
	@docker compose -f docker-compose.dev.yml --env-file devcontainer/.env up -d
	@echo "⏳ Waiting for services to be ready..."
	@sleep 5
	@make dev-status
	@echo ""
	@echo "✅ SnapDog2 development environment running!"
	@echo "🔗 Access services: http://localhost:8000"
	@echo "🔍 Attach debugger to container process"
	@echo "📝 Edit code locally - hot reload active"
	@echo ""
	@echo "To view logs: make dev-logs"
	@echo "To stop: make dev-stop"


# Start monitoring stack
monitoring-start:
	@echo "📊 Starting monitoring stack..."
	@docker compose -f docker-compose.dev.yml --env-file devcontainer/.env --profile monitoring up -d
	@echo "✅ Monitoring services started!"
	@echo "Grafana: http://localhost:8000/grafana/ (admin/snapdog-dev)"
	@echo "Prometheus: http://localhost:8000/prometheus/"

# Stop monitoring stack
monitoring-stop:
	@echo "📊 Stopping monitoring stack..."
	@docker compose -f docker-compose.dev.yml --env-file devcontainer/.env --profile monitoring down

# Stop development services
dev-stop:
	@echo "🛑 Stopping SnapDog2 development services..."
	@docker compose -f docker-compose.dev.yml --env-file devcontainer/.env down
	@echo "✅ Services stopped"

# Show service status
dev-status:
	@echo "📊 SnapDog2 Development Services Status:"
	@docker compose -f docker-compose.dev.yml --env-file devcontainer/.env ps

# Show service logs
dev-logs:
	@docker compose -f docker-compose.dev.yml --env-file devcontainer/.env logs -f

# Clean up everything
clean:
	@echo "🧹 Cleaning up development environment..."
	@docker compose -f docker-compose.dev.yml --env-file devcontainer/.env down -v --remove-orphans
	@docker system prune -f
	@echo "✅ Environment cleaned"

# Run tests with services
test:
	@echo "🧪 Running tests with development services..."
	@docker compose -f docker-compose.dev.yml --env-file devcontainer/.env up -d
	@sleep 3
	@dotnet test --verbosity normal
	@echo "✅ Tests completed"

# Build the application
build:
	@echo "🔨 Building SnapDog2..."
	@dotnet build --configuration Release
	@echo "✅ Build completed"


# Show all service URLs
urls:
	@echo "🌐 SnapDog2 Development URLs (Single Port Access):"
	@echo ""
	@echo "🏠 Main Dashboard:"
	@echo "  SnapDog Dashboard: http://localhost:8000"
	@echo ""
	@echo "📱 Application:"
	@echo "  SnapDog2 API:      Access via container logs or Jaeger tracing"
	@echo ""
	@echo "🎵 Audio Services (via Caddy):"
	@echo "  Snapcast Server:   http://localhost:8000/server/"
	@echo "  Navidrome Music:   http://localhost:8000/music/"
	@echo "  Living Room:       http://localhost:8000/clients/living-room/"
	@echo "  Kitchen:           http://localhost:8000/clients/kitchen/"
	@echo "  Bedroom:           http://localhost:8000/clients/bedroom/"
	@echo ""
	@echo "📊 Monitoring (via Caddy):"
	@echo "  Jaeger Tracing:    http://localhost:8000/tracing/"
	@echo "  Grafana:           http://localhost:8000/grafana/ (admin/snapdog-dev)"
	@echo "  Prometheus:        http://localhost:8000/prometheus/"
	@echo ""
	@echo "🔧 Internal Services (container-only):"
	@echo "  MQTT Broker:       mqtt:1883 (internal)"
	@echo "  KNX Gateway:       knx-simulator:6720 (internal)"
	@echo "  Snapcast JSON-RPC: snapcast-server:1704 (internal)"
	@echo ""
	@echo "🏠 Test Client IPs:"
	@echo "  Living Room:       172.20.0.6 (02:42:ac:11:00:10)"
	@echo "  Kitchen:           172.20.0.7 (02:42:ac:11:00:11)"
	@echo "  Bedroom:           172.20.0.8 (02:42:ac:11:00:12)"

# Create config files
config:
	@echo "📝 Creating configuration files..."
	@mkdir -p config config/grafana/dashboards config/grafana/datasources
	@echo "# Snapserver configuration" > config/snapserver.conf
	@echo "stream = pipe:///tmp/snapfifo?name=default" >> config/snapserver.conf
	@echo "# MQTT configuration" > config/mosquitto.conf
	@echo "listener 1883" >> config/mosquitto.conf
	@echo "allow_anonymous true" >> config/mosquitto.conf
	@echo "# Prometheus configuration" > config/prometheus.yml
	@echo "global:" >> config/prometheus.yml
	@echo "  scrape_interval: 15s" >> config/prometheus.yml
	@echo "scrape_configs:" >> config/prometheus.yml
	@echo "  - job_name: 'snapdog'" >> config/prometheus.yml
	@echo "    static_configs:" >> config/prometheus.yml
	@echo "      - targets: ['host.docker.internal:5000']" >> config/prometheus.yml
	@echo "✅ Configuration files created"

# Development workflow
dev: dev-setup dev-start urls

# Quick restart
restart: dev-stop dev-start

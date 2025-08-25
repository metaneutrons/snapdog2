#!/bin/bash

# SnapDog2 Development Environment Script
# Manages Docker Compose services for development

show_urls() {
    echo ""
    echo "🎵 SnapDog2 Services (http://localhost:8000):"
    echo "  🎵 SnapDog2 API:      http://localhost:8000/api"
    echo "  🎵 SnapDog2 Swagger:  http://localhost:8000/swagger"
    echo "  💿 Navidrome Music:   http://localhost:8000/music/"
    echo "  📻 Snapcast Server:   http://localhost:8000/server/"
    echo "  🛋️ Living Room:       http://localhost:8000/clients/living-room/"
    echo "  🍽️ Kitchen:           http://localhost:8000/clients/kitchen/"
    echo "  🛏️ Bedroom:           http://localhost:8000/clients/bedroom/"
    echo ""
    echo "🔍 Observability:"
    echo "  📊 Grafana:           http://localhost:8000/grafana/"
    echo ""
}

start_dev() {
    echo "🚀 Starting SnapDog2 development environment with Grafana observability..."
    docker compose -f docker-compose.dev.yml up -d
    sleep 3
    show_urls
}

stop_dev() {
    echo "🛑 Stopping development environment..."
    docker compose -f docker-compose.dev.yml down
}

restart_dev() {
    stop_dev
    start_dev
}

show_logs() {
    docker compose -f docker-compose.dev.yml logs -f
}

show_status() {
    echo "📊 Service Status:"
    docker compose -f docker-compose.dev.yml ps
}

clean_env() {
    echo "🧹 Cleaning up..."
    docker compose -f docker-compose.dev.yml down -v --remove-orphans
    docker system prune -f
}

run_tests() {
    echo "🧪 Running tests..."
    docker compose -f docker-compose.dev.yml up -d
    dotnet test --verbosity normal
    docker compose -f docker-compose.dev.yml down
}

show_help() {
    echo "🎵 SnapDog2 Development Environment"
    echo ""
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  start      Start development environment"
    echo "  stop       Stop development environment"
    echo "  restart    Restart development environment"
    echo "  status     Show service status"
    echo "  logs       Show logs"
    echo "  urls       Show service URLs"
    echo "  clean      Clean up containers and volumes"
    echo "  test       Run tests with services"
    echo "  help       Show this help"
    echo ""
}

# Main script logic
case "${1:-start}" in
    "start")
        start_dev
        ;;
    "stop")
        stop_dev
        ;;
    "restart")
        restart_dev
        ;;
    "status")
        show_status
        ;;
    "logs")
        show_logs
        ;;
    "urls")
        show_urls
        ;;
    "clean")
        clean_env
        ;;
    "test")
        run_tests
        ;;
    "help"|"-h"|"--help")
        show_help
        ;;
    *)
        echo "❌ Unknown command: $1"
        show_help
        exit 1
        ;;
esac

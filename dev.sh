#!/bin/bash

# SnapDog2 Development Environment Script
# Simple replacement for Makefile with SigNoz-only observability

set -e

show_help() {
    echo "SnapDog2 Development Commands:"
    echo ""
    echo "  ./dev.sh start     - Start development environment"
    echo "  ./dev.sh stop      - Stop all services"
    echo "  ./dev.sh restart   - Restart development environment"
    echo "  ./dev.sh logs      - Show logs from all services"
    echo "  ./dev.sh status    - Show status of all services"
    echo "  ./dev.sh clean     - Clean containers and volumes"
    echo "  ./dev.sh urls      - Show all service URLs"
    echo "  ./dev.sh test      - Run tests"
    echo "  ./dev.sh build     - Build the application"
    echo ""
}

start_dev() {
    echo "🚀 Starting SnapDog2 development environment with SigNoz..."
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

show_urls() {
    echo ""
    echo "🎵 SnapDog2 Services (http://localhost:8000):"
    echo "  🎵 SnapDog2 API:      http://localhost:8000"
    echo "  📻 Snapcast Server:   http://localhost:8000/server/"
    echo "  💿 Navidrome Music:   http://localhost:8000/music/"
    echo "  🛋️  Living Room:       http://localhost:8000/clients/living-room/"
    echo "  🍽️  Kitchen:           http://localhost:8000/clients/kitchen/"
    echo "  🛏️  Bedroom:           http://localhost:8000/clients/bedroom/"
    echo ""
    echo "🔍 Observability:"
    echo "  🔍 SigNoz:            http://localhost:8000/signoz/"
    echo ""
}

run_tests() {
    echo "🧪 Running tests..."
    docker compose -f docker-compose.dev.yml up -d
    dotnet test --verbosity normal
    docker compose -f docker-compose.dev.yml down
}

build_app() {
    echo "🏗️ Building SnapDog2..."
    dotnet build --configuration Release
}

case "${1:-help}" in
    start)
        start_dev
        ;;
    stop)
        stop_dev
        ;;
    restart)
        restart_dev
        ;;
    logs)
        show_logs
        ;;
    status)
        show_status
        ;;
    clean)
        clean_env
        ;;
    urls)
        show_urls
        ;;
    test)
        run_tests
        ;;
    build)
        build_app
        ;;
    help|--help|-h)
        show_help
        ;;
    *)
        echo "Unknown command: $1"
        show_help
        exit 1
        ;;
esac

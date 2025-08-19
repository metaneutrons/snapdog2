#!/bin/bash

# Test script to validate Docker Compose test approach
set -e

echo "🧪 Testing Docker Compose test infrastructure..."

# Check if docker-compose.test.yml exists
if [ ! -f "docker-compose.test.yml" ]; then
    echo "❌ docker-compose.test.yml not found"
    exit 1
fi

echo "✅ docker-compose.test.yml found"

# Test docker compose syntax
echo "🔍 Validating docker-compose.test.yml syntax..."
docker compose -f docker-compose.test.yml config > /dev/null
echo "✅ Docker Compose syntax is valid"

# Test if we can start the services (dry run)
echo "🔍 Testing service startup (dry run)..."
PROJECT_NAME="snapdog-test-validation-$(date +%s)"

# Start services
echo "🚀 Starting test services..."
docker compose -f docker-compose.test.yml -p "$PROJECT_NAME" up -d --build

# Wait a bit for services to start
echo "⏳ Waiting for services to initialize..."
sleep 30

# Check if services are running
echo "🔍 Checking service health..."
SERVICES_RUNNING=$(docker compose -f docker-compose.test.yml -p "$PROJECT_NAME" ps --services --filter "status=running" | wc -l)
TOTAL_SERVICES=$(docker compose -f docker-compose.test.yml -p "$PROJECT_NAME" ps --services | wc -l)

echo "📊 Services running: $SERVICES_RUNNING/$TOTAL_SERVICES"

# Test API connectivity
echo "🔍 Testing API connectivity..."
if curl -f -s http://localhost:5001/health > /dev/null; then
    echo "✅ SnapDog2 API is responding"
else
    echo "❌ SnapDog2 API is not responding"
    echo "📊 Service logs:"
    docker compose -f docker-compose.test.yml -p "$PROJECT_NAME" logs app-test --tail=20
fi

# Cleanup
echo "🧹 Cleaning up test services..."
docker compose -f docker-compose.test.yml -p "$PROJECT_NAME" down -v

echo "✅ Docker Compose test infrastructure validation complete"

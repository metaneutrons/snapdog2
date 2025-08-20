#!/bin/bash

# ═══════════════════════════════════════════════════════════════════════════════
# SnapDog2 Container Integration Test Runner
# ═══════════════════════════════════════════════════════════════════════════════
# Runs integration tests inside Docker containers with real Snapcast server
# ═══════════════════════════════════════════════════════════════════════════════

set -e

echo "🚀 Starting SnapDog2 Container Integration Tests..."

# Clean up any existing containers
echo "🧹 Cleaning up existing containers..."
docker compose -f SnapDog2.Tests/TestData/Docker/docker-compose.minimal.yml down -v 2>/dev/null || true

# Create TestResults directory
mkdir -p TestResults

# Start the test environment
echo "🐳 Starting test environment..."
docker compose -f SnapDog2.Tests/TestData/Docker/docker-compose.minimal.yml up --build --abort-on-container-exit

# Check test results
echo "📊 Test Results:"
if [ -f "TestResults/TestResults.trx" ]; then
    echo "✅ Test results saved to TestResults/TestResults.trx"
else
    echo "⚠️ No test results file found"
fi

# Clean up
echo "🧹 Cleaning up..."
docker compose -f SnapDog2.Tests/TestData/Docker/docker-compose.minimal.yml down -v

echo "✅ Container integration tests completed!"

#!/bin/bash

# Enterprise-Grade Test Execution Script for SnapDog2
# Provides comprehensive test execution with filtering, reporting, and CI/CD integration

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default values
TEST_CATEGORY=""
TEST_TYPE=""
PARALLEL="true"
COVERAGE="true"
OUTPUT_DIR="TestResults"
VERBOSE="false"
CI_MODE="false"

# Function to print colored output
print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_header() {
    echo -e "${BLUE}"
    echo "=================================="
    echo "  $1"
    echo "=================================="
    echo -e "${NC}"
}

# Function to show usage
show_usage() {
    cat << EOF
Enterprise Test Execution Script for SnapDog2

Usage: $0 [OPTIONS]

OPTIONS:
    -c, --category CATEGORY    Run tests by category (Unit, Integration, Container, Performance, Workflow)
    -t, --type TYPE           Run tests by type (Service, Controller, Configuration, Infrastructure)
    -s, --speed SPEED         Run tests by speed (Fast, Medium, Slow, VerySlow)
    -p, --parallel            Enable/disable parallel execution (default: true)
    --no-parallel             Disable parallel execution
    --coverage                Enable/disable code coverage (default: true)
    --no-coverage             Disable code coverage
    -o, --output DIR          Output directory for test results (default: TestResults)
    -v, --verbose             Enable verbose output
    --ci                      Run in CI mode (optimized for CI/CD)
    -h, --help                Show this help message

EXAMPLES:
    $0                                    # Run all tests
    $0 -c Unit                           # Run only unit tests
    $0 -c Integration --no-parallel      # Run integration tests sequentially
    $0 -c Performance -v                 # Run performance tests with verbose output
    $0 -t Service -s Fast                # Run fast service tests
    $0 --ci                              # Run in CI mode

TEST CATEGORIES:
    Unit         - Pure unit tests (no external dependencies)
    Integration  - Integration tests with real services
    Container    - Testcontainer-based tests
    Performance  - Performance and load tests
    Workflow     - End-to-end workflow tests
    Smoke        - Basic functionality verification
    Regression   - Tests for known issues

TEST TYPES:
    Service      - Service layer tests
    Controller   - API controller tests
    Configuration- Configuration tests
    Infrastructure- Infrastructure tests
    Domain       - Domain model tests
    Api          - API integration tests

TEST SPEEDS:
    Fast         - < 100ms execution time
    Medium       - 100ms - 1s execution time
    Slow         - > 1s execution time
    VerySlow     - > 10s execution time
EOF
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--category)
            TEST_CATEGORY="$2"
            shift 2
            ;;
        -t|--type)
            TEST_TYPE="$2"
            shift 2
            ;;
        -s|--speed)
            TEST_SPEED="$2"
            shift 2
            ;;
        -p|--parallel)
            PARALLEL="true"
            shift
            ;;
        --no-parallel)
            PARALLEL="false"
            shift
            ;;
        --coverage)
            COVERAGE="true"
            shift
            ;;
        --no-coverage)
            COVERAGE="false"
            shift
            ;;
        -o|--output)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        -v|--verbose)
            VERBOSE="true"
            shift
            ;;
        --ci)
            CI_MODE="true"
            PARALLEL="true"
            COVERAGE="true"
            shift
            ;;
        -h|--help)
            show_usage
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            show_usage
            exit 1
            ;;
    esac
done

# Validate Docker is available for container tests
check_docker() {
    if ! command -v docker &> /dev/null; then
        print_warning "Docker not found. Container tests will be skipped."
        return 1
    fi
    
    if ! docker info &> /dev/null; then
        print_warning "Docker daemon not running. Container tests will be skipped."
        return 1
    fi
    
    return 0
}

# Build test filter
build_filter() {
    local filter=""
    
    if [[ -n "$TEST_CATEGORY" ]]; then
        filter="Category=$TEST_CATEGORY"
    fi
    
    if [[ -n "$TEST_TYPE" ]]; then
        if [[ -n "$filter" ]]; then
            filter="$filter&Type=$TEST_TYPE"
        else
            filter="Type=$TEST_TYPE"
        fi
    fi
    
    if [[ -n "$TEST_SPEED" ]]; then
        if [[ -n "$filter" ]]; then
            filter="$filter&Speed=$TEST_SPEED"
        else
            filter="Speed=$TEST_SPEED"
        fi
    fi
    
    echo "$filter"
}

# Main execution
main() {
    print_header "SnapDog2 Enterprise Test Suite"
    
    # Check prerequisites
    if [[ "$TEST_CATEGORY" == "Container" ]] || [[ -z "$TEST_CATEGORY" ]]; then
        if ! check_docker; then
            if [[ "$TEST_CATEGORY" == "Container" ]]; then
                print_error "Docker is required for container tests but not available"
                exit 1
            fi
        fi
    fi
    
    # Create output directory
    mkdir -p "$OUTPUT_DIR"
    
    # Build test command
    local cmd="dotnet test"
    local filter=$(build_filter)
    
    if [[ -n "$filter" ]]; then
        cmd="$cmd --filter \"$filter\""
        print_info "Test Filter: $filter"
    fi
    
    # Configure parallel execution
    if [[ "$PARALLEL" == "false" ]]; then
        cmd="$cmd --parallel"
        print_info "Parallel execution: Disabled"
    else
        print_info "Parallel execution: Enabled"
    fi
    
    # Configure verbosity
    if [[ "$VERBOSE" == "true" ]]; then
        cmd="$cmd --verbosity detailed"
    else
        cmd="$cmd --verbosity normal"
    fi
    
    # Configure output
    cmd="$cmd --logger \"trx;LogFileName=$OUTPUT_DIR/TestResults.trx\""
    cmd="$cmd --logger \"console;verbosity=normal\""
    
    # Configure code coverage
    if [[ "$COVERAGE" == "true" ]]; then
        cmd="$cmd --collect:\"XPlat Code Coverage\""
        cmd="$cmd --results-directory \"$OUTPUT_DIR\""
        print_info "Code Coverage: Enabled"
    else
        print_info "Code Coverage: Disabled"
    fi
    
    # CI-specific configuration
    if [[ "$CI_MODE" == "true" ]]; then
        cmd="$cmd --logger \"GitHubActions;summary.includePassedTests=true;summary.includeSkippedTests=true\""
        print_info "CI Mode: Enabled"
    fi
    
    print_info "Output Directory: $OUTPUT_DIR"
    print_info "Executing: $cmd"
    echo
    
    # Execute tests
    local start_time=$(date +%s)
    
    if eval "$cmd"; then
        local end_time=$(date +%s)
        local duration=$((end_time - start_time))
        
        print_success "Tests completed successfully in ${duration}s"
        
        # Generate coverage report if enabled
        if [[ "$COVERAGE" == "true" ]]; then
            generate_coverage_report
        fi
        
        # Show test summary
        show_test_summary
        
    else
        local end_time=$(date +%s)
        local duration=$((end_time - start_time))
        
        print_error "Tests failed after ${duration}s"
        exit 1
    fi
}

# Generate coverage report
generate_coverage_report() {
    print_info "Generating coverage report..."
    
    if command -v reportgenerator &> /dev/null; then
        reportgenerator \
            -reports:"$OUTPUT_DIR/**/coverage.cobertura.xml" \
            -targetdir:"$OUTPUT_DIR/CoverageReport" \
            -reporttypes:"Html;Badges" \
            -title:"SnapDog2 Test Coverage"
        
        print_success "Coverage report generated: $OUTPUT_DIR/CoverageReport/index.html"
    else
        print_warning "ReportGenerator not found. Install with: dotnet tool install -g dotnet-reportgenerator-globaltool"
    fi
}

# Show test summary
show_test_summary() {
    print_header "Test Execution Summary"
    
    if [[ -f "$OUTPUT_DIR/TestResults.trx" ]]; then
        # Parse TRX file for summary (simplified)
        print_info "Test results saved to: $OUTPUT_DIR/TestResults.trx"
    fi
    
    if [[ -d "$OUTPUT_DIR/CoverageReport" ]]; then
        print_info "Coverage report: $OUTPUT_DIR/CoverageReport/index.html"
    fi
    
    print_success "All test artifacts saved to: $OUTPUT_DIR"
}

# Execute main function
main "$@"

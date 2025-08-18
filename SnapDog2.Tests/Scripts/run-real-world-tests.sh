#!/bin/bash

# Enterprise-Grade Real-World Scenario Test Runner
# Executes comprehensive real-world testing scenarios for SnapDog2 Zone Grouping

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
TEST_PROJECT="$PROJECT_ROOT/SnapDog2.Tests"
RESULTS_DIR="$PROJECT_ROOT/TestResults"
COVERAGE_DIR="$RESULTS_DIR/Coverage"

# Test categories
REAL_WORLD_TESTS="Integration.ZoneGroupingRealWorldTests"
FAULT_INJECTION_TESTS="Integration.FaultInjectionTests"
PERFORMANCE_TESTS="Performance.ZoneGroupingPerformanceTests"

# Default values
VERBOSE=false
COVERAGE=false
CI_MODE=false
PARALLEL=true
CATEGORY=""
OUTPUT_FORMAT="detailed"
TIMEOUT="30m"

# Function to print colored output
print_status() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

print_header() {
    echo
    print_status $CYAN "=============================================="
    print_status $CYAN "$1"
    print_status $CYAN "=============================================="
    echo
}

print_success() {
    print_status $GREEN "✅ $1"
}

print_error() {
    print_status $RED "❌ $1"
}

print_warning() {
    print_status $YELLOW "⚠️  $1"
}

print_info() {
    print_status $BLUE "ℹ️  $1"
}

# Function to show usage
show_usage() {
    cat << EOF
🧪 Enterprise-Grade Real-World Scenario Test Runner

Usage: $0 [OPTIONS]

OPTIONS:
    -c, --category CATEGORY    Run specific test category:
                              - realworld: Real-world integration scenarios
                              - fault: Fault injection tests
                              - performance: Performance and load tests
                              - all: All real-world tests (default)
    
    -v, --verbose             Enable verbose output
    --coverage               Generate code coverage reports
    --ci                     Run in CI mode (optimized for automation)
    --no-parallel            Disable parallel test execution
    --timeout DURATION       Test timeout (default: 30m)
    --format FORMAT          Output format: detailed|summary|junit
    -h, --help               Show this help message

EXAMPLES:
    $0                                    # Run all real-world tests
    $0 -c realworld -v                   # Run real-world scenarios with verbose output
    $0 -c fault --coverage               # Run fault injection tests with coverage
    $0 -c performance --ci               # Run performance tests in CI mode
    $0 --format junit --ci               # Generate JUnit XML for CI integration

CATEGORIES:
    realworld     - Complete end-to-end workflow testing
                   - API integration validation
                   - Cross-system consistency checks
                   - Recovery scenario validation
    
    fault         - Systematic fault injection testing
                   - Resilience under failure conditions
                   - Error handling validation
                   - Recovery capability testing
    
    performance   - Load and stress testing
                   - Response time validation
                   - Throughput measurement
                   - Resource usage monitoring

EOF
}

# Parse command line arguments
parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -c|--category)
                CATEGORY="$2"
                shift 2
                ;;
            -v|--verbose)
                VERBOSE=true
                shift
                ;;
            --coverage)
                COVERAGE=true
                shift
                ;;
            --ci)
                CI_MODE=true
                shift
                ;;
            --no-parallel)
                PARALLEL=false
                shift
                ;;
            --timeout)
                TIMEOUT="$2"
                shift 2
                ;;
            --format)
                OUTPUT_FORMAT="$2"
                shift 2
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
}

# Setup test environment
setup_environment() {
    print_header "Setting Up Test Environment"
    
    # Create results directories
    mkdir -p "$RESULTS_DIR"
    mkdir -p "$COVERAGE_DIR"
    
    # Check if containers are running
    if ! docker ps | grep -q "snapdog-app-1"; then
        print_warning "SnapDog2 containers not running. Starting development environment..."
        cd "$PROJECT_ROOT"
        ./dev.sh start
        
        # Wait for services to be ready
        print_info "Waiting for services to be ready..."
        sleep 15
    fi
    
    # Verify services are accessible
    if ! curl -s http://localhost:8000/health > /dev/null; then
        print_error "SnapDog2 API is not accessible"
        exit 1
    fi
    
    print_success "Test environment ready"
}

# Build test arguments
build_test_args() {
    local args=()
    
    # Base arguments
    args+=("test")
    args+=("$TEST_PROJECT")
    args+=("--logger" "console;verbosity=normal")
    args+=("--results-directory" "$RESULTS_DIR")
    
    # Timeout
    args+=("--blame-hang-timeout" "$TIMEOUT")
    
    # Output format
    case $OUTPUT_FORMAT in
        "junit")
            args+=("--logger" "junit;LogFilePath=$RESULTS_DIR/results.xml")
            ;;
        "summary")
            args+=("--verbosity" "minimal")
            ;;
        "detailed")
            if [[ "$VERBOSE" == "true" ]]; then
                args+=("--verbosity" "diagnostic")
            fi
            ;;
    esac
    
    # Coverage
    if [[ "$COVERAGE" == "true" ]]; then
        args+=("--collect-coverage")
        args+=("--coverage-output-format" "cobertura")
        args+=("--coverage-output" "$COVERAGE_DIR/coverage.xml")
    fi
    
    # Parallel execution
    if [[ "$PARALLEL" == "true" ]]; then
        args+=("--parallel")
    else
        args+=("--parallel" "none")
    fi
    
    # CI mode optimizations
    if [[ "$CI_MODE" == "true" ]]; then
        args+=("--no-build")
        args+=("--configuration" "Release")
    fi
    
    echo "${args[@]}"
}

# Run specific test category
run_test_category() {
    local category=$1
    local test_filter=""
    local category_name=""
    
    case $category in
        "realworld")
            test_filter="FullyQualifiedName~$REAL_WORLD_TESTS"
            category_name="Real-World Integration Scenarios"
            ;;
        "fault")
            test_filter="FullyQualifiedName~$FAULT_INJECTION_TESTS"
            category_name="Fault Injection Tests"
            ;;
        "performance")
            test_filter="FullyQualifiedName~$PERFORMANCE_TESTS"
            category_name="Performance Tests"
            ;;
        "all")
            test_filter="FullyQualifiedName~ZoneGroupingRealWorldTests|FullyQualifiedName~FaultInjectionTests|FullyQualifiedName~ZoneGroupingPerformanceTests"
            category_name="All Real-World Tests"
            ;;
        *)
            print_error "Unknown test category: $category"
            exit 1
            ;;
    esac
    
    print_header "Running $category_name"
    
    local test_args=($(build_test_args))
    test_args+=("--filter" "$test_filter")
    
    print_info "Executing: dotnet ${test_args[*]}"
    
    if dotnet "${test_args[@]}"; then
        print_success "$category_name completed successfully"
        return 0
    else
        print_error "$category_name failed"
        return 1
    fi
}

# Generate test report
generate_report() {
    print_header "Generating Test Report"
    
    local report_file="$RESULTS_DIR/real-world-test-report.md"
    
    cat > "$report_file" << EOF
# 🧪 Real-World Scenario Test Report

**Generated:** $(date)
**Environment:** $(uname -s) $(uname -r)
**Category:** ${CATEGORY:-all}

## Test Execution Summary

EOF
    
    # Add test results if available
    if [[ -f "$RESULTS_DIR/results.xml" ]]; then
        print_info "JUnit results available at: $RESULTS_DIR/results.xml"
        echo "- JUnit XML results: \`results.xml\`" >> "$report_file"
    fi
    
    # Add coverage results if available
    if [[ -f "$COVERAGE_DIR/coverage.xml" ]]; then
        print_info "Coverage report available at: $COVERAGE_DIR/coverage.xml"
        echo "- Coverage report: \`Coverage/coverage.xml\`" >> "$report_file"
    fi
    
    cat >> "$report_file" << EOF

## Test Categories Executed

### Real-World Integration Scenarios
- Complete end-to-end workflow validation
- API integration testing
- Cross-system consistency verification
- Recovery scenario validation

### Fault Injection Tests
- Systematic failure condition testing
- Resilience validation
- Error handling verification
- Recovery capability testing

### Performance Tests
- Load and stress testing
- Response time validation
- Throughput measurement
- Resource usage monitoring

## Results Location

All test results are available in: \`$RESULTS_DIR\`

EOF
    
    print_success "Test report generated: $report_file"
}

# Cleanup function
cleanup() {
    print_info "Cleaning up test environment..."
    # Add any cleanup logic here
}

# Main execution
main() {
    # Set up trap for cleanup
    trap cleanup EXIT
    
    # Parse arguments
    parse_arguments "$@"
    
    # Set default category if not specified
    if [[ -z "$CATEGORY" ]]; then
        CATEGORY="all"
    fi
    
    print_header "Enterprise-Grade Real-World Scenario Testing"
    print_info "Category: $CATEGORY"
    print_info "Verbose: $VERBOSE"
    print_info "Coverage: $COVERAGE"
    print_info "CI Mode: $CI_MODE"
    print_info "Parallel: $PARALLEL"
    print_info "Timeout: $TIMEOUT"
    print_info "Format: $OUTPUT_FORMAT"
    
    # Setup environment
    setup_environment
    
    # Run tests
    local exit_code=0
    if ! run_test_category "$CATEGORY"; then
        exit_code=1
    fi
    
    # Generate report
    generate_report
    
    # Final status
    if [[ $exit_code -eq 0 ]]; then
        print_header "🎉 All Tests Completed Successfully!"
        print_success "Real-world scenario testing passed"
        print_info "Results available in: $RESULTS_DIR"
    else
        print_header "❌ Tests Failed"
        print_error "Some real-world scenario tests failed"
        print_info "Check results in: $RESULTS_DIR"
    fi
    
    exit $exit_code
}

# Execute main function with all arguments
main "$@"

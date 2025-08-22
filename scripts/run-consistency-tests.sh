#!/bin/bash

# Command Framework Consistency Test Runner
# Runs all consistency tests and generates reports

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TEST_PROJECT="$PROJECT_ROOT/SnapDog2.Tests"
REPORTS_DIR="$PROJECT_ROOT/test-reports"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")

echo -e "${BLUE}🔍 Command Framework Consistency Test Runner${NC}"
echo "=================================================="
echo "Project Root: $PROJECT_ROOT"
echo "Test Project: $TEST_PROJECT"
echo "Reports Dir:  $REPORTS_DIR"
echo "Timestamp:    $TIMESTAMP"
echo

# Create reports directory
mkdir -p "$REPORTS_DIR"

# Function to run test category
run_test_category() {
    local category=$1
    local description=$2
    local report_file="$REPORTS_DIR/consistency-${category,,}-${TIMESTAMP}.trx"
    
    echo -e "${BLUE}Running $description...${NC}"
    
    if dotnet test "$TEST_PROJECT" \
        --filter "Category=${category}" \
        --logger "trx;LogFileName=${report_file}" \
        --logger "console;verbosity=normal" \
        --no-build; then
        echo -e "${GREEN}✓ $description passed${NC}"
        return 0
    else
        echo -e "${RED}✗ $description failed${NC}"
        return 1
    fi
}

# Function to generate summary report
generate_summary_report() {
    local summary_file="$REPORTS_DIR/consistency-summary-${TIMESTAMP}.md"
    
    echo -e "${BLUE}Generating summary report...${NC}"
    
    cat > "$summary_file" << EOF
# Command Framework Consistency Test Summary

**Generated**: $(date -u +"%Y-%m-%d %H:%M:%S") UTC
**Timestamp**: $TIMESTAMP

## Test Categories

EOF

    # Add test results to summary
    for category in "CoreConsistency" "ApiConsistency" "MqttConsistency" "CrossProtocolConsistency"; do
        local report_file="$REPORTS_DIR/consistency-${category,,}-${TIMESTAMP}.trx"
        if [[ -f "$report_file" ]]; then
            echo "- ✅ $category: Report generated" >> "$summary_file"
        else
            echo "- ❌ $category: No report found" >> "$summary_file"
        fi
    done
    
    cat >> "$summary_file" << EOF

## Reports Location

All detailed test reports are available in:
\`$REPORTS_DIR\`

## Next Steps

1. Review any failed tests in the detailed reports
2. Implement missing command/status support as indicated
3. Update implementation status documentation
4. Re-run tests to verify fixes

EOF

    echo -e "${GREEN}✓ Summary report generated: $summary_file${NC}"
}

# Main execution
main() {
    echo -e "${YELLOW}Building test project...${NC}"
    if ! dotnet build "$TEST_PROJECT" --configuration Debug; then
        echo -e "${RED}✗ Build failed${NC}"
        exit 1
    fi
    echo -e "${GREEN}✓ Build successful${NC}"
    echo

    local failed_categories=()
    
    # Run each test category
    if ! run_test_category "CoreConsistency" "Core Registry Tests"; then
        failed_categories+=("CoreConsistency")
    fi
    echo
    
    if ! run_test_category "ApiConsistency" "API Protocol Tests"; then
        failed_categories+=("ApiConsistency")
    fi
    echo
    
    if ! run_test_category "MqttConsistency" "MQTT Protocol Tests"; then
        failed_categories+=("MqttConsistency")
    fi
    echo
    
    if ! run_test_category "CrossProtocolConsistency" "Cross-Protocol Tests"; then
        failed_categories+=("CrossProtocolConsistency")
    fi
    echo
    
    # Run all consistency tests together for overall report
    echo -e "${BLUE}Running all consistency tests...${NC}"
    local all_report_file="$REPORTS_DIR/consistency-all-${TIMESTAMP}.trx"
    
    dotnet test "$TEST_PROJECT" \
        --filter "Category=Consistency" \
        --logger "trx;LogFileName=${all_report_file}" \
        --logger "console;verbosity=detailed" \
        --no-build
    
    echo
    
    # Generate summary report
    generate_summary_report
    
    # Final status
    echo
    echo "=================================================="
    if [[ ${#failed_categories[@]} -eq 0 ]]; then
        echo -e "${GREEN}🎉 All consistency tests passed!${NC}"
        echo -e "${GREEN}Command framework implementation is consistent across all protocols.${NC}"
        exit 0
    else
        echo -e "${RED}⚠️  Some consistency tests failed:${NC}"
        for category in "${failed_categories[@]}"; do
            echo -e "${RED}  - $category${NC}"
        done
        echo
        echo -e "${YELLOW}Check the detailed reports in $REPORTS_DIR for specific issues.${NC}"
        exit 1
    fi
}

# Help function
show_help() {
    cat << EOF
Command Framework Consistency Test Runner

Usage: $0 [OPTIONS]

Options:
    -h, --help          Show this help message
    -c, --category CAT  Run only specific category (CoreConsistency, ApiConsistency, MqttConsistency, CrossProtocolConsistency)
    -v, --verbose       Enable verbose output
    --no-build         Skip build step

Examples:
    $0                           # Run all consistency tests
    $0 -c CoreConsistency       # Run only core registry tests
    $0 -c ApiConsistency        # Run only API protocol tests
    $0 --verbose                # Run with verbose output

Reports are generated in: $REPORTS_DIR

EOF
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            exit 0
            ;;
        -c|--category)
            CATEGORY="$2"
            shift 2
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        --no-build)
            NO_BUILD=true
            shift
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            show_help
            exit 1
            ;;
    esac
done

# Run specific category if requested
if [[ -n "$CATEGORY" ]]; then
    echo -e "${BLUE}Running specific category: $CATEGORY${NC}"
    echo
    
    if [[ "$NO_BUILD" != true ]]; then
        echo -e "${YELLOW}Building test project...${NC}"
        dotnet build "$TEST_PROJECT" --configuration Debug
        echo
    fi
    
    case "$CATEGORY" in
        "CoreConsistency")
            run_test_category "CoreConsistency" "Core Registry Tests"
            ;;
        "ApiConsistency")
            run_test_category "ApiConsistency" "API Protocol Tests"
            ;;
        "MqttConsistency")
            run_test_category "MqttConsistency" "MQTT Protocol Tests"
            ;;
        "CrossProtocolConsistency")
            run_test_category "CrossProtocolConsistency" "Cross-Protocol Tests"
            ;;
        *)
            echo -e "${RED}Unknown category: $CATEGORY${NC}"
            echo "Valid categories: CoreConsistency, ApiConsistency, MqttConsistency, CrossProtocolConsistency"
            exit 1
            ;;
    esac
else
    # Run all tests
    main
fi

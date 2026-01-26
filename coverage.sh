#!/bin/bash
set -e

# Code Coverage Script for EFCore.FluentIncludes
# Runs all tests with coverage collection and generates an HTML report

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Configuration
COVERAGE_DIR="$SCRIPT_DIR/coverage"
REPORT_DIR="$COVERAGE_DIR/report"

# Clean previous coverage results
echo "Cleaning previous coverage results..."
rm -rf "$COVERAGE_DIR"
mkdir -p "$COVERAGE_DIR"

# Restore tools (in case not already done)
dotnet tool restore

# Run tests with coverage
echo "Running tests with code coverage..."
dotnet test \
    --settings coverage.runsettings \
    --results-directory "$COVERAGE_DIR" \
    --collect:"XPlat Code Coverage" \
    --no-build \
    "$@"

# Find coverage files
COVERAGE_FILES=$(find "$COVERAGE_DIR" -name "coverage.cobertura.xml" | tr '\n' ';')

if [ -z "$COVERAGE_FILES" ]; then
    echo "No coverage files found!"
    exit 1
fi

# Generate HTML report
echo "Generating HTML report..."
dotnet reportgenerator \
    -reports:"$COVERAGE_FILES" \
    -targetdir:"$REPORT_DIR" \
    -reporttypes:"Html;TextSummary"

# Show summary
echo ""
echo "=========================================="
cat "$REPORT_DIR/Summary.txt"
echo "=========================================="
echo ""
echo "HTML report generated at: $REPORT_DIR/index.html"

# Open report in browser (macOS)
if [[ "$OSTYPE" == "darwin"* ]]; then
    read -p "Open report in browser? [Y/n] " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]] || [[ -z $REPLY ]]; then
        open "$REPORT_DIR/index.html"
    fi
fi

#!/usr/bin/env python3
"""
Run all dynamic resource tests using pytest

This script runs pytest on the specified test files, with the option to run
only unit tests or only integration tests. Integration tests require Unity
to be running with the MCP plugin loaded.
"""

import sys
import os
import subprocess
import argparse

def run_tests(test_files, verbose=True, markers=None):
    """Run pytest on specific files using the current Python interpreter"""
    cmd = [sys.executable, "-m", "pytest"]
    
    if verbose:
        cmd.append("-xvs")
    
    if markers:
        cmd.append(f"-m={markers}")
        
    cmd.extend(test_files)
    
    print(f"\n{'=' * 60}")
    print(f"Running tests: {' '.join(test_files)}")
    print(f"{'=' * 60}\n")
    
    result = subprocess.run(cmd)
    return result.returncode == 0

def main():
    """Run dynamic resource tests"""
    parser = argparse.ArgumentParser(description='Run dynamic resource tests using pytest')
    parser.add_argument('--unit-only', action='store_true', help='Only run unit tests')
    parser.add_argument('--integration-only', action='store_true', help='Only run integration tests')
    parser.add_argument('--mock-only', action='store_true', help='Only run tests with mocked Unity client')
    parser.add_argument('-v', '--verbose', action='store_true', help='Show verbose output')
    args = parser.parse_args()
    
    # Get the directory where this script is located
    script_dir = os.path.dirname(os.path.abspath(__file__))
    
    # Change to the tests directory
    os.chdir(script_dir)
    
    success = True
    
    if args.unit_only:
        # Run unit tests (TestDynamicResourcesMocked and non-integration tests)
        print("\n📋 Running Unit Tests")
        print("-" * 60)
        unit_test_files = [
            "test_resource_parameters.py",
            "test_resource_context.py",
            "test_uri_parameter_matching.py"
        ]
        success = run_tests(unit_test_files, args.verbose) and success
    elif args.mock_only:
        # Run only tests with mocked Unity client
        print("\n🔄 Running Mocked Tests")
        print("-" * 60)
        print("Note: These tests use a mocked Unity client and do not require Unity to be running")
        integration_test_files = [
            "test_dynamic_tools.py::TestDynamicToolsMocked",
            "test_dynamic_resources.py::TestDynamicResourcesMocked"
        ]
        success = run_tests(integration_test_files, args.verbose) and success
    elif args.integration_only:
        # Run integration tests that require Unity
        print("\n🔄 Running Integration Tests")
        print("-" * 60)
        print("Note: Integration tests require Unity to be running with the MCP plugin loaded")
        print("      and the TCP server enabled on port 8080.")
        
        integration_test_files = [
            "test_dynamic_tools.py::TestDynamicTools",
            "test_dynamic_resources.py::TestDynamicResources"
        ]
        success = run_tests(integration_test_files, args.verbose) and success
    else:
        # Run all tests
        print("\n📋 Running All Tests")
        print("-" * 60)
        print("Note: Some tests require Unity to be running with the MCP plugin loaded")
        print("      and the TCP server enabled on port 8080.")
        print("      Use --unit-only or --mock-only to run tests that don't require Unity")
        all_test_files = [
            "test_resource_parameters.py",
            "test_resource_context.py",
            "test_uri_parameter_matching.py",
            "test_dynamic_tools.py",
            "test_dynamic_resources.py"
        ]
        success = run_tests(all_test_files, args.verbose) and success
    
    # Print summary
    print("\n📊 Test Summary")
    print("-" * 60)
    if success:
        print("✅ All tests passed successfully!")
    else:
        print("❌ Some tests failed. Check the logs above for details.")
        
    return 0 if success else 1

if __name__ == "__main__":
    sys.exit(main())
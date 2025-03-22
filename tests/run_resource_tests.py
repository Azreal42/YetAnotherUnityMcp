#!/usr/bin/env python3
"""
Run all dynamic resource tests
This script runs the unit tests and integration tests for resource parameter handling
"""

import sys
import os
import subprocess
import argparse
import time

def run_pytest(test_file, verbose=True):
    """Run pytest on a specific file using the current Python interpreter"""
    cmd = [sys.executable, "-m", "pytest"]
    if verbose:
        cmd.append("-xvs")
    cmd.append(test_file)
    
    print(f"\n{'=' * 60}")
    print(f"Running tests in {test_file}")
    print(f"{'=' * 60}\n")
    
    result = subprocess.run(cmd)
    return result.returncode == 0

def run_script(script_file, args=None):
    """Run a Python script directly using the current Python interpreter"""
    cmd = [sys.executable, script_file]
    if args:
        cmd.extend(args)
        
    print(f"\n{'=' * 60}")
    print(f"Running script {script_file} {' '.join(args or [])}")
    print(f"{'=' * 60}\n")
    
    result = subprocess.run(cmd)
    return result.returncode == 0

def main():
    parser = argparse.ArgumentParser(description='Run dynamic resource tests')
    parser.add_argument('--unit-only', action='store_true', help='Only run unit tests')
    parser.add_argument('--integration-only', action='store_true', help='Only run integration tests')
    parser.add_argument('-v', '--verbose', action='store_true', help='Show verbose output')
    args = parser.parse_args()
    
    # Get the directory where this script is located
    script_dir = os.path.dirname(os.path.abspath(__file__))
    
    # Change to the tests directory
    os.chdir(script_dir)
    
    success = True
    
    # Unit tests
    if not args.integration_only:
        print("\n📋 Running Unit Tests")
        print("-" * 60)
        
        # Run the unit tests
        test_files = [
            "test_resource_parameters.py",
            "test_resource_context.py",
            "test_uri_parameter_matching.py"
        ]
        
        for test_file in test_files:
            file_success = run_pytest(test_file, args.verbose)
            if not file_success:
                success = False
                print(f"❌ Tests in {test_file} failed")
            else:
                print(f"✅ Tests in {test_file} passed")
    
    # Integration tests
    if not args.unit_only:
        print("\n🔄 Running Integration Tests")
        print("-" * 60)
        
        # Check if Unity is running
        print("Note: Integration tests require Unity to be running with the MCP plugin loaded")
        print("      and the TCP server enabled on port 8080.")
        
        # Pause to give user time to read
        if not args.verbose:
            time.sleep(2)
            
        # Run integration tests
        integration_scripts = [
            ("test_dynamic_tools.py", None),
            ("test_dynamic_resources.py", None)
        ]
        
        for script, script_args in integration_scripts:
            file_success = run_script(script, script_args)
            if not file_success:
                success = False
                print(f"❌ Script {script} failed")
            else:
                print(f"✅ Script {script} passed")
    
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
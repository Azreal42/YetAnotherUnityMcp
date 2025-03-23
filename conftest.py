"""Global pytest configuration file"""

import pytest

def pytest_configure(config):
    # Set asyncio fixture loop scope
    config.option.asyncio_default_fixture_loop_scope = "function"
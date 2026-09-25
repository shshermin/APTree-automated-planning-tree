"""
Moveit_bridge_service.py and move_to_task.py import rclpy (ROS 2) 
at module level, so they can only be imported on a machine with a 
ROS 2 install. Without it they are skipped here rather than mocked 
wholesale - a mocked rclpy would test the mock.
"""
import importlib

import pytest

pytest.importorskip("rclpy", reason="ROS 2 (rclpy) not installed - MoveIt bridge tests need a ROS 2 environment")


@pytest.mark.parametrize("module", ["moveit_bridge_service", "move_to_task"])
def test_module_imports(module):
    importlib.import_module(module)

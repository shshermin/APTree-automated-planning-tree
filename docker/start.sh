#!/usr/bin/env bash
set -euo pipefail

# Start Python planner service (bound to all interfaces inside the container
# so the published port works; publish it only on the host's loopback)
PLANNER_HOST=0.0.0.0 python3 /opt/python_service/pddl_planning_service.py &

# Start ASP.NET backend
exec dotnet /app/BehaviorTreeMainProject.dll --urls http://0.0.0.0:5254

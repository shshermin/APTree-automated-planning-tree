#!/usr/bin/env bash
set -euo pipefail

# Start Python planner service
python3 /opt/python_service/pddl_planning_service.py &

# Start ASP.NET backend
exec dotnet /app/BehaviorTreeMainProject.dll --urls http://0.0.0.0:5254

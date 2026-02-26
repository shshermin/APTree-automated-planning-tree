#!/bin/bash
pkill -f pddl_planning_service.py
sleep 1
cd ~/APTree-automated-planning-tree/APTreeExecutionEngine/python_service
source pddl_env/bin/activate
nohup python pddl_planning_service.py > /tmp/flask.log 2>&1 &
sleep 2
cat /tmp/flask.log

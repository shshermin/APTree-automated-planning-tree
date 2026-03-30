ROBOT_IP = "192.168.1.100"



loading and running a program on the pendant (powershell):
cd c:\Users\sherk\Documents\BehaviorTreeMainProject\APTreeExecutionEngine\python_service
python -c "from ur10_control.ur10_commands import play_program; print(play_program('192.168.1.100', 'testdemo.urp', speed=30))"


// setting a named position 
python -c "from ur10_control.ur10_commands import save_position; pos = save_position('192.168.1.100', 'home'); print(pos)"

// getting robot coordinates
cd c:\Users\sherk\Documents\BehaviorTreeMainProject\APTreeExecutionEngine\python_service
python -c "from ur10_control.ur10_commands import get_current_pose; print(get_current_pose('192.168.1.100'))"






// moving to the locmanipulate
Invoke-RestMethod -Method Post -Uri "http://localhost:5001/move" -ContentType "application/json" -Body '{"commandType": "movel", "finalPosition": "rpmanipulate", "robotIp": "192.168.1.100", "pose": [0.133772, 0.34874, 0.490062, 3.137544, -0.07093, 0.006593], "velocity": 0.1, "acceleration": 0.1}'

// first pickupstick


Setting up all services:

terminal 1:
ssh -L 5000:localhost:5000 -i C:\Users\sherk\.ssh\id_ed25519 ubuntu@193.196.52.17

Terminal 2: 
ssh -i C:\Users\sherk\.ssh\id_ed25519 ubuntu@193.196.52.17
cd APTree-automated-planning-tree/APTreeExecutionEngine/python_service
source pddl_env/bin/activate
python pddl_planning_service.py

terminal 3:
cd c:\Users\sherk\Documents\BehaviorTreeMainProject\APTreeExecutionEngine\python_service
python robot_service.py



step #1: deequip gripper
cd c:\Users\sherk\Documents\BehaviorTreeMainProject\APTreeExecutionEngine\python_service
python -c "from ur10_control.ur10_commands import play_program; print(play_program('192.168.1.100', 'deequipdemo.urp', speed=10))"

step #2: equip gripper
python -c "from ur10_control.ur10_commands import play_program; print(play_program('192.168.1.100', 'equipdemo.urp', speed=10))"

step#3: go to the rpmanipulate
Invoke-RestMethod -Method Post -Uri "http://localhost:5001/move" -ContentType "application/json" -Body '{"commandType": "movel", "finalPosition": "rpmanipulate", "robotIp": "192.168.1.100", "pose": [0.133772, 0.34874, 0.490062, 0.0, -3.14159, 0.0], "velocity": 0.05, "acceleration": 0.1}'


rppickupInvoke-RestMethod -Method Post -Uri "http://localhost:5001/move" -ContentType "application/json" -Body '{"commandType": "movel", "finalPosition": "rppickup", "robotIp": "192.168.1.100", "pose": [0.602835, -0.254902, 0.339901, 0.0, -3.14159, 0.0], "velocity": 0.05, "acceleration": 0.1}'

cd ..
Invoke-RestMethod -Method Post -Uri "http://localhost:5001/move" -ContentType "application/json" -Body '{"commandType": "movel", "finalPosition": "rpmanipulate", "robotIp": "192.168.1.100", "pose": [0.133772, 0.34874, 0.490062, 3.137544, -0.07093, 0.006593], "velocity": 0.1, "acceleration": 0.1}'



ROBOT_IP = "192.168.1.100"



loading and running a program on the pendant (powershell):
cd c:\Users\sherk\Documents\BehaviorTreeMainProject\APTreeExecutionEngine\python_service
python -c "from ur10_control.ur10_commands import play_program; print(play_program('192.168.1.100', 'testdemo.urp', speed=30))"


// setting a named position 
python -c "from ur10_control.ur10_commands import save_position; pos = save_position('192.168.1.100', 'home'); print(pos)"

// moving between named positions 



// named position “rpmanipulate”
Saved position 'home': joints=[1.689459, -0.942758, -2.22138, -1.551921, 1.549635, 0.160935]
{'name': 'home', 'joints': [1.689459, -0.942758, -2.22138, -1.551921, 1.549635, 0.160935], 'tcp_pose': [0.133772, 0.34874, 0.490062, 3.137544, -0.07093, 0.006593]}
// named position “rppickup”
Saved position 'home': joints=[-0.030077, -1.543035, -2.092812, -1.071979, 1.552743, -1.616215]
{'name': 'home', 'joints': [-0.030077, -1.543035, -2.092812, -1.071979, 1.552743, -1.616215], 'tcp_pose': [0.59937, -0.193137, 0.349881, 3.138425, 0.006595, 0.006684]}
// named position “rpequip”

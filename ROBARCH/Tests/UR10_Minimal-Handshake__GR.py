#________________________________________________________________
# Move the UR10 between two joint targets (in radians) and exit.
#________________________________________________________________

# To run this script:
# 1. adapt Ipv4 to the robot's IP (RoboLab: 169.254.130.206) (Gili's IP for RoboLab 169.254.130.100)
# 2. Activate your environment (GR uses Develope powershell for VS)
# 2. Run the script
# 3. The robot will move between the two joint targets and exit.


from rtde_control import RTDEControlInterface as RTDEControl
from rtde_receive import RTDEReceiveInterface as RTDEReceive
import time
import math

ROBOT_IP = "192.168.3.60" #QUT
#"169.254.130.206"  # UR10, RoboLab K1


#_1_Motion parameters
SPEED = 0.6   # rad/s  (joint speed)
ACC   = 0.8   # rad/s^2 (joint acceleration)
BLEND = 0.0   # 0 => stop at each point (no blending)

#_2_two targets, described by Joint Position, translated into radians (roboLab_GR_26.10.25)
q1_deg = [-93.25, -95.68, 76.57, -67.78, -94.29, -202.23]
q1_rad = [math.radians(d) for d in q1_deg]

q2_deg= [-93.25, -92.17, 0104.64, -100.74, -91.93, -195.75] 
q2_rad = [math.radians(d) for d in q2_deg] 

#_3_Main function	
def main():
    rtde_c = RTDEControl(ROBOT_IP)
    rtde_r = RTDEReceive(ROBOT_IP)

    try:
        # print joint state
        print("Current q:", [round(v, 3) for v in rtde_r.getActualQ()]) 
        # Move to q1
        print("moveJ -> q1"); rtde_c.moveJ(q1_rad, SPEED, ACC, asynchronous=False)
        # Small pause
        time.sleep(0.3)
        # Move to q2
        print("moveJ -> q2"); rtde_c.moveJ(q2_rad, SPEED, ACC, asynchronous=False)
        # Small pause
        time.sleep(0.3)
        print("moveJ -> q1"); rtde_c.moveJ(q1_rad, SPEED, ACC, asynchronous=False)
        print("Done.")
    finally:
        try: rtde_c.stopScript()
        except Exception: pass

if __name__ == "__main__":
    main()

namespace RobotCommand
{
    public class RobotCommandRequest
    {
        public string Endpoint { get; set; } = "/move";
        public string CommandType { get; set; }
        public string InitialPosition { get; set; }
        public string FinalPosition { get; set; }
        public string RobotIp { get; set; }
        public double Velocity { get; set; } = 0.5;
        public double Acceleration { get; set; } = 1.0;

        /// <summary>Joint angles array — populated by ServiceInputProvider from the blackboard.</summary>
        public double[] Joints { get; set; }

        /// <summary>TCP pose [x, y, z, rx, ry, rz] — populated by ServiceInputProvider from the blackboard.</summary>
        public double[] Pose { get; set; }

        /// <summary>Program name for /play_program endpoint (e.g. "equipdemo.urp").</summary>
        public string ProgramName { get; set; }

        /// <summary>Speed slider percentage for /play_program endpoint.</summary>
        public int Speed { get; set; } = 30;

        /// <summary>Payload mass in kg to set after program finishes (for /play_program).</summary>
        public double? Payload { get; set; }

        /// <summary>Payload center of gravity [cx, cy, cz] in meters (for /play_program).</summary>
        public double[] PayloadCog { get; set; }

        /// <summary>TCP offset [x, y, z, rx, ry, rz] to set after program finishes (for /play_program).</summary>
        public double[] Tcp { get; set; }

        /// <summary>End effector type for planned moves ("gripper" or "nailgun").</summary>
        public string EndEffectorType { get; set; }
    }
}

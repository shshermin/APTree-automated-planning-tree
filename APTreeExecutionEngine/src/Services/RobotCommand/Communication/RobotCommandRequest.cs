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
    }
}

using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class RobotPosition : Location
    {
        public RobotJoints Joints { get; set; }
        public Coordinate TcpPose { get; set; }
        public Coordinate TcpOrinetation { get; set; }

        // Empty constructor - required by CustomProperty
        public RobotPosition() : base()
        {
            BaseType = new FastName("Location");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public RobotPosition(RobotJoints joints, Coordinate tcpPose, Coordinate tcpOrinetation) : this()
        {
            this.Joints = joints;
            this.TcpPose = tcpPose;
            this.TcpOrinetation = tcpOrinetation;
        }

        // Constructor with name and parameters
        public RobotPosition(string name, RobotJoints joints, Coordinate tcpPose, Coordinate tcpOrinetation) : base(name)
        {
            this.Joints = joints;
            this.TcpPose = tcpPose;
            this.TcpOrinetation = tcpOrinetation;
            BaseType = new FastName("Location");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set RobotPosition-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

            // Set Joints property
            if (parameters.ContainsKey("joints"))
            {
                if (parameters["joints"] is RobotJoints jointsValue)
                    Joints = jointsValue;
                else if (parameters["joints"] is string jointsStr && !string.IsNullOrWhiteSpace(jointsStr))
                    Joints = RobotJoints.Parse(jointsStr);
            }

            // Set TcpPose property
            if (parameters.ContainsKey("tcpPose"))
            {
                if (parameters["tcpPose"] is Coordinate tcpPoseValue)
                    TcpPose = tcpPoseValue;
                else if (parameters["tcpPose"] is string tcpPoseStr)
                    TcpPose = Coordinate.Parse(tcpPoseStr);
            }

            // Set TcpOrinetation property
            if (parameters.ContainsKey("tcpOrinetation"))
            {
                if (parameters["tcpOrinetation"] is Coordinate tcpOrinetationValue)
                    TcpOrinetation = tcpOrinetationValue;
                else if (parameters["tcpOrinetation"] is string tcpOrinetationStr)
                    TcpOrinetation = Coordinate.Parse(tcpOrinetationStr);
            }

        }
    }
}

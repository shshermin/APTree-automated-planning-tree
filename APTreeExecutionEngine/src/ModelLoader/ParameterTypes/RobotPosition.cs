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

        // Override SetParameters to set RobotPosition-specific properties.
        // Handles both pre-parsed objects and raw comma-separated strings from JSON.
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            base.SetParameters(parameters);

            // ── Joints ──
            if (parameters.TryGetValue("joints", out var jointsVal))
            {
                if (jointsVal is RobotJoints rj)
                    Joints = rj;
                else if (jointsVal is string jointsStr && !string.IsNullOrWhiteSpace(jointsStr))
                    Joints = RobotJoints.Parse(jointsStr);
            }

            // ── TcpPose ──
            if (parameters.TryGetValue("tcpPose", out var poseVal))
            {
                if (poseVal is Coordinate c)
                    TcpPose = c;
                else if (poseVal is string poseStr && !string.IsNullOrWhiteSpace(poseStr))
                    TcpPose = Coordinate.Parse(poseStr);
            }

            // ── TcpOrinetation ──
            if (parameters.TryGetValue("tcpOrinetation", out var orientVal))
            {
                if (orientVal is Coordinate oc)
                    TcpOrinetation = oc;
                else if (orientVal is string orientStr && !string.IsNullOrWhiteSpace(orientStr))
                    TcpOrinetation = Coordinate.Parse(orientStr);
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class RobotPosition : Location
    {
        public string NamedPos { get; set; }

        // Empty constructor - required by CustomProperty
        public RobotPosition() : base()
        {
            BaseType = new FastName("Location");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public RobotPosition(string namedPos) : this()
        {
            this.NamedPos = namedPos;
        }

        // Constructor with name and parameters
        public RobotPosition(string name, string namedPos) : base(name)
        {
            this.NamedPos = namedPos;
            BaseType = new FastName("Location");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set RobotPosition-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

            // Set NamedPos property
            if (parameters.ContainsKey("namedPos"))
            {
                NamedPos = parameters["namedPos"].ToString();
            }

        }
    }
}

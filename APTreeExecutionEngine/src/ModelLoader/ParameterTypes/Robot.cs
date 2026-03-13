using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class Robot : Agent
    {
        public Tool Tool { get; set; }
        public bool RobothasTool { get; set; }
        public RobotPosition Loc { get; set; }

        // Empty constructor - required by CustomProperty
        public Robot() : base()
        {
            BaseType = new FastName("Agent");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public Robot(Tool tool, bool robothasTool, RobotPosition loc) : this()
        {
            this.Tool = tool;
            this.RobothasTool = robothasTool;
            this.Loc = loc;
        }

        // Constructor with name and parameters
        public Robot(string name, Tool tool, bool robothasTool, RobotPosition loc) : base(name)
        {
            this.Tool = tool;
            this.RobothasTool = robothasTool;
            this.Loc = loc;
            BaseType = new FastName("Agent");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set Robot-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

            // Set Tool property
            if (parameters.ContainsKey("tool"))
            {
                if (parameters["tool"] is Tool toolValue)
                {
                    Tool = toolValue;
                }
            }

            // Set RobothasTool property
            if (parameters.ContainsKey("robothasTool"))
            {
                RobothasTool = Convert.ToBoolean(parameters["robothasTool"]);
            }

            // Set Loc property
            if (parameters.ContainsKey("loc"))
            {
                if (parameters["loc"] is RobotPosition locValue)
                {
                    Loc = locValue;
                }
            }

        }
    }
}

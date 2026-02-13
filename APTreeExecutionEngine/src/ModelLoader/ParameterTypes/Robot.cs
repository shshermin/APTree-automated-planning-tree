using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class Robot : Agent
    {
        public string Type { get; set; }
        public double Speed { get; set; }

        // Empty constructor - required by Entity
        public Robot() : base()
        {
            BaseType = new FastName("Agent");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public Robot(string type, double speed) : this()
        {
            this.Type = type;
            this.Speed = speed;
        }

        // Constructor with name and parameters
        public Robot(string name, string type, double speed) : base(name)
        {
            this.Type = type;
            this.Speed = speed;
            BaseType = new FastName("Agent");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set Robot-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

            // Set Type property
            if (parameters.ContainsKey("type"))
            {
                Type = parameters["type"].ToString();
            }

            // Set Speed property
            if (parameters.ContainsKey("speed"))
            {
                Speed = Convert.ToDouble(parameters["speed"]);
            }

        }
    }
}

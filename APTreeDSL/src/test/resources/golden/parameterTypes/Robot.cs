using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class Robot : Agent
    {
        public Location Loc { get; set; }
        public string MType { get; set; }

        // Empty constructor - required by CustomProperty
        public Robot() : base()
        {
            BaseType = new FastName("Agent");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public Robot(Location loc, string mType) : this()
        {
            this.Loc = loc;
            this.MType = mType;
        }

        // Constructor with name and parameters
        public Robot(string name, Location loc, string mType) : base(name)
        {
            this.Loc = loc;
            this.MType = mType;
            BaseType = new FastName("Agent");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set Robot-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

            // Set Loc property
            if (parameters.ContainsKey("loc"))
            {
                if (parameters["loc"] is Location locValue)
                {
                    Loc = locValue;
                }
            }

            // Set MType property
            if (parameters.ContainsKey("mType"))
            {
                MType = parameters["mType"].ToString();
            }

        }
    }
}

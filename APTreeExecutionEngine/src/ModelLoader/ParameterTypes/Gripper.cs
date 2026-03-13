using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class Gripper : Tool
    {
        public bool IsOpen { get; set; }
        public bool GripperEmpty { get; set; }
        public Location Loc { get; set; }

        // Empty constructor - required by CustomProperty
        public Gripper() : base()
        {
            BaseType = new FastName("Tool");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public Gripper(bool isOpen, bool gripperEmpty, Location loc) : this()
        {
            this.IsOpen = isOpen;
            this.GripperEmpty = gripperEmpty;
            this.Loc = loc;
        }

        // Constructor with name and parameters
        public Gripper(string name, bool isOpen, bool gripperEmpty, Location loc) : base(name)
        {
            this.IsOpen = isOpen;
            this.GripperEmpty = gripperEmpty;
            this.Loc = loc;
            BaseType = new FastName("Tool");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set Gripper-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

            // Set IsOpen property
            if (parameters.ContainsKey("isOpen"))
            {
                IsOpen = Convert.ToBoolean(parameters["isOpen"]);
            }

            // Set GripperEmpty property
            if (parameters.ContainsKey("gripperEmpty"))
            {
                GripperEmpty = Convert.ToBoolean(parameters["gripperEmpty"]);
            }

            // Set Loc property
            if (parameters.ContainsKey("loc"))
            {
                if (parameters["loc"] is Location locValue)
                {
                    Loc = locValue;
                }
            }

        }
    }
}

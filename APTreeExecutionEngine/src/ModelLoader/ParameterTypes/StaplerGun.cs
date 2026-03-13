using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class StaplerGun : Tool
    {
        public bool ReadyToFire { get; set; }
        public Location Loc { get; set; }

        // Empty constructor - required by CustomProperty
        public StaplerGun() : base()
        {
            BaseType = new FastName("Tool");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public StaplerGun(bool readyToFire, Location loc) : this()
        {
            this.ReadyToFire = readyToFire;
            this.Loc = loc;
        }

        // Constructor with name and parameters
        public StaplerGun(string name, bool readyToFire, Location loc) : base(name)
        {
            this.ReadyToFire = readyToFire;
            this.Loc = loc;
            BaseType = new FastName("Tool");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set StaplerGun-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

            // Set ReadyToFire property
            if (parameters.ContainsKey("readyToFire"))
            {
                ReadyToFire = Convert.ToBoolean(parameters["readyToFire"]);
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

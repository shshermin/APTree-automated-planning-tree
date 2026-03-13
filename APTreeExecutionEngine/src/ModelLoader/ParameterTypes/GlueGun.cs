using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class GlueGun : Tool
    {
        public Location Loc { get; set; }
        public bool IsActive { get; set; }

        // Empty constructor - required by CustomProperty
        public GlueGun() : base()
        {
            BaseType = new FastName("Tool");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public GlueGun(Location loc, bool isActive) : this()
        {
            this.Loc = loc;
            this.IsActive = isActive;
        }

        // Constructor with name and parameters
        public GlueGun(string name, Location loc, bool isActive) : base(name)
        {
            this.Loc = loc;
            this.IsActive = isActive;
            BaseType = new FastName("Tool");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set GlueGun-specific properties
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

            // Set IsActive property
            if (parameters.ContainsKey("isActive"))
            {
                IsActive = Convert.ToBoolean(parameters["isActive"]);
            }

        }
    }
}

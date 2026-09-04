using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class Beam : Element
    {
        public Location Loc { get; set; }

        // Empty constructor - required by CustomProperty
        public Beam() : base()
        {
            BaseType = new FastName("Element");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public Beam(Location loc) : this()
        {
            this.Loc = loc;
        }

        // Constructor with name and parameters
        public Beam(string name, Location loc) : base(name)
        {
            this.Loc = loc;
            BaseType = new FastName("Element");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set Beam-specific properties
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

        }
    }
}

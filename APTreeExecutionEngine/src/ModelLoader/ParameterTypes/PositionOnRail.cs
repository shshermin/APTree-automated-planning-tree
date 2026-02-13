using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class PositionOnRail : Location
    {

        // Empty constructor - required by Entity
        public PositionOnRail() : base()
        {
            BaseType = new FastName("Location");
            // TypeName is automatically set in base constructor
        }


        // Override SetParameters to set PositionOnRail-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

        }
    }
}

using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class FirstPos : Location
    {

        // Empty constructor - required by Entity
        public FirstPos() : base()
        {
            BaseType = new FastName("Location");
            // TypeName is automatically set in base constructor
        }


        // Override SetParameters to set FirstPos-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

        }
    }
}

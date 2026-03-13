using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class Demo : Module
    {

        // Empty constructor - required by CustomProperty
        public Demo() : base()
        {
            BaseType = new FastName("Module");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set Demo-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

        }
    }
}

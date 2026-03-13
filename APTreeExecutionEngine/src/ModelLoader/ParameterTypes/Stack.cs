using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class Stack : Layer
    {

        // Empty constructor - required by CustomProperty
        public Stack() : base()
        {
            BaseType = new FastName("Layer");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set Stack-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

        }
    }
}

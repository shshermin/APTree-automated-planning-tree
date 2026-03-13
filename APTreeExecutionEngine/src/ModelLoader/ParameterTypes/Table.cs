using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class Table : Element
    {

        // Empty constructor - required by CustomProperty
        public Table() : base()
        {
            BaseType = new FastName("Element");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set Table-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

        }
    }
}

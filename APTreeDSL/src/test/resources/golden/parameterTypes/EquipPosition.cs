using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class EquipPosition : Location
    {

        // Empty constructor - required by CustomProperty
        public EquipPosition() : base()
        {
            BaseType = new FastName("Location");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set EquipPosition-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

        }
    }
}

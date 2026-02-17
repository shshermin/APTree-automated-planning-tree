using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class VacGripper : Tool
    {
        public string IsOn { get; set; }

        // Empty constructor - required by CustomProperty
        public VacGripper() : base()
        {
            BaseType = new FastName("Tool");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public VacGripper(string isOn) : this()
        {
            this.IsOn = isOn;
        }

        // Constructor with name and parameters
        public VacGripper(string name, string isOn) : base(name)
        {
            this.IsOn = isOn;
            BaseType = new FastName("Tool");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set VacGripper-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

            // Set IsOn property
            if (parameters.ContainsKey("isOn"))
            {
                IsOn = parameters["isOn"].ToString();
            }

        }
    }
}

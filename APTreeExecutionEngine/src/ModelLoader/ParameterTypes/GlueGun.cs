using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class GlueGun : Tool
    {
        public string IsOn { get; set; }

        // Empty constructor - required by Entity
        public GlueGun() : base()
        {
            BaseType = new FastName("Tool");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public GlueGun(string isOn) : this()
        {
            this.IsOn = isOn;
        }

        // Constructor with name and parameters
        public GlueGun(string name, string isOn) : base(name)
        {
            this.IsOn = isOn;
            BaseType = new FastName("Tool");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set GlueGun-specific properties
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

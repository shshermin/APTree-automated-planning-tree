using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class Cassette : Module
    {
        public string Layers { get; set; }

        // Empty constructor - required by Entity
        public Cassette() : base()
        {
            BaseType = new FastName("Module");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public Cassette(string layers) : this()
        {
            this.Layers = layers;
        }

        // Constructor with name and parameters
        public Cassette(string name, string layers) : base(name)
        {
            this.Layers = layers;
            BaseType = new FastName("Module");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set Cassette-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

            // Set Layers property
            if (parameters.ContainsKey("layers"))
            {
                Layers = parameters["layers"].ToString();
            }

        }
    }
}

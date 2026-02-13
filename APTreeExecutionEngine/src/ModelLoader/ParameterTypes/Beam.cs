using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class Beam : Element
    {
        public double Length { get; set; }

        // Empty constructor - required by Entity
        public Beam() : base()
        {
            BaseType = new FastName("Element");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public Beam(double length) : this()
        {
            this.Length = length;
        }

        // Constructor with name and parameters
        public Beam(string name, double length) : base(name)
        {
            this.Length = length;
            BaseType = new FastName("Element");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set Beam-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

            // Set Length property
            if (parameters.ContainsKey("length"))
            {
                Length = Convert.ToDouble(parameters["length"]);
            }

        }
    }
}

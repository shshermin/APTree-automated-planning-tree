using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class Plate : Element
    {
        public double Thickness { get; set; }
        public string Material { get; set; }

        // Empty constructor - required by Entity
        public Plate() : base()
        {
            BaseType = new FastName("Element");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public Plate(double thickness, string material) : this()
        {
            this.Thickness = thickness;
            this.Material = material;
        }

        // Constructor with name and parameters
        public Plate(string name, double thickness, string material) : base(name)
        {
            this.Thickness = thickness;
            this.Material = material;
            BaseType = new FastName("Element");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set Plate-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

            // Set Thickness property
            if (parameters.ContainsKey("thickness"))
            {
                Thickness = Convert.ToDouble(parameters["thickness"]);
            }

            // Set Material property
            if (parameters.ContainsKey("material"))
            {
                Material = parameters["material"].ToString();
            }

        }
    }
}

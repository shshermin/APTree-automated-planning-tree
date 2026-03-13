using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class Cube : Element
    {
        public Location InitLoc { get; set; }
        public Location FinalLoc { get; set; }

        // Empty constructor - required by CustomProperty
        public Cube() : base()
        {
            BaseType = new FastName("Element");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public Cube(Location initLoc, Location finalLoc) : this()
        {
            this.InitLoc = initLoc;
            this.FinalLoc = finalLoc;
        }

        // Constructor with name and parameters
        public Cube(string name, Location initLoc, Location finalLoc) : base(name)
        {
            this.InitLoc = initLoc;
            this.FinalLoc = finalLoc;
            BaseType = new FastName("Element");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set Cube-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

            // Set InitLoc property
            if (parameters.ContainsKey("initLoc"))
            {
                if (parameters["initLoc"] is Location initLocValue)
                {
                    InitLoc = initLocValue;
                }
            }

            // Set FinalLoc property
            if (parameters.ContainsKey("finalLoc"))
            {
                if (parameters["finalLoc"] is Location finalLocValue)
                {
                    FinalLoc = finalLocValue;
                }
            }

        }
    }
}

using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class FinalLocation : Location
    {
        public Coordinate Position { get; set; }
        public Coordinate Orientation { get; set; }

        // Empty constructor - required by CustomProperty
        public FinalLocation() : base()
        {
            BaseType = new FastName("Location");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public FinalLocation(Coordinate position, Coordinate orientation) : this()
        {
            this.Position = position;
            this.Orientation = orientation;
        }

        // Constructor with name and parameters
        public FinalLocation(string name, Coordinate position, Coordinate orientation) : base(name)
        {
            this.Position = position;
            this.Orientation = orientation;
            BaseType = new FastName("Location");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set FinalLocation-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

            // Set Position property
            if (parameters.ContainsKey("position"))
            {
                if (parameters["position"] is Coordinate positionValue)
                    Position = positionValue;
                else if (parameters["position"] is string posStr)
                    Position = Coordinate.Parse(posStr);
            }

            // Set Orientation property
            if (parameters.ContainsKey("orientation"))
            {
                if (parameters["orientation"] is Coordinate orientationValue)
                    Orientation = orientationValue;
                else if (parameters["orientation"] is string oriStr)
                    Orientation = Coordinate.Parse(oriStr);
            }

        }
    }
}

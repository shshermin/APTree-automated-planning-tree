using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class NailLocation : Location
    {
        public Coordinate Position { get; set; }
        public Coordinate Orientation { get; set; }

        // Empty constructor - required by CustomProperty
        public NailLocation() : base()
        {
            BaseType = new FastName("Location");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public NailLocation(Coordinate position, Coordinate orientation) : this()
        {
            this.Position = position;
            this.Orientation = orientation;
        }

        // Constructor with name and parameters
        public NailLocation(string name, Coordinate position, Coordinate orientation) : base(name)
        {
            this.Position = position;
            this.Orientation = orientation;
            BaseType = new FastName("Location");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set NailLocation-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

            // Set Position property
            if (parameters.ContainsKey("position"))
            {
                if (parameters["position"] is Coordinate positionValue)
                    Position = positionValue;
                else if (parameters["position"] is string positionStr)
                    Position = Coordinate.Parse(positionStr);
            }

            // Set Orientation property
            if (parameters.ContainsKey("orientation"))
            {
                if (parameters["orientation"] is Coordinate orientationValue)
                    Orientation = orientationValue;
                else if (parameters["orientation"] is string orientationStr)
                    Orientation = Coordinate.Parse(orientationStr);
            }

        }
    }
}

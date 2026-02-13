using System;
using System.Collections.Generic;

namespace ModelLoader.ParameterTypes
{
    public class Stack : Layer
    {
        public int Level { get; set; }
        public Module BelongsToModule { get; set; }

        // Empty constructor - required by Entity
        public Stack() : base()
        {
            BaseType = new FastName("Layer");
            // TypeName is automatically set in base constructor
        }

        // Constructor with parameters
        public Stack(int level, Module belongsToModule) : this()
        {
            this.Level = level;
            this.BelongsToModule = belongsToModule;
        }

        // Constructor with name and parameters
        public Stack(string name, int level, Module belongsToModule) : base(name)
        {
            this.Level = level;
            this.BelongsToModule = belongsToModule;
            BaseType = new FastName("Layer");
            // TypeName is automatically set in base constructor
        }

        // Override SetParameters to set Stack-specific properties
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            // Call base implementation first
            base.SetParameters(parameters);

            // Set Level property
            if (parameters.ContainsKey("level"))
            {
                Level = Convert.ToInt32(parameters["level"]);
            }

            // Set BelongsToModule property
            if (parameters.ContainsKey("belongsToModule"))
            {
                if (parameters["belongsToModule"] is Module belongsToModuleValue)
                {
                    BelongsToModule = belongsToModuleValue;
                }
            }

        }
    }
}

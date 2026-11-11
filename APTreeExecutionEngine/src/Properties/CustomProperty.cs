using System;
using System.Collections.Generic;

public abstract class CustomProperty 
{
   /// <summary>
   /// Instance name (PDDL object name). Declared only here: the base types used to
   /// redeclare it, which gave every object two NameKeys that could disagree.
   /// </summary>
   public FastName NameKey { get;  set; }
   public DateTime LastModified { get;  set; }
   public string ID { get; set; }
   public FastName TypeName { get; set; }
   public abstract FastName BaseType { get; set; }

       // Protected empty constructor - forces all derived classes to have one
    protected CustomProperty() 
    {
        LastModified = DateTime.Now;
        // Automatically set TypeName to the actual class name
        TypeName = new FastName(GetType().Name);
    }

   // Optional: Constructor that takes a name
   protected CustomProperty(string name) : this()
   {
       NameKey = new FastName(name);
       ID = name;
   }

   /// <summary>
   /// Abstract method to set parameter values from a dictionary
   /// </summary>
   /// <param name="parameters">Dictionary of parameter names and their values</param>
   public abstract void SetParameters(Dictionary<string, object> parameters);
}

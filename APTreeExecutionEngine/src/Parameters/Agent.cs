using System;

public class Agent : Entity
{
    public FastName NameKey { get; set; }
    public DateTime LastModified { get; set; }
    public string ID { get; set; }
    public FastName TypeName { get; set; }
    public override FastName BaseType { get; set; }

    // Tool attribute for the agent
    public Tool Tool { get; set; }

    // Empty constructor - required by Entity
    public Agent() : base()
    {
        BaseType = new FastName("Agent");
        TypeName = new FastName("Agent");
    }

    public Agent(string InName) : base(InName)
    {
        BaseType = new FastName("Agent");
        TypeName = new FastName("Agent");
    }

    // Constructor with tool
    public Agent(string InName, Tool tool) : this(InName)
    {
        Tool = tool;
    }
    
     public override void SetParameters(Dictionary<string, object> parameters)
    {
        // Location base class doesn't have specific parameters to set
        // Derived classes will override this method to set their specific properties
    }
}
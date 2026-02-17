using System;

public class Layer : CustomProperty
{
	public FastName NameKey { get; set; }
	public DateTime LastModified { get; set; }
	public string ID { get; set; }
	public FastName TypeName { get; set; }
	public override FastName BaseType { get; set; }

	/// <summary>
	/// Each element should have a nameid. the name id should comply with PDDL naming conventions
	/// </summary>

	private Module module;

	// Empty constructor - required by CustomProperty
	public Layer() : base()
	{
		BaseType = new FastName("Layer");
		TypeName = new FastName("Layer");
	}

	public Layer(string InName) : base(InName)
	{
		BaseType = new FastName("Layer");
		TypeName = new FastName("Layer");
	}
	 public override void SetParameters(Dictionary<string, object> parameters)
    {
        // Location base class doesn't have specific parameters to set
        // Derived classes will override this method to set their specific properties
    }
}

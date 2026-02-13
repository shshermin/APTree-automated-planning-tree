using System;

public class Module : Entity
{
	public FastName NameKey { get; set; }
	public DateTime LastModified { get; set; }
	public string ID { get; set; }
	public FastName TypeName { get; set; }
	public override FastName BaseType { get; set; }
	/// <summary>
	/// Each element should have a nameid. the name id should comply with PDDL naming conventions
	/// </summary>
	public readonly string NameID;

	private Layer layer;

	// Empty constructor - required by Entity
	public Module() : base()
	{
		BaseType = new FastName("Module");
		TypeName = new FastName("Module");
	}

	public Module(string InName) : base(InName)
	{
		BaseType = new FastName("Module");
		TypeName = new FastName("Module");
	}
	 public override void SetParameters(Dictionary<string, object> parameters)
    {
        // Location base class doesn't have specific parameters to set
        // Derived classes will override this method to set their specific properties
    }
}

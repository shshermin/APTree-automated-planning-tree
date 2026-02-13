using System;
using System.Collections.Generic;

public abstract class Location : Entity  {
    public FastName NameKey { get; set; }
    public DateTime LastModified { get; set; }
    public string ID { get; set; }
    public FastName TypeName { get; set; }
    public override FastName BaseType { get; set; }
    public Coordinate coordinate { get; set; }
    public BoundingBox bbox { get; set; }

    // Empty constructor - required by Entity
    protected Location() : base()
    {
        BaseType = new FastName("Location");
        TypeName = new FastName("Location");
    }

    public Location(string InName) : base(InName)
    {
        BaseType = new FastName("Location");
        TypeName = new FastName("Location");
    }

    // Implement the abstract SetParameters method
    public override void SetParameters(Dictionary<string, object> parameters)
    {
        // Location base class doesn't have specific parameters to set
        // Derived classes will override this method to set their specific properties
    }
}

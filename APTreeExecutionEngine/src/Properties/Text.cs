using System;

public class Text : CustomProperty
{
    public FastName NameKey { get; set; }
    public DateTime LastModified { get; set; }
    public string ID { get; set; }
    public FastName TypeName { get; set; }
    public override FastName BaseType { get; set; }

    // Empty constructor - required by CustomProperty
    public Text() : base()
    {
        BaseType = new FastName("Text");
        TypeName = new FastName("Text");
    }

    public Text(string InName) : base(InName)
    {
        BaseType = new FastName("Text");
        TypeName = new FastName("Text");
        NameKey = new FastName(InName);
        ID = InName;
    }

    public override void SetParameters(Dictionary<string, object> parameters)
    {
        // Text doesn't have specific parameters to set
    }
}

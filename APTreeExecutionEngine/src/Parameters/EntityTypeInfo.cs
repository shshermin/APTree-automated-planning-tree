public class EntityTypeInfo
{
    public FastName TypeName { get; set; }
    public Type BaseType { get; set; }
    public Dictionary<string, Type> Properties { get; set; }

    public EntityTypeInfo(Type baseType, Dictionary<string, Type> properties)
    {
        BaseType = baseType;
        Properties = properties;
    }
}
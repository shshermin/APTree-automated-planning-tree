/// <summary>
/// Interface for LL actions that accept typed inputs from the parent ML action.
/// The DecoratorLLInputResolver passes each resolved ML object to BindInput().
/// The LL action matches by type and assigns to its internal properties.
/// Objects of unrecognized types are silently ignored.
/// </summary>
public interface ILLInputBindable
{
    /// <summary>
    /// Receives a single resolved ML parameter object. The implementation should
    /// match by type (e.g. RobotPosition, Location, Tool) and assign to the
    /// appropriate internal property. Unknown types are ignored.
    /// </summary>
    void BindInput(object value);
}

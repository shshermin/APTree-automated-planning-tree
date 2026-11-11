using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

/// <summary>
/// Regression: Element, Location, Agent, Tool, Layer, Module and Text redeclared
/// NameKey, so an object had two NameKeys and code reading it as CustomProperty
/// saw a different (usually null) value than code reading it as Element.
/// </summary>
public class CustomPropertyNameKeyTests
{
    [Fact]
    public void ANameKeySetThroughTheConcreteType_IsSeenThroughCustomProperty()
    {
        var beam = new Beam { NameKey = new FastName("beam1") };

        Assert.Equal("beam1", ((CustomProperty)beam).NameKey?.ToString());
    }

    [Fact]
    public void ANameKeySetThroughCustomProperty_IsSeenThroughTheBaseType()
    {
        var robot = new Robot();
        ((CustomProperty)robot).NameKey = new FastName("r1");

        Assert.Equal("r1", ((Agent)robot).NameKey?.ToString());
    }

    [Fact]
    public void FormattedPredicates_ContainTheirParameterNames()
    {
        var p1 = new FirstPos { NameKey = new FastName("p1") };
        var beam = new Beam { NameKey = new FastName("beam1"), Loc = p1 };

        string formatted = BlackboardExtensions.FormatPredicate(new AtPlace(beam, p1, false));

        Assert.Contains("beam1", formatted);
        Assert.Contains("p1", formatted);
    }
}

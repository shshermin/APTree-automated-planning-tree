/// <summary>
/// Determines which UR10 move primitive (or external planner) is used by MoveToLL.
/// </summary>
public enum MoveType
{
    /// <summary>Joint-space interpolation — fast, curved TCP path. URScript: movej</summary>
    MoveJ,

    /// <summary>Linear TCP interpolation — straight-line Cartesian path. URScript: movel</summary>
    MoveL,

    /// <summary>Process/blend move — continuous path, no stops at waypoints. URScript: movep</summary>
    MoveP,

    /// <summary>Circular arc move — requires a via-point. URScript: movec</summary>
    MoveC,

    /// <summary>Call an external motion planner to compute and execute a trajectory (generic, defaults to joint-space PTP).</summary>
    Planned,

    /// <summary>MoveIt-planned joint-space motion (Pilz PTP). Executed via URScript movej chain.</summary>
    PlannedJ,

    /// <summary>MoveIt-planned linear Cartesian motion (Pilz LIN). Executed via URScript movej chain.</summary>
    PlannedL
}
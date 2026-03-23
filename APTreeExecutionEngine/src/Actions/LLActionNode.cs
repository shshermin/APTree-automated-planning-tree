using System;
using System.Collections.Generic;
using BehaviorTreeMainProject;
using BehaviorTreeMainProject.Log.Services;

/// <summary>
/// A low-level (LL) action node representing a single robot primitive
/// (e.g. MoveTo, OpenGripper, CloseGripper, Lift, Lower, etc.).
/// Created by ServiceLLSubtreeInject when expanding ML actions into LL subtrees.
///
/// LL actions have no PDDL preconditions/effects — they are purely execution-level.
/// </summary>
public class LLActionNode : PActionNode
{
    /// <summary>The LL primitive name (e.g. "MoveTo", "OpenGripper").</summary>
    public string LLActionName { get; }

    /// <summary>Resolved parameters for this step (e.g. target=beam1, robot=robot1).</summary>
    public Dictionary<string, string> ResolvedParameters { get; }

    // Empty states — LL nodes have no PDDL preconditions/effects
    private readonly State _emptyPreconditions;
    private readonly State _emptyEffects;

    protected override State Preconditions => _emptyPreconditions;
    protected override State Effects => _emptyEffects;

    public LLActionNode(
        FastName stepName,
        string llActionName,
        Dictionary<string, string> resolvedParams,
        Blackboard<FastName> blackboard
    ) : base(llActionName, stepName.ToString(), blackboard)
    {
        LLActionName = llActionName;
        ResolvedParameters = resolvedParams ?? new Dictionary<string, string>();

        _emptyPreconditions = new State(StateType.Precondition, new FastName($"{stepName}_pre"));
        _emptyEffects = new State(StateType.Effect, new FastName($"{stepName}_eff"));

        LoggingService.LogInfo($"🔧 LLActionNode: Created '{llActionName}' step '{stepName}' with {ResolvedParameters.Count} params");
    }
}

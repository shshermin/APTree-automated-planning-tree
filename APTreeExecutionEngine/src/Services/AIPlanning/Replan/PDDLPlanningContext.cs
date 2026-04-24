using System.Collections.Generic;
using AIPlanning;

namespace BehaviorTreeMainProject.Services.AIPlanning.Replan
{
    /// <summary>
    /// Mutable context passed through the replan decorator pipeline.
    ///
    /// Producers (decorators) populate <see cref="ExtraObjects"/>, adjust
    /// <see cref="PlanningRequest"/>, or flip the short-circuit flags.
    /// Consumers (the service) then invoke the planner.
    /// </summary>
    public class PDDLPlanningContext
    {
        /// <summary>The owning planning service (useful for decorators that
        /// want to call helper methods on it).</summary>
        public ServicePDDLPlanning Service { get; internal set; }

        /// <summary>The owning DynamicFlowNode (may be null if not a dynamic node).</summary>
        public DynamicFlowNode FlowNode { get; internal set; }

        /// <summary>Parent HL action this ML plan is implementing.
        /// Null when this service is planning at the HL (cross-cassette) level.</summary>
        public PActionNode ParentAction { get; internal set; }

        /// <summary>Runtime blackboard (shared with the owning tree).</summary>
        public Blackboard<FastName> Blackboard { get; internal set; }

        /// <summary>The request object the planner will be invoked with.
        /// Decorators may mutate this (e.g. swap ProblemFile, rewrite ProblemFileContent).</summary>
        public PDDLPlanningRequest PlanningRequest { get; internal set; }

        /// <summary>
        /// The ORIGINAL (static) problem file path — captured at service
        /// construction time. Never points at a previously-generated file.
        /// Decorators that regenerate the problem file should use this as
        /// the parent source.
        /// </summary>
        public string OriginalProblemFile { get; internal set; }

        /// <summary>
        /// Extra PDDL object declarations (name → PDDL type) that should be
        /// appended to the (:objects) block on regeneration. Populated by
        /// decorators like <c>DecoratorExtraPDDLObjects</c>. Consumed by
        /// <c>DecoratorRegenerateFromBlackboard</c>.
        /// </summary>
        public Dictionary<string, string> ExtraObjects { get; }
            = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>True when a decorator has already written a fresh
        /// problem file this tick. Prevents double-regeneration.</summary>
        public bool ProblemFileRegenerated { get; set; }
    }
}

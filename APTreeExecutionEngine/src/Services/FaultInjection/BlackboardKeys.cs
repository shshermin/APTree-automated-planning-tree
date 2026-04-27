namespace BehaviorTreeMainProject.Services.FaultInjection
{
    /// <summary>
    /// Central registry of blackboard key naming conventions shared across
    /// sensors, services, and decorators.
    ///
    /// Rules:
    ///  • Never construct these key names inline — always call the helpers here
    ///    so the convention is enforced in one place.
    ///  • Nothing in this class knows about any specific sensor, service, or decorator.
    /// </summary>
    public static class BlackboardKeys
    {
        /// <summary>
        /// Key written (true) by a sensor/camera service when it detects a fault
        /// on the ML flow node whose DebugDisplayName is <paramref name="flowNodeName"/>.
        /// Read and cleared by <see cref="DecoratorFaultAbort"/> at end-of-tick.
        /// </summary>
        public static FastName AbortKey(string flowNodeName)
            => new FastName($"abort_{flowNodeName}");

        /// <summary>
        /// Key written (string = DateTime.Now.Ticks.ToString()) by
        /// <see cref="WorldStateManager"/> at the moment a fault fires.
        /// Used by <see cref="DecoratorFaultAbort"/> and
        /// <see cref="BehaviorTreeMainProject.Services.AIPlanning.ServicePDDLPlanning"/>
        /// to compute Recovery Time and log absolute t_fault / t_resume timestamps.
        /// Cleared after the "resumed" log is emitted.
        /// </summary>
        public static FastName FaultTimestampKey(string flowNodeName)
            => new FastName($"fault_time_{flowNodeName}");

        /// <summary>
        /// Key written (true) by fault injection when a previously completed HL action's
        /// effects have been invalidated (e.g. a stacked element was dislodged).
        /// Read and cleared by <see cref="BehaviorTreeMainProject.Decorators.Replan.DecoratorHLFaultReplan"/>
        /// at the start of the next planning cycle to force a full HL replan from
        /// the current blackboard state while preserving :objects and :goal.
        /// </summary>
        public static FastName HLReplanKey(string flowNodeName)
            => new FastName($"hl_replan_{flowNodeName}");
    }
}

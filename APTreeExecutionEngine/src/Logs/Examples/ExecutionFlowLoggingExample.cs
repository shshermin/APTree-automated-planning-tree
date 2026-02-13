using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject.Log.Examples
{
    /// <summary>
    /// Example demonstrating how to use the ExecutionFlowLogger
    /// This shows the different types of logging available for tracking execution flow
    /// </summary>
    public static class ExecutionFlowLoggingExample
    {
        public static void RunExample()
        {
            // Initialize the execution flow logger
            ExecutionFlowLogger.Initialize("Example", enableConsole: true, enableFile: true);
            
            // Example of logging different types of events
            LogExampleEvents();
            
            // Close the logger and generate statistics
            ExecutionFlowLogger.Close();
            
            Console.WriteLine($"📁 Execution flow log saved to: {ExecutionFlowLogger.GetLogFilePath()}");
        }
        
        private static void LogExampleEvents()
        {
            // Log node ticks
            ExecutionFlowLogger.LogNodeTick("RootComposite", "BTFlowNode_Composite", "GeneralServices", "InProgress");
            ExecutionFlowLogger.LogNodeTick("Cassette1", "BTFlowNode_Dynamic", "NodeLogic", "Succeeded");
            ExecutionFlowLogger.LogNodeTick("TravelML", "GenericBTAction", "Children", "InProgress");
            
            // Log service ticks
            ExecutionFlowLogger.LogServiceTick("PlanningPhaseManager", "BTService_PlanningPhaseManager", "RootComposite", "SUCCESS");
            ExecutionFlowLogger.LogServiceTick("PDDLPlanner", "CallPDDLPlanner", "Cassette1", "SUCCESS");
            ExecutionFlowLogger.LogServiceTick("SubtreeInjectionService", "SubtreeInjectionService", "TravelML", "SUCCESS");
            
            // Log decorator ticks
            ExecutionFlowLogger.LogDecoratorTick("PlanningComplete", "BTDecorator_PlanningComplete", "TravelML", "BLOCK");
            ExecutionFlowLogger.LogDecoratorTick("PlanningComplete", "BTDecorator_PlanningComplete", "TravelML", "ALLOW");
            
            // Log phase transitions
            ExecutionFlowLogger.LogPhaseTransition("RootComposite", "AlwaysOnServices", "GeneralServices");
            ExecutionFlowLogger.LogPhaseTransition("RootComposite", "GeneralServices", "Decorators");
            ExecutionFlowLogger.LogPhaseTransition("RootComposite", "Decorators", "NodeLogic");
            
            // Log planning events
            ExecutionFlowLogger.LogPlanningEvent("SERVICE_START", "PDDLPlanner started for Cassette1");
            ExecutionFlowLogger.LogPlanningEvent("NODEGRAPH_GENERATED", "Generated 5 actions for Cassette1");
            ExecutionFlowLogger.LogPlanningEvent("PHASE_COMPLETE", "All planning services finished");
            
            // Log execution events
            ExecutionFlowLogger.LogExecutionEvent("PHASE_START", "ML actions can now execute");
            ExecutionFlowLogger.LogExecutionEvent("ACTION_START", "TravelML_r1_ng1 started execution");
            ExecutionFlowLogger.LogExecutionEvent("ACTION_COMPLETE", "TravelML_r1_ng1 completed successfully");
            
            // Log separators for better readability
            ExecutionFlowLogger.LogSeparator();
            ExecutionFlowLogger.LogHeader("📋 PLANNING PHASE COMPLETED");
            ExecutionFlowLogger.LogSeparator();
        }
    }
}

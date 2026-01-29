using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDataStructures;
using AIPlanning;
using BehaviorTreeMainProject.Services;
using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject.Services.AIPlanning
{
    public class CallPDDLPlanner : BTServicePlanner
    {
        private DateTime planningStartTime;
        private bool planningStarted = false;

        private readonly Blackboard<FastName> blackboard;
        private readonly FactoryAction actionFactory;
        public FastName PlannerName = new FastName("PDDLPlanner");
        public List<PActionNode> TempActionList = new List<PActionNode>();
        public PDDLPlanningRequest PlanningRequest;
        
        // Parallel execution configuration
        public enum ParallelExecutionMode
        {
            Sequential,      // All actions run sequentially (MEETS)
            Parallel,        // Actions run in parallel (OVERLAPS)
            Hybrid           // Mix of sequential and parallel
        }
        
        public ParallelExecutionMode ExecutionMode { get; set; } = ParallelExecutionMode.Sequential;

        public CallPDDLPlanner(BehaviorTree InOwningTree, PDDLPlanningRequest InPlanningRequest)
            : base(InOwningTree, new RestPlannerCommunicator("http://localhost:5000"), InPlanningRequest)
        {
            this.blackboard = InOwningTree.linkedBlackboard;
            this.actionFactory = FactoryAction.Instance;
            this.PlanningRequest = InPlanningRequest;
        }
      

        public override bool OnEvaluate(float InDeltaTime)
        {
            if (!planningStarted)
            {
                planningStartTime = DateTime.Now;
                planningStarted = true;
            }
            
            return base.OnEvaluate(InDeltaTime);
        }

        protected override NodeGraph GenerateNodeGraphFromResult(PlanningResult result)
        {
            var endTime = DateTime.Now;
            bool success = result.Success;
            int actionsGenerated = 0;
            NodeGraph nodeGraph = null;

            LoggingService.LogInfo($"🔧 CallPDDLPlanner: Converting PDDL result to NodeGraph...");
            LoggingService.LogInfo($"📋 CallPDDLPlanner: Execution Mode: {ExecutionMode}");
            LoggingService.LogInfo($"📋 CallPDDLPlanner: Problem File: {PlanningRequest.ProblemFile}");
            
            try
            {
                if (string.IsNullOrEmpty(result.Plan))
                {
                    LoggingService.LogWarning("⚠️ CallPDDLPlanner: No plan in planning result");
                    success = false;
                }
                else
                {
                    // Parse the plan string and create NodeGraph
                    nodeGraph = ParsePlanStringToNodeGraph(result.Plan);
                    
                    if (nodeGraph != null)
                    {
                        actionsGenerated = nodeGraph.GetAllActionNodes().Count;
                        LoggingService.LogSuccess($"✅ CallPDDLPlanner: Generated NodeGraph with {actionsGenerated} actions");
                        LoggingService.LogSuccess($"✅ CallPDDLPlanner: Execution Mode applied: {ExecutionMode}");
                    }
                    else
                    {
                        success = false;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ CallPDDLPlanner: Error generating NodeGraph: {ex.Message}");
                success = false;
            }

            return success ? nodeGraph : null;
        }
        
        /// <summary>
        /// Parses planner output string and converts it to a NodeGraph
        /// </summary>
        /// <param name="planString">Raw planner output string</param>
        /// <returns>A populated NodeGraph instance</returns>
        private NodeGraph ParsePlanStringToNodeGraph(string planString)
        {
            LoggingService.LogInfo($"🔧 CallPDDLPlanner: ParsePlanStringToNodeGraph called");
            LoggingService.LogInfo($"🔧 CallPDDLPlanner: Plan string length: {planString?.Length ?? 0}");
            
            if (string.IsNullOrEmpty(planString))
            {
                LoggingService.LogError($"❌ CallPDDLPlanner: Plan string is null or empty");
                return new NodeGraph();
            }
            
            try
            {
                // Step 1: Parse planner output to extract action instances and relations
                var (actionInstances, relations) = Parser.ParsePlannerOutput(planString);
                
                LoggingService.LogInfo($"🔧 CallPDDLPlanner: Extracted {actionInstances.Count} action instances and {relations.Count} relations");
                
                // Step 2: Create NodeGraph from the extracted data
                var nodeGraph = Parser.ParseNodeGraph(actionInstances, relations, blackboard);
                
                LoggingService.LogSuccess($"✅ CallPDDLPlanner: Successfully created NodeGraph with {nodeGraph.GetAllActionNodes().Count} nodes");
                return nodeGraph;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ CallPDDLPlanner: Exception in ParsePlanStringToNodeGraph: {ex.Message}");
                LoggingService.LogError($"❌ CallPDDLPlanner: Stack trace: {ex.StackTrace}");
                return new NodeGraph();
            }
        }
        

        
        private NodeGraph CreateNodeGraphWithExecutionMode(List<PActionNode> actions)
        {
            var nodeGraph = new NodeGraph();
            
            // Add all actions to the NodeGraph
            foreach (var action in actions)
            {
                nodeGraph.AddNode(action);
            }
            
            if (actions.Count == 0) return nodeGraph;
            
            LoggingService.LogInfo($"🔧 CallPDDLPlanner: Creating NodeGraph with {ExecutionMode} execution mode for {actions.Count} actions");
            
            switch (ExecutionMode)
            {
                case ParallelExecutionMode.Sequential:
                    return CreateSequentialNodeGraph(actions, nodeGraph);
                    
                case ParallelExecutionMode.Parallel:
                    return CreateParallelNodeGraph(actions, nodeGraph);
                    
                case ParallelExecutionMode.Hybrid:
                    return CreateHybridNodeGraph(actions, nodeGraph);
                    
                default:
                    return CreateParallelNodeGraph(actions, nodeGraph);
            }
        }
        
        private NodeGraph CreateSequentialNodeGraph(List<PActionNode> actions, NodeGraph nodeGraph)
        {
            LoggingService.LogInfo($"🔧 CallPDDLPlanner: Creating sequential execution pattern");
            
            // Add sequential relations (MEETS constraints) between consecutive actions
            for (int i = 0; i < actions.Count - 1; i++)
            {
                nodeGraph.AddOrderRelation(actions[i], actions[i + 1]);
                nodeGraph.AddTemporalConstraint(actions[i], actions[i + 1], TemporalConstraint.MEETS);
                LoggingService.LogInfo($"🔧 CallPDDLPlanner: Added sequential relation: {actions[i].InstanceName} → {actions[i + 1].InstanceName}");
            }
            
            return nodeGraph;
        }
        
        private NodeGraph CreateParallelNodeGraph(List<PActionNode> actions, NodeGraph nodeGraph)
        {
            LoggingService.LogInfo($"🔧 CallPDDLPlanner: Creating parallel execution pattern");
            
            if (actions.Count == 1)
            {
                LoggingService.LogInfo($"🔧 CallPDDLPlanner: Single action execution");
                return nodeGraph;
            }
            
            // First action starts, then all others run in parallel
            for (int i = 1; i < actions.Count; i++)
            {
                nodeGraph.AddOrderRelation(actions[0], actions[i]);
                nodeGraph.AddTemporalConstraint(actions[0], actions[i], TemporalConstraint.OVERLAPS);
                LoggingService.LogInfo($"🔧 CallPDDLPlanner: Added parallel relation: {actions[0].InstanceName} || {actions[i].InstanceName}");
            }
            
            return nodeGraph;
        }
        
        private NodeGraph CreateHybridNodeGraph(List<PActionNode> actions, NodeGraph nodeGraph)
        {
            LoggingService.LogInfo($"🔧 CallPDDLPlanner: Creating hybrid execution pattern");
            
            if (actions.Count <= 2)
            {
                return CreateParallelNodeGraph(actions, nodeGraph);
            }
            
            // Hybrid pattern: First action sequential, then parallel groups
            // Group 1: First action
            // Group 2: Actions 2-3 run in parallel
            // Group 3: Actions 4+ run in parallel after group 2
            
            // First action to second action (sequential)
            nodeGraph.AddOrderRelation(actions[0], actions[1]);
            nodeGraph.AddTemporalConstraint(actions[0], actions[1], TemporalConstraint.MEETS);
            LoggingService.LogInfo($"🔧 CallPDDLPlanner: Added sequential relation: {actions[0].InstanceName} → {actions[1].InstanceName}");
            
            // Second action to third action (parallel)
            if (actions.Count > 2)
            {
                nodeGraph.AddOrderRelation(actions[1], actions[2]);
                nodeGraph.AddTemporalConstraint(actions[1], actions[2], TemporalConstraint.OVERLAPS);
                LoggingService.LogInfo($"🔧 CallPDDLPlanner: Added parallel relation: {actions[1].InstanceName} || {actions[2].InstanceName}");
            }
            
            // Remaining actions in parallel
            for (int i = 3; i < actions.Count; i++)
            {
                nodeGraph.AddOrderRelation(actions[1], actions[i]);
                nodeGraph.AddTemporalConstraint(actions[1], actions[i], TemporalConstraint.OVERLAPS);
                LoggingService.LogInfo($"🔧 CallPDDLPlanner: Added parallel relation: {actions[1].InstanceName} || {actions[i].InstanceName}");
            }
            
            return nodeGraph;
        }
    }
}

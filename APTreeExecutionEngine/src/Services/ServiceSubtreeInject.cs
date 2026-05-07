using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using AIPlanning;
using BehaviorTreeMainProject.Services.AIPlanning;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject
{
  
    public class ServiceSubtreeInject : Service
    {
        private static readonly Dictionary<string, SubtreeConfiguration> subtreeConfigurations = new Dictionary<string, SubtreeConfiguration>();
        private static readonly Dictionary<string, DynamicFlowNode> cachedSubtrees = new Dictionary<string, DynamicFlowNode>();
        private static bool _defaultConfigsInitialized = false;

        /// <summary>
        /// The config name used by ProcessSubtreeInjection for ML-level subtrees.
        /// Defaults to "FF_Default". Override before running the tree to use a
        /// scenario-specific config (e.g. "FF_Demonstrator").
        /// </summary>
        public static string DefaultSubtreeConfigName { get; set; } = "FF_Default";

        // Action to be processed in the next tick
        private PActionNode pendingAction;

        // Flag to ensure subtree is injected only once
        private bool hasInjectedSubtree = false;

        // Logging system
        private static readonly string LogFilePath = "SubtreeInjectionService_Debug.log";
        private static readonly object LogLock = new object();

        public ServiceSubtreeInject(IBehaviorTree owningTree, PActionNode action) : base(owningTree)
        {
            pendingAction = action;
            
            InitializeDefaultConfigurations();
        }

        /// <summary>
        /// Alternative constructor that allows setting the tree later
        /// </summary>
        public ServiceSubtreeInject(PActionNode action) : base(null)
        {
            pendingAction = action;
            
            InitializeDefaultConfigurations();
        }

        /// <summary>
        /// Log message to both console and file
        /// </summary>
        private void LogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] {message}";
            
            // Write to console
            LoggingService.LogInfo(logMessage);
            
            // File write handled by LoggingService's buffered LogFileManager
        }

        public void resetAfterSuccessFullExecution()
        {
            // reset the pending action
            //pendingAction = null;
            // reset the parameter instances
           // parameterInstances.Clear();

            // Reset the DynamicPlanningComplete flag on blackboard
            if (linkedBlackboard != null)
            {
                LogMessage($"🔄 ServiceSubtreeInject: Resetting CassetteSubtreeCompleted flags on blackboard");

                // Loop through the array and set each item to false
                if (linkedBlackboard.CassetteSubtreeCompleted != null)
                {
                    for (int i = 0; i < linkedBlackboard.CassetteSubtreeCompleted.Length; i++)
                    {
                        linkedBlackboard.CassetteSubtreeCompleted[i] = false;
                        LogMessage($"🔄 ServiceSubtreeInject: Set cassette{i + 1} subtree completion flag to false");
                    }
                    LogMessage($"✅ ServiceSubtreeInject: Successfully reset all {linkedBlackboard.CassetteSubtreeCompleted.Length} cassette subtree completion flags");
                }
                else
                {
                    LogMessage($"⚠️ ServiceSubtreeInject: CassetteSubtreeCompleted array is null, cannot reset flags");
                }
            }
            else
            {
                LogMessage($"⚠️ ServiceSubtreeInject: LinkedBlackboard is null, cannot reset CassetteSubtreeCompleted flags");
            }

            // NEW: Clear NodeGraphs for all existing subtrees except successful ones
            ClearNodeGraphsForExistingSubtrees();
                    
        }

        

        /// <summary>
        /// Service tick method - implements the required logic:
        /// 1. Check if action is HL by checking the name
        /// 2. If not HL, return true
        /// 3. If HL, inject the subtree
        /// 4. Return true if injection successful, false otherwise
        /// </summary>
        public override bool OnEvaluate(float InDeltaTime)
        {
            LogMessage($"🔍 ServiceSubtreeInject: Tick called for service attached to tree: {OwningTree?.GetType().Name}");
            
            // First, check if we have a pending action to process
            if (pendingAction != null)
            {
                var actionType = pendingAction.actionType.ToString();
                LogMessage($"🔍 ServiceSubtreeInject: Processing queued action: {actionType}");
                LogMessage($"🔍 ServiceSubtreeInject: Action type ends with 'HL': {actionType.EndsWith("HL")}");
                
                // 1. Check if the action is HL by checking the name of the action
                if (!actionType.EndsWith("HL"))
                {
                    LogMessage($"🔍 ServiceSubtreeInject: Action {actionType} is not a high-level action (no 'HL' suffix)");
                    // 2. If it is not HL return true
                    return true;
                }
                
                // 3. If it is HL, then we Inject the subtree
                LogMessage($"🔍 ServiceSubtreeInject: Detected high-level action: {actionType}");

                // Guard: only inject once — re-planning is handled by ServicePDDLPlanning
                if (hasInjectedSubtree)
                {
                    LogMessage($"⏭️ ServiceSubtreeInject: Subtree already injected for {actionType}, skipping (re-planning handled by ServicePDDLPlanning)");
                    return true;
                }

                try
                {
                    ProcessSubtreeInjection( null); // customParameters would be passed here if needed
                    hasInjectedSubtree = true;
                    LogMessage($"✅ ServiceSubtreeInject: Successfully injected subtree for {actionType}");
                    // 4. If the injection was successful return true
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage($"❌ ServiceSubtreeInject: Failed to inject subtree for {actionType}: {ex.Message}");
                    // 4. else, return false
                    return false; 
                }
            }
            else
            {
                LogMessage($"🔍 ServiceSubtreeInject: No pending action to process (pendingAction is null)");
            }
            
            return true; // No action to process
            
               
            }
            
           

        

        /// <summary>
        /// Process subtree injection for a specific action
        /// </summary>
        private void ProcessSubtreeInjection( Dictionary<string, object> customParameters = null)
        {
            try
            {
                var actionType = pendingAction.actionType.ToString();
                LogMessage($"🔧 ServiceSubtreeInject: Processing injection for {actionType}");
                
                // Use the globally configured subtree config (settable per scenario)
                string configName = DefaultSubtreeConfigName;
                
                // Create instance name from action
                string instanceName = pendingAction.InstanceName.ToString();
                
                // NOTE: Problem file generation has been moved to ServicePDDLPlanning.OnEvaluate()
                // so that each planning/re-planning cycle uses a fresh blackboard snapshot.
                // Here we only inject the subtree structure (DynamicFlowNode + ServicePDDLPlanning)
                
                var mergedParameters = customParameters ?? new Dictionary<string, object>();
                
                // Inherit the HL-level problem file so the ML subtree uses the correct
                // cassette-specific objects (e.g. ProblemL3L4.pddl instead of ProblemL1L2.pddl)
                if (pendingAction.ParentNode is FlowNode hlFlowNode
                    && hlFlowNode.ServicePlanning is ServicePDDLPlanning hlPlanner
                    && !string.IsNullOrEmpty(hlPlanner.PlanningRequest.ProblemFile))
                {
                    mergedParameters["problemFile"] = hlPlanner.PlanningRequest.ProblemFile;
                    LogMessage($"🔧 ServiceSubtreeInject: Inherited HL problem file: {hlPlanner.PlanningRequest.ProblemFile}");
                }
                
                LogMessage($"🔧 ServiceSubtreeInject: Merged parameters count: {mergedParameters.Count}");
                foreach (var param in mergedParameters)
                {
                    LogMessage($"   Parameter: {param.Key} = {param.Value}");
                }
                
                // Inject the subtree
                InjectSubtreeIntoAction(pendingAction, configName, instanceName, mergedParameters);
                
                LogMessage($"✅ ServiceSubtreeInject: Successfully processed injection for {actionType}");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ ServiceSubtreeInject: Error processing injection: {ex.Message}");
            }
        }



        /// <summary>
        /// Configuration for subtree creation.
        /// PlannerName identifies which planner to use (e.g. "FF", "ENHSP", "LAMA-FIRST").
        /// </summary>
        public class SubtreeConfiguration
        {
            public string Name { get; set; }
            public string PlannerName { get; set; }
            public SuccessCriteria SuccessCriteria { get; set; }
            public Dictionary<string, object> PlannerParameters { get; set; }
            public bool UseCaching { get; set; } = true;

            public SubtreeConfiguration(string name, string plannerName, SuccessCriteria successCriteria = SuccessCriteria.ALL_FINISHED)
            {
                Name = name;
                PlannerName = plannerName;
                SuccessCriteria = successCriteria;
                PlannerParameters = new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Initialize default subtree configurations by auto-discovering Planner subclasses.
        /// Each Planner provides its own default config via GetDefaultConfig(), keeping
        /// planner-specific values (domain file, planner path, timeout, etc.) out of this service.
        /// </summary>
        private void InitializeDefaultConfigurations()
        {
            if (_defaultConfigsInitialized) return;
            _defaultConfigsInitialized = true;

            var assembly = typeof(Planner).Assembly;
            foreach (var type in assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(Planner)) && !t.IsAbstract))
            {
                var planner = (Planner)Activator.CreateInstance(type);
                var configName = $"{planner.DefaultPlannerName}_Default";

                var config = new SubtreeConfiguration(configName, planner.DefaultPlannerName, SuccessCriteria.ALL_FINISHED);
                config.PlannerParameters["domainFile"] = planner.DefaultDomainFile;
                config.PlannerParameters["problemFile"] = planner.DefaultProblemFile;
                config.PlannerParameters["plannerPath"] = planner.DefaultPlannerPath;
                config.PlannerParameters["timeoutSeconds"] = planner.DefaultTimeoutSeconds;
                config.PlannerParameters["maxPlanLength"] = planner.DefaultMaxPlanLength;
                config.PlannerParameters["executionMode"] = ServicePDDLPlanning.ParallelExecutionMode.Sequential;
                if (planner.HasEnhspConfig)
                    config.PlannerParameters["enhspConfig"] = planner.DefaultEnhspConfig;

                subtreeConfigurations[configName] = config;
                LogMessage($"✅ ServiceSubtreeInject: Auto-registered config '{configName}' from {type.Name}");
            }

            // Register a dedicated ML-level ENHSP config that uses DomainML.pddl and opt-hmax.
            // This is separate from ENHSP_Default (which uses DomainHL.pddl for HL cassette planning).
            if (subtreeConfigurations.TryGetValue("ENHSP_Default", out var enhspDefault))
            {
                var mlConfig = new SubtreeConfiguration("ENHSP_ML_Default", "Enhsp", SuccessCriteria.ALL_FINISHED);
                foreach (var kv in enhspDefault.PlannerParameters)
                    mlConfig.PlannerParameters[kv.Key] = kv.Value;
                mlConfig.PlannerParameters["domainFile"] = "Plannerinputs/static/DomainML.pddl";
                mlConfig.PlannerParameters["enhspConfig"] = "opt-hmax";
                subtreeConfigurations["ENHSP_ML_Default"] = mlConfig;
                LogMessage("✅ ServiceSubtreeInject: Registered ENHSP_ML_Default (DomainML.pddl, opt-hmax)");
            }

            LogMessage("✅ ServiceSubtreeInject: Initialized default configurations");
        }

        /// <summary>
        /// Register a custom subtree configuration (instance method)
        /// </summary>
        public void RegisterConfiguration(string configName, SubtreeConfiguration configuration)
        {
            subtreeConfigurations[configName] = configuration;
            LogMessage($"✅ ServiceSubtreeInject: Registered configuration '{configName}'");
        }

        /// <summary>
        /// Register a custom subtree configuration globally (static, available to all instances).
        /// Call this before the tree starts ticking.
        /// </summary>
        public static void RegisterGlobalConfiguration(string configName, SubtreeConfiguration configuration)
        {
            subtreeConfigurations[configName] = configuration;
        }

        /// <summary>
        /// Get a registered configuration
        /// </summary>
        public SubtreeConfiguration GetConfiguration(string configName)
        {
            if (subtreeConfigurations.TryGetValue(configName, out var config))
            {
                return config;
            }
            throw new ArgumentException($"Configuration '{configName}' not found");
        }

       
        /// <summary>
        /// Create a subtree using a configuration
        /// </summary>
        public DynamicFlowNode CreateSubtree(SubtreeConfiguration config, string instanceName, Dictionary<string, object> customParameters = null)
        {
            try
            {
                LogMessage($"🔧 ServiceSubtreeInject: Creating subtree '{config.Name}' for instance '{instanceName}'");

                // Check cache first
                string cacheKey = $"{config.Name}_{instanceName}";
                if (config.UseCaching && cachedSubtrees.TryGetValue(cacheKey, out var cachedSubtree))
                {
                    LogMessage($"✅ ServiceSubtreeInject: Using cached subtree for '{cacheKey}'");
                    return cachedSubtree;
                }

                // Create subtree using the unified planner factory method
                DynamicFlowNode subtree = CreatePlannerSubtree(config, instanceName, customParameters);

                // Cache the subtree if caching is enabled
                if (config.UseCaching)
                {
                    cachedSubtrees[cacheKey] = subtree;
                    LogMessage($"💾 ServiceSubtreeInject: Cached subtree for '{cacheKey}'");
                }

                // Add the ExclusiveBranchGate decorator
                subtree.AddDecorator(new DecoratorExclusiveBranchGate(subtree as DynamicFlowNode));
                LogMessage($"🔧 ServiceSubtreeInject: Added ExclusiveBranchGate decorator to flow node '{subtree.DebugDisplayName}'");
                // DEBUG: Check if the decorator is actually in the list
                var decoratorCount = subtree.GetType().GetField("Decorators", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(subtree) as System.Collections.Generic.List<object>;
                if (decoratorCount != null)
                {
                    LogMessage($"🔍 DEBUG: ServiceSubtreeInject: Decorators list has {decoratorCount.Count} decorators");
                    foreach (var decorator in decoratorCount)
                    {
                        LogMessage($"🔍 DEBUG: ServiceSubtreeInject: Found decorator: {decorator.GetType().Name}");
                    }
                }
                else
                {
                    LogMessage($"⚠️ DEBUG: ServiceSubtreeInject: Could not access Decorators list for subtree '{subtree.DebugDisplayName}'");
                }

                LogMessage($"✅ ServiceSubtreeInject: Created subtree successfully");
                return subtree;
            }
            catch (Exception ex)
            {
                LogMessage($"❌ ServiceSubtreeInject: Error creating subtree: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Inject a subtree into an action
        /// </summary>
        public void InjectSubtreeIntoAction(PActionNode action, string configName, string instanceName, Dictionary<string, object> customParameters = null)
        {
            var config = GetConfiguration(configName);
            var subtree = CreateSubtree(config, instanceName, customParameters);
            action.SetAsHighLevelAction(subtree, subtree.ServicePlanning);
            LogMessage($"✅ ServiceSubtreeInject: Injected subtree '{configName}' into action '{action.InstanceName.ToString()}'");
            
            // Register the subtree in the .bt model file (annotation + BT block)
            RegisterSubtreeInBTModel(action, config, instanceName);
            
            // Track subtree injection
            BehaviorTreeComponentLogger.TrackSubtreeInjection($"{configName}_{instanceName}");
            
            // NOTE: Subtrees are now added to blackboard after successful planning, not during injection
        }

        /// <summary>
        /// Registers the subtree in the .bt model file by:
        /// 1. Adding @SubtreeName annotation to the parent HL action line
        /// 2. Appending a new BehaviorTree block for the subtree with an empty NodeGraph
        /// This allows the existing planner flow (APTreeModelWriter.UpdateCassetteNodeGraph)
        /// to later find the subtree's FlowNode and populate its NodeGraph with planned actions.
        /// </summary>
        private void RegisterSubtreeInBTModel(PActionNode action, SubtreeConfiguration config, string instanceName)
        {
            try
            {
                // Derive names consistent with the runtime subtree structure
                var subtreeBTName = $"{instanceName}Subtree";
                // Must match the runtime DynamicFlowNode name so APTreeModelWriter can find it later
                var flowNodeName = $"{config.Name}_DynamicFlow_{instanceName}";
                var plannerServiceName = $"subtreeSrv_{instanceName}";

                // Extract just the filename from the full paths, without .pddl extension
                // (DSL model uses plain names like "DomainMLTruss", not "DomainMLTruss.pddl")
                string domainFilePath = config.PlannerParameters.TryGetValue("domainFile", out var dfObj) ? dfObj?.ToString() : null;
                string domainFileName = string.IsNullOrWhiteSpace(domainFilePath)
                    ? "DomainML"
                    : System.IO.Path.GetFileNameWithoutExtension(domainFilePath);

                string problemFilePath = config.PlannerParameters.TryGetValue("problemFile", out var pfObj) ? pfObj?.ToString() : null;
                string problemFileName = string.IsNullOrWhiteSpace(problemFilePath)
                    ? null
                    : System.IO.Path.GetFileNameWithoutExtension(problemFilePath);

                LogMessage($"🔧 ServiceSubtreeInject: Registering subtree BT model '{subtreeBTName}' for action '{instanceName}'");

                // 1. Annotate the parent HL action with @SubtreeName
                BehaviorTreeMainProject.Services.AIPlanning.APTreeModelWriter.AnnotateActionWithSubtree(instanceName, subtreeBTName);

                // 2. Append the subtree BehaviorTree block after the main BT
                BehaviorTreeMainProject.Services.AIPlanning.APTreeModelWriter.AppendSubtreeBTModel(
                    subtreeBTName, flowNodeName, plannerServiceName, config.PlannerName, domainFileName, problemFileName);

                LogMessage($"✅ ServiceSubtreeInject: Registered subtree '{subtreeBTName}' in BT model for action '{instanceName}'");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ ServiceSubtreeInject: Error registering subtree in BT model: {ex.Message}");
            }
        }

        




        
        /// <summary>
        /// Clear NodeGraphs for all existing subtrees except successful ones
        /// </summary>
        private void ClearNodeGraphsForExistingSubtrees()
        {
            try
            {
                var allInjectedSubtrees = linkedBlackboard.GetAllInjectedSubtrees();
                LogMessage($"🔄 ServiceSubtreeInject: Starting NodeGraph cleanup for {allInjectedSubtrees.Count} injected subtrees");
                
                foreach (var subtree in allInjectedSubtrees)
                {
                    if (subtree == null) continue;
                    
                    // Check if this subtree was successful
                    bool isSuccessful = subtree.status == BTNodeResult.Success;
                    
                    if (isSuccessful)
                    {
                        LogMessage($"✅ ServiceSubtreeInject: Skipping successful subtree '{subtree.DebugDisplayName}' - keeping NodeGraph");
                        continue;
                    }

                    // Clear the NodeGraph for non-successful subtrees
                    // allow subtree to re-plan next round
                    var actionGraph = subtree.GetActionGraph();
                    int actionCount = actionGraph?.GetAllActionNodes().Count ?? 0;
                    
                    subtree.ResetForNextRound(); // clears planningCompleted, tickCount, actionGraph
                    if (subtree.ServicePlanning is ServicePlanning p)
                    {
                        p.ResetPlanningService(); // or p.ResetPlanningService();
                    }
                    
                    // Track NodeGraph reset
                    if (actionCount > 0)
                    {
                        BehaviorTreeComponentLogger.TrackNodeGraphReset("SubtreeReset", actionCount, $"Non-successful subtree: {subtree.DebugDisplayName}");
                    }
                    
                    // Note: NodeGraph clearing is handled by ResetForNextRound() -> ClearActionGraph() -> actionGraph.Clear()
                    LogMessage($"🧹 ServiceSubtreeInject: NodeGraph clearing handled by ResetForNextRound() for subtree '{subtree.DebugDisplayName}' (status: {subtree.status})");
                }
                
                // NEW: Clear all injected subtrees from blackboard to start fresh
                // This ensures only currently active subtrees are tracked
                LogMessage($"🧹 ServiceSubtreeInject: Clearing all injected subtrees from blackboard to start fresh");
                linkedBlackboard.ClearInjectedSubtrees();
                LogMessage($"✅ ServiceSubtreeInject: Cleared {allInjectedSubtrees.Count} injected subtrees from blackboard");
                
                // Track subtree clearing
                BehaviorTreeComponentLogger.TrackSubtreeClearing(allInjectedSubtrees.Count, "Reset after successful execution");
                
                LogMessage($"✅ ServiceSubtreeInject: Completed NodeGraph cleanup");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ ServiceSubtreeInject: Error during NodeGraph cleanup: {ex.Message}");
            }
        }



        /// <summary>
        /// Creates a subtree for any planner type. The planner name comes from config.PlannerName,
        /// so adding a new planner requires no changes here — just add a new Planner subclass.
        /// </summary>
        private DynamicFlowNode CreatePlannerSubtree(SubtreeConfiguration config, string instanceName, Dictionary<string, object> customParameters)
        {
            var subtreeTree = new BehaviorTree();
            subtreeTree.Initialise(linkedBlackboard, $"{config.Name}_Subtree_{instanceName}");

            var dynamicFlowNode = new DynamicFlowNode(
                new FastName($"{config.Name}_DynamicFlow_{instanceName}"),
                subtreeTree,
                config.SuccessCriteria
            );

            var parameters = MergeParameters(config.PlannerParameters, customParameters);

            LogMessage($"🔧 ServiceSubtreeInject: Creating {config.PlannerName} subtree with parameters:");
            LogMessage($"   Domain File: {parameters["domainFile"]}");
            LogMessage($"   Problem File: {parameters["problemFile"]}");
            LogMessage($"   Planner Path: {parameters["plannerPath"]}");
            LogMessage($"   Timeout: {parameters["timeoutSeconds"]} seconds");
            LogMessage($"   Max Plan Length: {parameters["maxPlanLength"]}");

            var pddlRequest = new PDDLPlanningRequest(
                parameters["domainFile"].ToString(),
                parameters["problemFile"].ToString(),
                parameters["plannerPath"].ToString(),
                config.PlannerName,
                Convert.ToInt32(parameters["timeoutSeconds"]),
                Convert.ToInt32(parameters["maxPlanLength"])
            );
            if (parameters.TryGetValue("enhspConfig", out var enhspCfg) && enhspCfg != null)
                pddlRequest.EnhspConfig = enhspCfg.ToString();

            var planner = new ServicePDDLPlanning(subtreeTree, pddlRequest);
            planner.ExecutionMode = (ServicePDDLPlanning.ParallelExecutionMode)parameters["executionMode"];
            planner.CurrentPlanner = Planner.FromName(config.PlannerName);

            dynamicFlowNode.SetPlanningService(planner);
            subtreeTree.root = dynamicFlowNode;

            return dynamicFlowNode;
        }

        

     

        

        /// <summary>
        /// Merge default and custom parameters
        /// </summary>
        private Dictionary<string, object> MergeParameters(Dictionary<string, object> defaultParams, Dictionary<string, object> customParams)
        {
            var merged = new Dictionary<string, object>(defaultParams);
            
            LogMessage($"🔧 ServiceSubtreeInject: Merging parameters - Default params: {defaultParams.Count}, Custom params: {customParams?.Count ?? 0}");
            
            if (customParams != null)
            {
                foreach (var kvp in customParams)
                {
                    var oldValue = defaultParams.ContainsKey(kvp.Key) ? defaultParams[kvp.Key].ToString() : "not set";
                    LogMessage($"🔧 ServiceSubtreeInject: Overriding parameter {kvp.Key}: {oldValue} -> {kvp.Value}");
                    merged[kvp.Key] = kvp.Value;
                }
            }
            
            LogMessage($"🔧 ServiceSubtreeInject: Final merged parameters count: {merged.Count}");
            return merged;
        }

        

      




    }
}

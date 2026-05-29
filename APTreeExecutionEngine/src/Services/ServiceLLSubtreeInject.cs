using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

using AIPlanning;
using BehaviorTreeMainProject.Services.AIPlanning;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;
using RobotCommand;

namespace BehaviorTreeMainProject
{
    /// <summary>
    /// Low-Level (LL) subtree injection service.
    /// Attaches to ML-level actions and expands them into parameterized LL subtrees
    /// that contain the actual robot execution primitives (move, grip, place, etc.).
    ///
    /// Approach: Hybrid — each ML action type maps to a parameterized subtree template.
    /// Certain template nodes can internally call motion planners or robot services.
    ///
    /// Flow: HL plan → ML actions (via ServiceSubtreeInject) → LL subtrees (via this service)
    /// </summary>
    public class ServiceLLSubtreeInject : Service
    {
        private static readonly Dictionary<string, LLSubtreeTemplate> _templates = new Dictionary<string, LLSubtreeTemplate>(StringComparer.OrdinalIgnoreCase);
        private static bool _templatesInitialized = false;

        // The ML-level action this service is attached to
        private PActionNode _mlAction;

        // Shared robot communicator — one instance for all LL nodes in this subtree
        private readonly IRobotCommandCommunicator _communicator;

        // Guard: only inject once per action
        private bool _hasInjected = false;

        // Logging
        private static readonly string LogFilePath = "ServiceLLSubtreeInject_Debug.log";
        private static readonly object LogLock = new object();

        // ──────────────────── Templates ────────────────────

        /// <summary>
        /// Describes a low-level subtree template for an ML action type.
        /// The Steps list defines the ordered sequence of LL primitives.
        /// Each step can reference ML-action parameters by name (e.g. "{obj}", "{client}").
        /// </summary>
        public class LLSubtreeTemplate
        {
            public string MLActionType { get; set; }
            public List<LLStep> Steps { get; set; } = new List<LLStep>();

            public LLSubtreeTemplate(string mlActionType)
            {
                MLActionType = mlActionType;
            }
        }

        /// <summary>
        /// A single low-level step inside a template.
        /// ActionName  – the LL primitive (e.g. "MoveTo", "OpenGripper").
        /// Parameters  – key/value pairs; values may contain "{paramName}" placeholders
        ///               that are resolved at injection time from the ML action's parameters.
        /// </summary>
        public class LLStep
        {
            public string ActionName { get; set; }
            /// <summary>
            /// Per-step instance name from the JSON template (e.g. "moveToRobotPosition").
            /// Included in the runtime LL node's InstanceName so fault triggers and
            /// logs can distinguish otherwise identical steps (e.g. two MoveToLLs in
            /// the same ML subtree).
            /// </summary>
            public string InstanceName { get; set; }
            public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
            public MoveType? MoveType { get; set; }

            public LLStep(string actionName, MoveType? moveType = null)
            {
                ActionName = actionName;
                MoveType = moveType;
            }
        }

        // ──────────────────── Construction ────────────────────

        public ServiceLLSubtreeInject(IBehaviorTree owningTree, PActionNode mlAction, IRobotCommandCommunicator communicator = null) : base(owningTree)
        {
            _mlAction = mlAction;
            _communicator = communicator;
            InitializeDefaultTemplates();
        }

        public ServiceLLSubtreeInject(PActionNode mlAction, IRobotCommandCommunicator communicator = null) : base(null)
        {
            _mlAction = mlAction;
            _communicator = communicator;
            InitializeDefaultTemplates();
        }

        // ──────────────────── Template registration ────────────────────

        /// <summary>
        /// Register a template for an ML action type.
        /// Call this before the tree starts ticking to customise the LL decomposition.
        /// </summary>
        public static void RegisterTemplate(string mlActionType, LLSubtreeTemplate template)
        {
            _templates[mlActionType] = template;
        }

        /// <summary>
        /// Returns the template registered for the given ML action type, or null.
        /// </summary>
        public static LLSubtreeTemplate GetTemplate(string mlActionType)
        {
            _templates.TryGetValue(mlActionType, out var t);
            return t;
        }

        /// <summary>
        /// Loads LL subtree templates from the generated JSON file.
        /// Falls back to a warning if the file is not found.
        /// </summary>
        private static void InitializeDefaultTemplates()
        {
            if (_templatesInitialized) return;
            _templatesInitialized = true;

            // Convention: look for DemonstratorLLSubtrees.json next to the executable,
            // then fall back to the ModelLoader source path (dev mode).
            string jsonPath = FindLLSubtreeJson();
            if (jsonPath == null)
            {
                LogMessageStatic("⚠️ ServiceLLSubtreeInject: No LLSubtrees.json found — no LL templates loaded");
                return;
            }

            try
            {
                string json = File.ReadAllText(jsonPath);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("llSubtrees", out var subtreesArr))
                {
                    LogMessageStatic("⚠️ ServiceLLSubtreeInject: JSON missing 'llSubtrees' array");
                    return;
                }

                int count = 0;
                foreach (var subtreeEl in subtreesArr.EnumerateArray())
                {
                    string mlAction = subtreeEl.GetProperty("mlAction").GetString();
                    var template = new LLSubtreeTemplate(mlAction);

                    foreach (var stepEl in subtreeEl.GetProperty("steps").EnumerateArray())
                    {
                        string actionType = stepEl.GetProperty("actionType").GetString();

                        // Parse MoveType from contParams if present
                        MoveType? moveType = null;
                        if (stepEl.TryGetProperty("contParams", out var contParams) &&
                            contParams.TryGetProperty("moveType", out var moveTypeEl))
                        {
                            moveType = ParseMoveType(moveTypeEl.GetString());
                        }

                        var step = new LLStep(actionType, moveType);

                        // Optional per-step instanceName (used to disambiguate runtime
                        // LL node names and to let fault triggers target specific steps).
                        if (stepEl.TryGetProperty("instanceName", out var instNameEl))
                        {
                            step.InstanceName = instNameEl.GetString();
                        }

                        // Load paramBindings → Parameters dict (already in {placeholder} format)
                        if (stepEl.TryGetProperty("paramBindings", out var bindings))
                        {
                            foreach (var binding in bindings.EnumerateObject())
                            {
                                step.Parameters[binding.Name] = binding.Value.GetString();
                            }
                        }

                        template.Steps.Add(step);
                    }

                    _templates[mlAction] = template;
                    count++;
                }

                LogMessageStatic($"✅ ServiceLLSubtreeInject: Loaded {count} LL subtree templates from {jsonPath}");
            }
            catch (Exception ex)
            {
                LogMessageStatic($"❌ ServiceLLSubtreeInject: Failed to load LLSubtrees.json: {ex.Message}");
            }
        }

        /// <summary>
        /// Searches known locations for the LL subtree JSON file.
        /// </summary>
        private static string FindLLSubtreeJson()
        {
            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DemonstratorLLSubtrees.json"),
                Path.Combine("src", "ModelLoader", "DemonstratorLLSubtrees.json"),
                "DemonstratorLLSubtrees.json"
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                    return Path.GetFullPath(path);
            }
            return null;
        }

        /// <summary>
        /// Parses a moveType string from the .bt model to the MoveType enum.
        /// </summary>
        private static MoveType? ParseMoveType(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return value.ToLowerInvariant() switch
            {
                "movej" => global::MoveType.MoveJ,
                "movel" => global::MoveType.MoveL,
                "movep" => global::MoveType.MoveP,
                "movec" => global::MoveType.MoveC,
                "planned" => global::MoveType.Planned,
                "plannedj" or "planned_j" => global::MoveType.PlannedJ,
                "plannedl" or "planned_l" => global::MoveType.PlannedL,
                _ => null
            };
        }

        private static void LogMessageStatic(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] {message}";
            LoggingService.LogInfo(logMessage);
            // File write handled by LoggingService's buffered LogFileManager
        }

        // ──────────────────── Service tick ────────────────────

        /// <summary>
        /// Called each tick. If the action is an ML action and hasn't been expanded yet,
        /// build and inject the LL subtree.
        /// </summary>
        public override bool OnEvaluate(float InDeltaTime)
        {
            if (_mlAction == null) return true;

            var actionType = _mlAction.actionType.ToString();

            // Only target ML-level actions
            if (!actionType.EndsWith("ML"))
                return true;

            if (_hasInjected)
                return true;

            // Gate: only inject when execution is active (blackboard flag)
            if (linkedBlackboard != null)
            {
                try
                {
                    bool executionActive = linkedBlackboard.GetBool(new FastName("ExecutionActive"));
                    if (!executionActive)
                    {
                        LogMessage($"\u23f8\ufe0f ServiceLLSubtreeInject: ExecutionActive is false \u2014 skipping injection for '{actionType}'");
                        return true;
                    }
                }
                catch
                {
                    // Flag not yet set on blackboard — skip injection
                    LogMessage($"\u23f8\ufe0f ServiceLLSubtreeInject: ExecutionActive not found on blackboard \u2014 skipping injection for '{actionType}'");
                    return true;
                }
            }

            // Look up template
            if (!_templates.TryGetValue(actionType, out var template))
            {
                LogMessage($"⚠️ ServiceLLSubtreeInject: No LL template registered for '{actionType}' — action will execute as-is");
                _hasInjected = true; // don't retry
                return true;
            }

            try
            {
                InjectLLSubtree(_mlAction, template);
                _hasInjected = true;
                LogMessage($"✅ ServiceLLSubtreeInject: Injected LL subtree ({template.Steps.Count} steps) for {actionType} → {_mlAction.InstanceName}");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"❌ ServiceLLSubtreeInject: Failed to inject LL subtree for {actionType}: {ex.Message}");
                return false;
            }
        }

        // ──────────────────── Subtree construction ────────────────────

        /// <summary>
        /// Builds a sequential DynamicFlowNode containing one child per LL step,
        /// resolves parameter placeholders, and attaches it to the ML action.
        /// </summary>
        private void InjectLLSubtree(PActionNode mlAction, LLSubtreeTemplate template)
        {
            // Collect parameter values from the ML action via reflection
            var mlParamStrings = ExtractMLActionParameters(mlAction);
            var mlParamObjects = ExtractMLActionParameterObjects(mlAction);
            LogMessage($"🔧 ServiceLLSubtreeInject: ML action '{mlAction.InstanceName}' has {mlParamStrings.Count} parameters");
            foreach (var kv in mlParamStrings)
                LogMessage($"   {kv.Key} = {kv.Value}");

            // Create the subtree
            var subtreeTree = new BehaviorTree();
            subtreeTree.Initialise(linkedBlackboard, $"LL_Subtree_{mlAction.InstanceName}");

            var flowNode = new DynamicFlowNode(
                new FastName($"LL_DynamicFlow_{mlAction.InstanceName}"),
                subtreeTree,
                SuccessCriteria.ALL,
                addRetryDecorator: false
            );

            // Build executable LL action nodes for the NodeGraph
            var llActions = new List<PActionNode>();
            foreach (var step in template.Steps)
            {
                var resolvedParams = ResolveParameters(step.Parameters, mlParamStrings);
                var resolvedObjects = ResolveParameterObjects(step.Parameters, mlParamObjects);
                // Include the per-step instanceName so multiple steps of the same ActionName
                // (e.g. two MoveToLLs in StackML) get distinct runtime names and fault
                // triggers like AfterLLStep="moveToRobotPosition" can match.
                var stepName = string.IsNullOrWhiteSpace(step.InstanceName)
                    ? $"{step.ActionName}_{mlAction.InstanceName}"
                    : $"{step.ActionName}_{step.InstanceName}_{mlAction.InstanceName}";

                PActionNode llNode = CreateLLNode(step, stepName, resolvedParams, resolvedObjects, linkedBlackboard, _communicator);

                // Set the owning tree so services can access the blackboard
                llNode.SetOwiningTree(subtreeTree);
                llNode.SetTreeForAllServices(subtreeTree);

                llActions.Add(llNode);
                LogMessage($"   ➕ Added LL step: {step.ActionName} → {stepName}");
            }

            // Create a sequential NodeGraph (MEETS temporal constraints) and set it
            var graph = flowNode.CreateNodeGraphFromActions(llActions);
            flowNode.ForceSetActionGraph(graph);

            subtreeTree.root = flowNode;

            // Attach LL subtree to the ML action WITHOUT adding DecoratorResetOnSubtreeSuccess.
            // That decorator is intended for HL actions only — if it fires on ML actions it
            // clears the parent ML-level NodeGraph and causes a NullRef on the next ML action.
            mlAction.IsHighLevelAction = true;
            mlAction.HighLevelSubtree = flowNode;
            flowNode.SetParentNode(mlAction);

            // Attach recovery decorator so operator can retry/replan on LL failure
            mlAction.AddDecorator(new DecoratorRecovery(mlAction));

            LogMessage($"✅ ServiceLLSubtreeInject: Attached LL subtree ({template.Steps.Count} steps) + recovery decorator to {mlAction.InstanceName}");
        }

        // ──────────────────── LL node factory ────────────────────

        /// <summary>
        /// Creates the correct ExeAction (or falls back to LLActionNode) for a template step.
        /// Attaches a DecoratorLLInputResolver to resolve ML inputs into the action's typed properties.
        /// </summary>
        private PActionNode CreateLLNode(LLStep step, string stepName, Dictionary<string, string> resolvedParams, Dictionary<string, object> resolvedObjects, Blackboard<FastName> blackboard, IRobotCommandCommunicator communicator)
        {
            ExeAction exeNode = null;

            switch (step.ActionName)
            {
                case "MoveToLL":
                    var target = resolvedParams.GetValueOrDefault("target", "unknown");
                    var moveType = step.MoveType ?? MoveType.MoveJ;
                    double vel = step.Parameters.TryGetValue("velocity", out var vStr) && double.TryParse(vStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var vParsed) ? vParsed : 1.0;
                    double acc = step.Parameters.TryGetValue("acceleration", out var aStr) && double.TryParse(aStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var aParsed) ? aParsed : 1.0;
                    exeNode = new MoveToLL(stepName, "", target, blackboard, moveType, velocity: vel, acceleration: acc, communicator: communicator);
                    break;

                case "CloseGripperLL":
                    exeNode = new CloseGripperLL(stepName, blackboard, communicator: communicator);
                    break;

                case "OpenGripperLL":
                    exeNode = new OpenGripperLL(stepName, blackboard, communicator: communicator);
                    break;

                case "LiftLL":
                    exeNode = new LiftLL(stepName, blackboard, communicator: communicator);
                    break;

                case "StackReleaseLL":
                    var srMoveType = step.MoveType ?? MoveType.MoveJ;
                    double srVel = step.Parameters.TryGetValue("velocity", out var srVStr) && double.TryParse(srVStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var srVParsed) ? srVParsed : 1.0;
                    double srAcc = step.Parameters.TryGetValue("acceleration", out var srAStr) && double.TryParse(srAStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var srAParsed) ? srAParsed : 1.0;
                    exeNode = new StackReleaseLL(stepName, blackboard, srMoveType, srVel, srAcc, communicator: communicator);
                    break;

                case "EquipToolLL":
                    exeNode = new EquipToolLL(stepName, blackboard, communicator: communicator);
                    break;

                case "DeequipToolLL":
                    exeNode = new DeequipToolLL(stepName, blackboard, communicator: communicator);
                    break;

                case "NailingLL":
                    exeNode = new NailingLL(stepName, blackboard, communicator: communicator);
                    break;

                case "NailAndRetractLL":
                    exeNode = new NailAndRetractLL(stepName, blackboard, communicator: communicator);
                    break;

                case "PushDownLL":
                    exeNode = new PushDownLL(stepName, blackboard, communicator: communicator);
                    break;

                default:
                    // Fallback: generic LLActionNode for steps not yet mapped to ExeAction
                    LogMessage($"⚠️ ServiceLLSubtreeInject: No ExeAction for '{step.ActionName}', using generic LLActionNode");
                    return new LLActionNode(
                        new FastName(stepName),
                        step.ActionName,
                        resolvedParams,
                        blackboard
                    );
            }

            // Attach the decorator that resolves ML inputs → typed properties before first tick
            exeNode.AddDecorator(new DecoratorLLInputResolver(exeNode, resolvedObjects));
            return exeNode;
        }

        /// <summary>
        /// Extracts named parameter values as strings (IDs) from an ML action.
        /// Used for placeholder resolution in step templates.
        /// </summary>
        private Dictionary<string, string> ExtractMLActionParameters(PActionNode mlAction)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var actionType = mlAction.GetType();

            var props = actionType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var prop in props)
            {
                var value = prop.GetValue(mlAction);
                if (value is CustomProperty cp)
                {
                    result[prop.Name] = cp.ID;
                }
                else if (value != null)
                {
                    result[prop.Name] = value.ToString();
                }
            }

            return result;
        }

        /// <summary>
        /// Extracts actual parameter objects from an ML action via reflection.
        /// Returns the full CustomProperty objects (RobotPosition, Robot, Element, etc.)
        /// so LL actions can access typed data (joints, poses, IPs) directly.
        /// </summary>
        private Dictionary<string, object> ExtractMLActionParameterObjects(PActionNode mlAction)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var actionType = mlAction.GetType();

            var props = actionType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var prop in props)
            {
                var value = prop.GetValue(mlAction);
                if (value != null)
                {
                    result[prop.Name] = value;
                }
            }

            return result;
        }

        /// <summary>
        /// Resolves step parameter placeholders to actual objects from the ML action.
        /// E.g. template has {to} → resolves to the actual RobotPosition object.
        /// </summary>
        private Dictionary<string, object> ResolveParameterObjects(Dictionary<string, string> stepParams, Dictionary<string, object> mlParams)
        {
            var resolved = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in stepParams)
            {
                string value = kv.Value;
                if (value.StartsWith("{") && value.EndsWith("}"))
                {
                    string paramName = value.Substring(1, value.Length - 2);
                    if (mlParams.TryGetValue(paramName, out var mlValue))
                    {
                        resolved[kv.Key] = mlValue;
                    }
                }
            }
            return resolved;
        }

        /// <summary>
        /// Replaces {paramName} placeholders in step parameters with actual values
        /// from the ML action's parameter list.
        /// </summary>
        private Dictionary<string, string> ResolveParameters(Dictionary<string, string> stepParams, Dictionary<string, string> mlParams)
        {
            var resolved = new Dictionary<string, string>();
            foreach (var kv in stepParams)
            {
                string value = kv.Value;
                if (value.StartsWith("{") && value.EndsWith("}"))
                {
                    string paramName = value.Substring(1, value.Length - 2);
                    if (mlParams.TryGetValue(paramName, out var mlValue))
                    {
                        value = mlValue;
                    }
                    else
                    {
                        LogMessage($"⚠️ ServiceLLSubtreeInject: Placeholder '{kv.Value}' not found in ML action parameters");
                    }
                }
                resolved[kv.Key] = value;
            }
            return resolved;
        }

        // ──────────────────── Reset ────────────────────

        /// <summary>
        /// Resets the injection flag so the service can re-inject on the next tick cycle.
        /// Call this when the parent ML subtree is reset (e.g., after re-planning).
        /// </summary>
        public void Reset()
        {
            _hasInjected = false;
        }

        // ──────────────────── Logging ────────────────────

        private void LogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] {message}";
            LoggingService.LogInfo(logMessage);
            // File write handled by LoggingService's buffered LogFileManager
        }
    }
}

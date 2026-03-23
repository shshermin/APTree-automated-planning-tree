using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using AIPlanning;
using BehaviorTreeMainProject.Services.AIPlanning;
using ModelLoader.ParameterTypes;
using BehaviorTreeMainProject.Log.Services;

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
            public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
            public MoveType? MoveType { get; set; }

            public LLStep(string actionName, MoveType? moveType = null)
            {
                ActionName = actionName;
                MoveType = moveType;
            }
        }

        // ──────────────────── Construction ────────────────────

        public ServiceLLSubtreeInject(IBehaviorTree owningTree, PActionNode mlAction) : base(owningTree)
        {
            _mlAction = mlAction;
            InitializeDefaultTemplates();
        }

        public ServiceLLSubtreeInject(PActionNode mlAction) : base(null)
        {
            _mlAction = mlAction;
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
        /// Initialises sensible default templates for every known ML action type.
        /// These are intentionally minimal stubs — extend or replace them as you
        /// implement the actual robot primitives.
        /// </summary>
        private static void InitializeDefaultTemplates()
        {
            if (_templatesInitialized) return;
            _templatesInitialized = true;

            // ── PickUpML ──
            // Target is {p} = the stick's InitialLocation (position where the stick sits)
            var pickUp = new LLSubtreeTemplate("PickUpML");
            pickUp.Steps.Add(new LLStep("MoveToLL", MoveType.MoveJ) { Parameters = { ["target"] = "{p}", ["robot"] = "{client}" } });
            pickUp.Steps.Add(new LLStep("CloseGripperLL") { Parameters = { ["robot"] = "{client}" } });
            pickUp.Steps.Add(new LLStep("MoveToLL", MoveType.MoveL) { Parameters = { ["target"] = "{p}", ["robot"] = "{client}" } });
            _templates["PickUpML"] = pickUp;

            // ── StackML ──
            var stack = new LLSubtreeTemplate("StackML");
            stack.Steps.Add(new LLStep("MoveTo") { Parameters = { ["target"] = "{pos}", ["robot"] = "{client}" } });
            stack.Steps.Add(new LLStep("OpenGripper") { Parameters = { ["robot"] = "{client}" } });
            stack.Steps.Add(new LLStep("Retract") { Parameters = { ["robot"] = "{client}" } });
            _templates["StackML"] = stack;

            // ── StackOnTwoML ──
            var stackTwo = new LLSubtreeTemplate("StackOnTwoML");
            stackTwo.Steps.Add(new LLStep("MoveTo") { Parameters = { ["target"] = "{pos}", ["robot"] = "{client}" } });
            stackTwo.Steps.Add(new LLStep("Lower") { Parameters = { ["robot"] = "{client}", ["obj"] = "{obj}" } });
            stackTwo.Steps.Add(new LLStep("OpenGripper") { Parameters = { ["robot"] = "{client}" } });
            stackTwo.Steps.Add(new LLStep("Retract") { Parameters = { ["robot"] = "{client}" } });
            _templates["StackOnTwoML"] = stackTwo;

            // ── StackOnMultipleML ──
            var stackMulti = new LLSubtreeTemplate("StackOnMultipleML");
            stackMulti.Steps.Add(new LLStep("MoveTo") { Parameters = { ["target"] = "{pos}", ["robot"] = "{client}" } });
            stackMulti.Steps.Add(new LLStep("Lower") { Parameters = { ["robot"] = "{client}", ["obj"] = "{obj}" } });
            stackMulti.Steps.Add(new LLStep("OpenGripper") { Parameters = { ["robot"] = "{client}" } });
            stackMulti.Steps.Add(new LLStep("Retract") { Parameters = { ["robot"] = "{client}" } });
            _templates["StackOnMultipleML"] = stackMulti;

            // ── TravelML ──
            var travel = new LLSubtreeTemplate("TravelML");
            travel.Steps.Add(new LLStep("MoveToLL", MoveType.MoveJ) { Parameters = { ["target"] = "{to}", ["robot"] = "{client}" } });
            _templates["TravelML"] = travel;

            // ── NailingML ──
            var nailing = new LLSubtreeTemplate("NailingML");
            nailing.Steps.Add(new LLStep("MoveTo") { Parameters = { ["target"] = "{obj}", ["robot"] = "{client}" } });
            nailing.Steps.Add(new LLStep("ActivateTool") { Parameters = { ["robot"] = "{client}", ["tool"] = "{tool}" } });
            nailing.Steps.Add(new LLStep("Nail") { Parameters = { ["obj"] = "{obj}", ["target"] = "{base}", ["tool"] = "{tool}" } });
            nailing.Steps.Add(new LLStep("DeactivateTool") { Parameters = { ["robot"] = "{client}", ["tool"] = "{tool}" } });
            _templates["NailingML"] = nailing;


            // ── EquipeML (tool change — equip) ──
            var equip = new LLSubtreeTemplate("EquipeML");
            equip.Steps.Add(new LLStep("MoveTo") { Parameters = { ["target"] = "{toolLoc}", ["robot"] = "{client}" } });
            equip.Steps.Add(new LLStep("EquipTool") { Parameters = { ["robot"] = "{client}", ["tool"] = "{tool}" } });
            _templates["EquipeML"] = equip;

            // ── DeequipML (tool change — de-equip) ──
            var deequip = new LLSubtreeTemplate("DeequipML");
            deequip.Steps.Add(new LLStep("MoveTo") { Parameters = { ["target"] = "{toolLoc}", ["robot"] = "{client}" } });
            deequip.Steps.Add(new LLStep("DeequipTool") { Parameters = { ["robot"] = "{client}", ["tool"] = "{tool}" } });
            _templates["DeequipML"] = deequip;

            // ── PlaceML ──
            var place = new LLSubtreeTemplate("PlaceML");
            place.Steps.Add(new LLStep("MoveTo") { Parameters = { ["target"] = "{pos}", ["robot"] = "{client}" } });
            place.Steps.Add(new LLStep("Lower") { Parameters = { ["robot"] = "{client}", ["obj"] = "{obj}" } });
            place.Steps.Add(new LLStep("OpenGripper") { Parameters = { ["robot"] = "{client}" } });
            place.Steps.Add(new LLStep("Retract") { Parameters = { ["robot"] = "{client}" } });
            _templates["PlaceML"] = place;

            // ── CloseToolML ──
            var closeTool = new LLSubtreeTemplate("CloseToolML");
            closeTool.Steps.Add(new LLStep("DeactivateTool") { Parameters = { ["robot"] = "{client}", ["tool"] = "{tool}" } });
            _templates["CloseToolML"] = closeTool;

            // ── InitializeML ──
            var init = new LLSubtreeTemplate("InitializeML");
            init.Steps.Add(new LLStep("Initialize") { Parameters = { ["robot"] = "{client}" } });
            _templates["InitializeML"] = init;
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
                SuccessCriteria.ALL
            );

            // Build executable LL action nodes for the NodeGraph
            var llActions = new List<PActionNode>();
            foreach (var step in template.Steps)
            {
                var resolvedParams = ResolveParameters(step.Parameters, mlParamStrings);
                var resolvedObjects = ResolveParameterObjects(step.Parameters, mlParamObjects);
                var stepName = $"{step.ActionName}_{mlAction.InstanceName}";

                PActionNode llNode = CreateLLNode(step, stepName, resolvedParams, resolvedObjects, linkedBlackboard);

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
            LogMessage($"✅ ServiceLLSubtreeInject: Attached LL subtree ({template.Steps.Count} steps) to {mlAction.InstanceName}");
        }

        // ──────────────────── LL node factory ────────────────────

        /// <summary>
        /// Creates the correct ExeAction (or falls back to LLActionNode) for a template step.
        /// </summary>
        private PActionNode CreateLLNode(LLStep step, string stepName, Dictionary<string, string> resolvedParams, Dictionary<string, object> resolvedObjects, Blackboard<FastName> blackboard)
        {
            switch (step.ActionName)
            {
                case "MoveToLL":
                    var target = resolvedParams.GetValueOrDefault("target", "unknown");
                    var moveType = step.MoveType ?? MoveType.MoveJ;
                    var moveNode = new MoveToLL(stepName, "", target, blackboard, moveType);
                    moveNode.MLInputs = resolvedObjects;
                    return moveNode;

                case "CloseGripperLL":
                    var gripNode = new CloseGripperLL(stepName, blackboard);
                    gripNode.MLInputs = resolvedObjects;
                    return gripNode;

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
            lock (LogLock)
            {
                try { File.AppendAllText(LogFilePath, logMessage + Environment.NewLine); }
                catch { /* swallow file write errors */ }
            }
        }
    }
}

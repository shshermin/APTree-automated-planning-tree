using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BehaviorTreeMainProject.Log;

namespace BehaviorTreeMainProject.Log.Services
{
    /// <summary>
    /// Logger for generating comprehensive CSV summaries of blackboard data
    /// </summary>
    public class BlackboardSummaryLogger : BaseLogger
    {
        private static BlackboardSummaryLogger? instance;
        private static readonly object lockObject = new object();
        
        // Phase tracking
        private bool isTreeTicking = false;
        private DateTime treeTickingStartTime;
        
        // Data collection for CSV
        private readonly Dictionary<string, int> beforeTickingCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, int> afterTickingCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, TimeSpan> generationTimes = new Dictionary<string, TimeSpan>();
        private readonly Dictionary<string, (string instance, int count)> mostCommonInstances = new Dictionary<string, (string, int)>();
        
        // Instance tracking for most common analysis
        private readonly Dictionary<string, Dictionary<string, int>> instanceTypeCounts = new Dictionary<string, Dictionary<string, int>>();

        public static BlackboardSummaryLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new BlackboardSummaryLogger();
                        }
                    }
                }
                return instance;
            }
        }

        private BlackboardSummaryLogger()
        {
            base.Initialize("BlackboardSummary", true, true);
            
            // Initialize tracking dictionaries
            InitializeTrackingDictionaries();
            
            WriteSectionHeader("📊 BLACKBOARD SUMMARY LOGGER INITIALIZED");
            WriteLog("Ready to track blackboard data for CSV summary generation");
        }

        private void InitializeTrackingDictionaries()
        {
            var categories = new[] { "PredicateTypes", "ActionTypes", "ParameterTypes", "ActionInstances", "ParameterInstances", "PredicateInstances", "PredicateNegationCount" };
            
            foreach (var category in categories)
            {
                beforeTickingCounts[category] = 0;
                afterTickingCounts[category] = 0;
                generationTimes[category] = TimeSpan.Zero;
                mostCommonInstances[category] = ("N/A", 0);
                instanceTypeCounts[category] = new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Mark the start of tree ticking phase
        /// </summary>
        public static void StartTreeTicking()
        {
            Instance.StartTreeTickingInternal();
        }

        private void StartTreeTickingInternal()
        {
            lock (lockObject)
            {
                if (!isTreeTicking)
                {
                    isTreeTicking = true;
                    treeTickingStartTime = DateTime.Now;
                    WriteLog("🌳 Tree ticking phase started - switching to 'after ticking' tracking");
                }
            }
        }

        /// <summary>
        /// Mark the end of tree ticking phase
        /// </summary>
        public static void EndTreeTicking()
        {
            Instance.EndTreeTickingInternal();
        }

        private void EndTreeTickingInternal()
        {
            lock (lockObject)
            {
                if (isTreeTicking)
                {
                    isTreeTicking = false;
                    var tickingDuration = DateTime.Now - treeTickingStartTime;
                    WriteLog($"🌳 Tree ticking phase ended - duration: {tickingDuration:hh\\:mm\\:ss\\.fff}");
                }
            }
        }

        /// <summary>
        /// Track a new type or instance creation
        /// </summary>
        public static void TrackCreation(string category, string instanceName, TimeSpan generationTime)
        {
            Instance.TrackCreationInternal(category, instanceName, generationTime);
        }

        private void TrackCreationInternal(string category, string instanceName, TimeSpan generationTime)
        {
            lock (lockObject)
            {
                // Determine if this is before or after ticking
                var targetDict = isTreeTicking ? afterTickingCounts : beforeTickingCounts;
                
                if (targetDict.ContainsKey(category))
                {
                    targetDict[category]++;
                    
                    // Track generation time
                    generationTimes[category] = generationTimes[category].Add(generationTime);
                    
                    // Track instance for most common analysis
                    if (!instanceTypeCounts[category].ContainsKey(instanceName))
                    {
                        instanceTypeCounts[category][instanceName] = 0;
                    }
                    instanceTypeCounts[category][instanceName]++;
                    
                    // Update most common instance
                    var currentMostCommon = mostCommonInstances[category];
                    var currentCount = instanceTypeCounts[category][instanceName];
                    if (currentCount > currentMostCommon.count)
                    {
                        mostCommonInstances[category] = (instanceName, currentCount);
                    }
                }
            }
        }

        /// <summary>
        /// Capture the current state of the blackboard (before ticking starts)
        /// </summary>
        public static void CaptureBlackboardState(Blackboard<FastName> blackboard)
        {
            Instance.CaptureBlackboardStateInternal(blackboard);
        }

        private void CaptureBlackboardStateInternal(Blackboard<FastName> blackboard)
        {
            lock (lockObject)
            {
                WriteLog("📊 Capturing blackboard state before ticking starts...");
                
                // Count predicate types
                var predicateTypes = blackboard.GetAllPredicateTypes();
                beforeTickingCounts["PredicateTypes"] = predicateTypes.Count;
                WriteLog($"   PredicateTypes: {predicateTypes.Count}");
                
                // Count action types
                var actionTypes = blackboard.GetAllActionTypes();
                beforeTickingCounts["ActionTypes"] = actionTypes.Count;
                WriteLog($"   ActionTypes: {actionTypes.Count}");
                
                // Count parameter types (using entity types)
                var parameterTypes = blackboard.GetAllEntityTypes();
                beforeTickingCounts["ParameterTypes"] = parameterTypes.Count;
                WriteLog($"   ParameterTypes: {parameterTypes.Count}");
                
                // Count action instances
                var actionInstances = blackboard.GetAllActions();
                beforeTickingCounts["ActionInstances"] = actionInstances.Count;
                WriteLog($"   ActionInstances: {actionInstances.Count}");
                
                // Count parameter instances (combine all entity instances)
                var elements = blackboard.GetAllElements();
                var agents = blackboard.GetAllAgents();
                var locations = blackboard.GetAllLocations();
                var layers = blackboard.GetAllLayers();
                var modules = blackboard.GetAllModules();
                var tools = blackboard.GetAllTools();
                var totalParameterInstances = elements.Count + agents.Count + locations.Count + layers.Count + modules.Count + tools.Count;
                beforeTickingCounts["ParameterInstances"] = totalParameterInstances;
                WriteLog($"   ParameterInstances: {totalParameterInstances} (Elements:{elements.Count}, Agents:{agents.Count}, Locations:{locations.Count}, Layers:{layers.Count}, Modules:{modules.Count}, Tools:{tools.Count})");
                
                // Count predicate instances (overall counts only)
                var predicateInstances = blackboard.GetAllPredicates();
                var totalNegationCount = predicateInstances.Count(p => p.not);
                
                // Keep overall counts for backward compatibility
                beforeTickingCounts["PredicateInstances"] = predicateInstances.Count;
                WriteLog($"   PredicateInstances (Total): {predicateInstances.Count}");
                
                beforeTickingCounts["PredicateNegationCount"] = totalNegationCount;
                WriteLog($"   PredicateNegationCount (Total): {totalNegationCount}");
                
                WriteLog("✅ Blackboard state captured successfully");
            }
        }

        /// <summary>
        /// Generate and export the comprehensive CSV summary
        /// </summary>
        public static void GenerateCSVSummary(Blackboard<FastName> blackboard)
        {
            Instance.GenerateCSVSummaryInternal(blackboard);
        }

        private void GenerateCSVSummaryInternal(Blackboard<FastName> blackboard)
        {
            lock (lockObject)
            {
                WriteSectionHeader("📊 BLACKBOARD CSV SUMMARY");
                
                // Collect current data from blackboard
                var currentData = CollectCurrentBlackboardData(blackboard);
                
                // Generate CSV content
                var csvContent = GenerateCSVContent(currentData);
                
                // Write CSV to log
                WriteLog("CSV Summary:");
                WriteLog(csvContent);
                
                // Also write to a separate CSV file
                WriteCSVToFile(csvContent);
            }
        }

        private Dictionary<string, object> CollectCurrentBlackboardData(Blackboard<FastName> blackboard)
        {
            var data = new Dictionary<string, object>();
            
            try
            {
                // Get counts from blackboard
                data["PredicateTypes"] = blackboard.GetAllPredicateTypes().Count;
                data["ActionTypes"] = blackboard.GetAllActionTypes().Count;
                data["ParameterTypes"] = blackboard.GetAllEntityTypes().Count;
                data["ActionInstances"] = blackboard.GetAllActions().Count;
                // Count parameter instances using the same method as before ticking
                var elements = blackboard.GetAllElements();
                var agents = blackboard.GetAllAgents();
                var locations = blackboard.GetAllLocations();
                var layers = blackboard.GetAllLayers();
                var modules = blackboard.GetAllModules();
                var tools = blackboard.GetAllTools();
                var totalParameterInstances = elements.Count + agents.Count + locations.Count + layers.Count + modules.Count + tools.Count;
                data["ParameterInstances"] = totalParameterInstances;
                data["PredicateInstances"] = blackboard.GetAllPredicates().Count;
                
                // Get predicate negation count from BlackboardTrackingLogger
                var (_, _, negations) = BlackboardTrackingLogger.GetCurrentCounts();
                data["PredicateNegationCount"] = negations;
                
                // Get predicate instances for most common analysis
                var predicateInstances = blackboard.GetAllPredicates();
                
                // Get most common instances and store them in the dictionary
                var mostCommonAction = GetMostCommonInstance(blackboard.GetAllActions());
                var mostCommonParameter = GetMostCommonParameterInstance(blackboard);
                var mostCommonPredicate = GetMostCommonInstance(blackboard.GetAllPredicates());
                
                // Store in the mostCommonInstances dictionary for CSV generation
                mostCommonInstances["ActionInstances"] = mostCommonAction;
                mostCommonInstances["ParameterInstances"] = mostCommonParameter;
                mostCommonInstances["PredicateInstances"] = mostCommonPredicate;
                
                
                data["MostCommonActionInstance"] = mostCommonAction;
                data["MostCommonParameterInstance"] = mostCommonParameter;
                data["MostCommonPredicateInstance"] = mostCommonPredicate;
                
            }
            catch (Exception ex)
            {
                WriteLog($"⚠️ Error collecting blackboard data: {ex.Message}");
            }
            
            return data;
        }

        private (string name, int count) GetMostCommonInstance<T>(Dictionary<FastName, T> dictionary)
        {
            if (dictionary.Count == 0) return ("N/A", 0);
            
            var typeCounts = new Dictionary<string, int>();
            
            foreach (var item in dictionary.Values)
            {
                string typeName = GetTypeName(item!);
                if (!typeCounts.ContainsKey(typeName))
                    typeCounts[typeName] = 0;
                typeCounts[typeName]++;
            }
            
            var mostCommon = typeCounts.OrderByDescending(x => x.Value).First();
            return (mostCommon.Key, mostCommon.Value);
        }

        private (string name, int count) GetMostCommonInstance<T>(List<T> list)
        {
            if (list.Count == 0) return ("N/A", 0);
            
            var typeCounts = new Dictionary<string, int>();
            
            foreach (var item in list)
            {
                string typeName = GetTypeName(item!);
                if (!typeCounts.ContainsKey(typeName))
                    typeCounts[typeName] = 0;
                typeCounts[typeName]++;
            }
            
            var mostCommon = typeCounts.OrderByDescending(x => x.Value).First();
            return (mostCommon.Key, mostCommon.Value);
        }

        private string GetTypeName(object item)
        {
            switch (item)
            {
                case Predicate predicate:
                    return predicate.PredicateTypeName;
                
                case PActionNode action:
                    return action.actionType.ToString();
                
                case FlowNode flowNode:
                    return flowNode.TypeName;
                
                case Element element:
                    return element.TypeName.ToString();
                
                case Agent agent:
                    return agent.TypeName.ToString();
                
                case Location location:
                    return location.TypeName.ToString();
                
                case Layer layer:
                    return layer.TypeName.ToString();
                
                case Module module:
                    return module.TypeName.ToString();
                
                case Tool tool:
                    return tool.TypeName.ToString();
                
                default:
                    return item.GetType().Name;
            }
        }

        private (string name, int count) GetMostCommonParameterInstance(Blackboard<FastName> blackboard)
        {
            var allParameters = new List<object>();
            allParameters.AddRange(blackboard.GetAllElements());
            allParameters.AddRange(blackboard.GetAllLocations());
            allParameters.AddRange(blackboard.GetAllAgents());
            allParameters.AddRange(blackboard.GetAllLayers());
            allParameters.AddRange(blackboard.GetAllModules());
            allParameters.AddRange(blackboard.GetAllTools());
            
            return GetMostCommonInstance(allParameters);
        }

        private string GenerateCSVContent(Dictionary<string, object> currentData)
        {
            var csv = new StringBuilder();
            
            // CSV Header
            csv.AppendLine("Category,Count,BeforeTicking,AfterTicking,GenerationTimeMs,AverageTimeMs,MostCommonInstance,MostCommonCount");
            
            // Standard categories
            var standardCategories = new[] { "PredicateTypes", "ActionTypes", "ParameterTypes", "ActionInstances", "ParameterInstances", "PredicateInstances", "PredicateNegationCount" };
            
            // Use only standard categories (no individual predicate types)
            var allCategories = standardCategories.ToList();
            
            foreach (var category in allCategories)
            {
                var beforeCount = beforeTickingCounts.ContainsKey(category) ? beforeTickingCounts[category] : 0;
                var afterCount = afterTickingCounts.ContainsKey(category) ? afterTickingCounts[category] : 0;
                var totalCount = beforeCount + afterCount;
                var generationTimeMs = generationTimes.ContainsKey(category) ? generationTimes[category].TotalMilliseconds : 0;
                var mostCommon = mostCommonInstances.ContainsKey(category) ? mostCommonInstances[category] : ("N/A", 0);
                
                // Get current count from blackboard if available
                var currentCount = currentData.ContainsKey(category) ? currentData[category] : totalCount;
                
                // Calculate average time per instance
                var currentCountInt = Convert.ToInt32(currentCount);
                var averageTimeMs = currentCountInt > 0 ? generationTimeMs / currentCountInt : 0;
                
                csv.AppendLine($"{category},{currentCount},{beforeCount},{afterCount},{generationTimeMs:F2},{averageTimeMs:F2},{mostCommon.Item1},{mostCommon.Item2}");
            }
            
            return csv.ToString();
        }

        private void WriteCSVToFile(string csvContent)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var csvFilePath = $"WrittenLogs/BlackboardSummary_{timestamp}.csv";
                
                System.IO.File.WriteAllText(csvFilePath, csvContent, Encoding.UTF8);
                WriteLog($"📄 CSV summary written to: {csvFilePath}");
            }
            catch (Exception ex)
            {
                WriteLog($"⚠️ Error writing CSV file: {ex.Message}");
            }
        }

        /// <summary>
        /// Close the logger
        /// </summary>
        public new static void Close()
        {
            Instance.CloseInternal();
        }

        private void CloseInternal()
        {
            WriteSectionHeader("🏁 BLACKBOARD SUMMARY LOGGER CLOSED");
            base.Close();
        }
    }
}

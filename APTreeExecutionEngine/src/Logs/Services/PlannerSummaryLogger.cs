using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BehaviorTreeMainProject.Log;

namespace BehaviorTreeMainProject.Log.Services
{
    /// <summary>
    /// Logger for generating comprehensive CSV summaries of planner statistics
    /// </summary>
    public class PlannerSummaryLogger : BaseLogger
    {
        private static PlannerSummaryLogger instance;
        private static readonly object lockObject = new object();

        public static PlannerSummaryLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new PlannerSummaryLogger();
                        }
                    }
                }
                return instance;
            }
        }

        private PlannerSummaryLogger()
        {
            base.Initialize("PlannerSummary", true, true);
            
            WriteSectionHeader("📊 PLANNER SUMMARY LOGGER INITIALIZED");
            WriteLog("Ready to generate planner statistics CSV summary");
        }

        /// <summary>
        /// Generate and export the comprehensive planner CSV summary
        /// </summary>
        public static void GenerateCSVSummary()
        {
            Instance.GenerateCSVSummaryInternal();
        }

        private void GenerateCSVSummaryInternal()
        {
            lock (lockObject)
            {
                WriteSectionHeader("📊 PLANNER STATISTICS CSV SUMMARY");
                
                // Get planner data from ExecutionSummaryLogger
                var plannerData = GetPlannerStatisticsFromExecutionSummary();
                
                // Generate CSV content
                var csvContent = GenerateCSVContent(plannerData);
                
                // Write CSV to log
                WriteLog("CSV Summary:");
                WriteLog(csvContent);
                
                // Also write to a separate CSV file
                WriteCSVToFile(csvContent);
            }
        }

        private Dictionary<string, PlannerStats> GetPlannerStatisticsFromExecutionSummary()
        {
            var plannerStats = new Dictionary<string, PlannerStats>();
            
            try
            {
                WriteLog("🔍 Attempting to access ExecutionSummaryLogger planning metrics...");
                
                // Access ExecutionSummaryLogger's private planningServiceMetrics
                var executionSummary = ExecutionSummaryLogger.Instance;
                var planningMetricsField = typeof(ExecutionSummaryLogger).GetField("planningServiceMetrics", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                WriteLog($"🔍 Planning metrics field found: {planningMetricsField != null}");
                
                if (planningMetricsField != null)
                {
                    var planningMetricsRaw = planningMetricsField.GetValue(executionSummary);
                    WriteLog($"🔍 Planning metrics raw data: {planningMetricsRaw != null}");
                    
                    if (planningMetricsRaw != null)
                    {
                        // Use reflection to iterate over the dictionary
                        var dictionaryType = planningMetricsRaw.GetType();
                        var getEnumeratorMethod = dictionaryType.GetMethod("GetEnumerator");
                        var enumerator = getEnumeratorMethod.Invoke(planningMetricsRaw, null);
                        var moveNextMethod = enumerator.GetType().GetMethod("MoveNext");
                        var currentProperty = enumerator.GetType().GetProperty("Current");
                        
                        int itemCount = 0;
                        while ((bool)moveNextMethod.Invoke(enumerator, null))
                        {
                            itemCount++;
                            var current = currentProperty.GetValue(enumerator);
                            var keyProperty = current.GetType().GetProperty("Key");
                            var valueProperty = current.GetType().GetProperty("Value");
                            
                            var serviceName = keyProperty.GetValue(current)?.ToString();
                            var metrics = valueProperty.GetValue(current);
                            
                            WriteLog($"🔍 Processing item {itemCount}: Service={serviceName}");
                            
                            // Extract planner type and statistics using reflection
                            var plannerTypeProperty = metrics.GetType().GetProperty("PlannerType");
                            var totalCallsProperty = metrics.GetType().GetProperty("TotalCalls");
                            var successfulCallsProperty = metrics.GetType().GetProperty("SuccessfulCalls");
                            var totalPlannerTimeProperty = metrics.GetType().GetProperty("TotalPlannerTime");
                            var totalActionsProperty = metrics.GetType().GetProperty("TotalActionsGenerated");
                            
                            if (plannerTypeProperty != null && totalCallsProperty != null && 
                                successfulCallsProperty != null && totalPlannerTimeProperty != null && totalActionsProperty != null)
                            {
                                                            var plannerType = plannerTypeProperty.GetValue(metrics)?.ToString() ?? "Unknown";
                            var totalCalls = (int)totalCallsProperty.GetValue(metrics);
                            var successfulCalls = (int)successfulCallsProperty.GetValue(metrics);
                            var totalPlannerTime = (TimeSpan)totalPlannerTimeProperty.GetValue(metrics); // This will be TotalPlannerTime
                            var totalActions = (int)totalActionsProperty.GetValue(metrics);
                            
                            // Get TotalServiceTime using reflection
                            var totalServiceTimeProperty = metrics.GetType().GetProperty("TotalServiceTime");
                            var totalServiceTime = totalServiceTimeProperty != null ? (TimeSpan)totalServiceTimeProperty.GetValue(metrics) : totalPlannerTime;
                                
                                // Group by planner type (FF, ENHSP, LAMA-FIRST)
                                if (!plannerStats.ContainsKey(plannerType))
                                {
                                    plannerStats[plannerType] = new PlannerStats
                                    {
                                        PlannerType = plannerType,
                                        TotalCalls = 0,
                                        SuccessfulCalls = 0,
                                        TotalPlannerTime = TimeSpan.Zero,
                                        TotalServiceTime = TimeSpan.Zero,
                                        TotalActionsGenerated = 0
                                    };
                                }
                                
                                var stats = plannerStats[plannerType];
                                stats.TotalCalls += totalCalls;
                                stats.SuccessfulCalls += successfulCalls;
                                stats.TotalPlannerTime += totalPlannerTime;
                                stats.TotalServiceTime += totalServiceTime;
                                stats.TotalActionsGenerated += totalActions;
                            }
                        }
                        
                        WriteLog($"🔍 Total items processed: {itemCount}");
                    }
                    else
                    {
                        WriteLog("🔍 Planning metrics raw data is null");
                    }
                }
                else
                {
                    WriteLog("🔍 Planning metrics field is null");
                }
            }
            catch (Exception ex)
            {
                WriteLog($"⚠️ Error collecting planner statistics: {ex.Message}");
            }
            
            WriteLog($"🔍 Final planner stats count: {plannerStats.Count}");
            foreach (var kvp in plannerStats)
            {
                WriteLog($"🔍 Planner: {kvp.Key}, Calls: {kvp.Value.TotalCalls}, Success: {kvp.Value.SuccessfulCalls}");
            }
            
            return plannerStats;
        }

        private string GenerateCSVContent(Dictionary<string, PlannerStats> plannerData)
        {
            var csv = new StringBuilder();
            
            // CSV Header
            csv.AppendLine("PlannerType,CallCount,SuccessfulCalls,SuccessRate,AveragePlannerTimeMs,AverageServiceTimeMs,AveragePlanSize,TotalPlannerTimeMs,TotalServiceTimeMs");
            
            // Calculate totals
            var totalCalls = plannerData.Values.Sum(p => p.TotalCalls);
            var totalSuccessfulCalls = plannerData.Values.Sum(p => p.SuccessfulCalls);
            var totalPlannerTime = TimeSpan.FromMilliseconds(plannerData.Values.Sum(p => p.TotalPlannerTime.TotalMilliseconds));
            var totalServiceTime = TimeSpan.FromMilliseconds(plannerData.Values.Sum(p => p.TotalServiceTime.TotalMilliseconds));
            var totalActions = plannerData.Values.Sum(p => p.TotalActionsGenerated);
            
            // CSV Rows for each planner
            foreach (var kvp in plannerData.OrderBy(x => x.Key))
            {
                var plannerType = kvp.Key;
                var stats = kvp.Value;
                
                var successRate = stats.TotalCalls > 0 ? (double)stats.SuccessfulCalls / stats.TotalCalls * 100 : 0;
                var averagePlannerTime = stats.TotalCalls > 0 ? stats.TotalPlannerTime.TotalMilliseconds / stats.TotalCalls : 0;
                var averageServiceTime = stats.TotalCalls > 0 ? stats.TotalServiceTime.TotalMilliseconds / stats.TotalCalls : 0;
                var averagePlanSize = stats.SuccessfulCalls > 0 ? (double)stats.TotalActionsGenerated / stats.SuccessfulCalls : 0;
                
                csv.AppendLine($"{plannerType},{stats.TotalCalls},{stats.SuccessfulCalls},{successRate:F2}%,{averagePlannerTime:F2},{averageServiceTime:F2},{averagePlanSize:F2},{stats.TotalPlannerTime.TotalMilliseconds:F2},{stats.TotalServiceTime.TotalMilliseconds:F2}");
            }
            
            // Add totals row
            var overallSuccessRate = totalCalls > 0 ? (double)totalSuccessfulCalls / totalCalls * 100 : 0;
            var overallAveragePlannerTime = totalCalls > 0 ? totalPlannerTime.TotalMilliseconds / totalCalls : 0;
            var overallAverageServiceTime = totalCalls > 0 ? totalServiceTime.TotalMilliseconds / totalCalls : 0;
            var overallAveragePlanSize = totalSuccessfulCalls > 0 ? (double)totalActions / totalSuccessfulCalls : 0;
            
            csv.AppendLine($"TOTAL,{totalCalls},{totalSuccessfulCalls},{overallSuccessRate:F2}%,{overallAveragePlannerTime:F2},{overallAverageServiceTime:F2},{overallAveragePlanSize:F2},{totalPlannerTime.TotalMilliseconds:F2},{totalServiceTime.TotalMilliseconds:F2}");
            
            return csv.ToString();
        }

        private void WriteCSVToFile(string csvContent)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var csvFilePath = $"WrittenLogs/PlannerSummary_{timestamp}.csv";
                
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
            WriteSectionHeader("🏁 PLANNER SUMMARY LOGGER CLOSED");
            base.Close();
        }

        /// <summary>
        /// Planner statistics data structure
        /// </summary>
        private class PlannerStats
        {
            public string PlannerType { get; set; }
            public int TotalCalls { get; set; }
            public int SuccessfulCalls { get; set; }
            public TimeSpan TotalPlannerTime { get; set; } // External planner time only
            public TimeSpan TotalServiceTime { get; set; } // Total service time including NodeGraph generation
            public int TotalActionsGenerated { get; set; }
        }
    }
}

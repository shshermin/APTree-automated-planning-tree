using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BehaviorTreeMainProject.Log.Services;

namespace AIPlanning
{
    public class RestPlannerCommunicator : IPlannerCommunicator
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        
        /// <summary>
        /// sends http requests to planning services
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public RestPlannerCommunicator(string baseUrl)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(120); // Longer timeout for planning
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        }
        
        public async Task<PlanningResult> SendPlanningRequestAsync(IPlanningRequest request)
        {
            try
            {
                LoggingService.LogInfo($"🔧 RestPlannerCommunicator: Sending request to {_baseUrl}/plan");
                
                // Serialize request to JSON with polymorphic support
                var json = JsonSerializer.Serialize(request, request.GetType(), new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                LoggingService.LogInfo($"🔧 RestPlannerCommunicator: Request JSON:\n{json}");
                
                // Create HTTP content
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // Send POST request
                var response = await _httpClient.PostAsync($"{_baseUrl}/plan", content);
                
                // Read response
                var responseJson = await response.Content.ReadAsStringAsync();
                LoggingService.LogInfo($"🔧 RestPlannerCommunicator: Response JSON:\n{responseJson}");
                
                // DEBUG: Show the raw response structure
                LoggingService.LogInfo($"🔍 RestPlannerCommunicator: RAW PYTHON SERVICE RESPONSE:");
                LoggingService.LogInfo($"📋 Response length: {responseJson?.Length ?? 0} characters");
                LoggingService.LogInfo($"📋 Response preview: {responseJson?.Substring(0, Math.Min(500, responseJson.Length))}");
                LoggingService.LogInfo($"📋 Full response:");
                LoggingService.LogInfo("=".PadRight(80, '='));
                LoggingService.LogInfo(responseJson);
                LoggingService.LogInfo("=".PadRight(80, '='));
                
                if (response.IsSuccessStatusCode)
                {
                    // Deserialize successful response
                    var result = JsonSerializer.Deserialize<PlanningResult>(responseJson, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    // DEBUG: Show what's in the PlanningResult
                    LoggingService.LogInfo($"🔍 RestPlannerCommunicator: PLANNING RESULT CONTENTS:");
                    LoggingService.LogInfo($"📋 Success: {result.Success}");
                    LoggingService.LogInfo($"📋 Plan length: {result.Plan?.Length ?? 0} characters");
                    LoggingService.LogInfo($"📋 Plan preview: {result.Plan?.Substring(0, Math.Min(300, result.Plan.Length))}");
                    LoggingService.LogInfo($"📋 Error: {result.Error ?? "None"}");
                    LoggingService.LogInfo($"📋 Planning time: {result.PlanningTimeSeconds} seconds");
                    LoggingService.LogInfo($"📋 Plan length: {result.PlanLength} actions");
                    LoggingService.LogInfo($"📋 Planner used: {result.PlannerUsed}");
                    LoggingService.LogInfo($"📋 Full Plan content:");
                    LoggingService.LogInfo("=".PadRight(80, '='));
                    LoggingService.LogInfo(result.Plan);
                    LoggingService.LogInfo("=".PadRight(80, '='));
                    
                    LoggingService.LogSuccess($"✅ RestPlannerCommunicator: Planning completed successfully");
                    return result;
                }
                else
                {
                    // Handle HTTP error
                    LoggingService.LogError($"❌ RestPlannerCommunicator: HTTP {response.StatusCode}: {response.ReasonPhrase}");
                    return new PlanningResult 
                    { 
                        Success = false, 
                        Error = $"HTTP {response.StatusCode}: {response.ReasonPhrase} - {responseJson}"
                    };
                }
            }
            catch (TaskCanceledException ex)
            {
                LoggingService.LogError($"❌ RestPlannerCommunicator: Request timeout: {ex.Message}");
                return new PlanningResult 
                { 
                    Success = false, 
                    Error = $"Planning request timed out: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ RestPlannerCommunicator: Error: {ex.Message}");
                return new PlanningResult 
                { 
                    Success = false, 
                    Error = $"Failed to communicate with planning service: {ex.Message}"
                };
            }
        }
        
        public bool IsAvailable()
        {
            try
            {
                // Simple health check
                var response = _httpClient.GetAsync($"{_baseUrl}/health").Result;
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        
        public string GetPlannerName()
        {
            // Extract planner name from the base URL for better identification
            var uri = new Uri(_baseUrl);
            var path = uri.AbsolutePath.Trim('/');
            if (path.Contains("/"))
            {
                var segments = path.Split('/');
                return $"REST_{segments[segments.Length - 1].ToUpper()}_Planner";
            }
            return "REST_Generic_Planner";
        }
        
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}

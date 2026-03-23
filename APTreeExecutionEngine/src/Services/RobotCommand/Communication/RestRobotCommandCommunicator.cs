using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BehaviorTreeMainProject.Log.Services;

namespace RobotCommand
{
    public class RestRobotCommandCommunicator
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public RestRobotCommandCommunicator(string baseUrl)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        }

        public async Task<RobotCommandResult> SendCommandAsync(RobotCommandRequest request)
        {
            try
            {
                LoggingService.LogInfo($"🤖 RestRobotCommandCommunicator: Sending command to {_baseUrl}{request.Endpoint}");

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                LoggingService.LogInfo($"🤖 RestRobotCommandCommunicator: Request JSON:\n{json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}{request.Endpoint}", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                LoggingService.LogInfo($"🤖 RestRobotCommandCommunicator: Response JSON:\n{responseJson}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<RobotCommandResult>(responseJson, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    LoggingService.LogSuccess($"✅ RestRobotCommandCommunicator: Command completed - Success: {result.Success}");
                    return result;
                }
                else
                {
                    LoggingService.LogError($"❌ RestRobotCommandCommunicator: HTTP {response.StatusCode}: {response.ReasonPhrase}");
                    return new RobotCommandResult
                    {
                        Success = false,
                        Error = $"HTTP {response.StatusCode}: {response.ReasonPhrase} - {responseJson}"
                    };
                }
            }
            catch (TaskCanceledException ex)
            {
                LoggingService.LogError($"❌ RestRobotCommandCommunicator: Request timeout: {ex.Message}");
                return new RobotCommandResult
                {
                    Success = false,
                    Error = $"Request timeout: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"❌ RestRobotCommandCommunicator: Error: {ex.Message}");
                return new RobotCommandResult
                {
                    Success = false,
                    Error = $"Communication error: {ex.Message}"
                };
            }
        }
    }
}

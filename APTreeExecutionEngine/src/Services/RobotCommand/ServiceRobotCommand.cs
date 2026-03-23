using System;
using System.Threading.Tasks;
using BehaviorTreeMainProject.Log.Services;

namespace RobotCommand
{
    /// <summary>
    /// Service that sends robot movement commands via REST API to the Flask service.
    /// Mirrors ServicePlanning's async-over-sync execution pattern.
    /// Attach one instance per LL action node to handle its robot command.
    /// </summary>
    public class ServiceRobotCommand : Service
    {
        private readonly RestRobotCommandCommunicator _communicator;
        private readonly RobotCommandRequest _request;

        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        public bool IsExecuting { get; private set; } = false;
        public bool HasCompleted { get; private set; } = false;
        public bool WasSuccessful { get; private set; } = false;
        public string LastError { get; private set; }
        public RobotCommandResult LastResult { get; private set; }

        public ServiceRobotCommand(IBehaviorTree owningTree, RestRobotCommandCommunicator communicator, RobotCommandRequest request)
            : base(owningTree)
        {
            _communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        /// <summary>
        /// Alternative constructor that allows setting the tree later (via SetOwiningTree).
        /// </summary>
        public ServiceRobotCommand(RestRobotCommandCommunicator communicator, RobotCommandRequest request)
            : base(null)
        {
            _communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public override bool OnEvaluate(float InDeltaTime)
        {
            if (HasCompleted)
            {
                LoggingService.LogInfo($"⏭️ ServiceRobotCommand: Command already completed (Success: {WasSuccessful})");
                return WasSuccessful;
            }

            if (IsExecuting)
            {
                LoggingService.LogInfo($"⏳ ServiceRobotCommand: Command already in progress, waiting...");
                return true;
            }

            StartTime = DateTime.Now;
            IsExecuting = true;

            LoggingService.LogInfo($"🚀 ServiceRobotCommand: Sending move command - {_request.InitialPosition} → {_request.FinalPosition}");

            try
            {
                var result = Task.Run(async () => await _communicator.SendCommandAsync(_request)).Result;
                LastResult = result;

                EndTime = DateTime.Now;
                IsExecuting = false;
                HasCompleted = true;

                if (result.Success)
                {
                    WasSuccessful = true;
                    LoggingService.LogSuccess($"✅ ServiceRobotCommand: Move completed in {(EndTime - StartTime).TotalSeconds:F2}s - {result.Message}");
                    return true;
                }
                else
                {
                    WasSuccessful = false;
                    LastError = result.Error;
                    LoggingService.LogError($"❌ ServiceRobotCommand: Move failed - {result.Error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                EndTime = DateTime.Now;
                IsExecuting = false;
                HasCompleted = true;
                WasSuccessful = false;
                LastError = ex.Message;
                LoggingService.LogError($"❌ ServiceRobotCommand: Exception - {ex.Message}");
                return false;
            }
        }
    }
}

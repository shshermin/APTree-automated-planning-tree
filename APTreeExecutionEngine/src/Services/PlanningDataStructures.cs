using System;
using System.Collections.Generic;

namespace PlanningDataStructures
{
    // Base interface for all planning requests
    public interface IPlanningRequest
    {
        string PlanningType { get; }
    }

    // PDDL-specific request
    public class PDDLPlanningRequest : IPlanningRequest
    {
        public string PlanningType => "PDDL";
        public string DomainFile { get; set; }
        public string ProblemFile { get; set; }
        public string PlannerPath { get; set; }
        public string PlannerName { get; set; } = "ENHSP";  // New: planner selection
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxPlanLength { get; set; } = 20;
        
       
        
        // New constructor with planner selection
        public PDDLPlanningRequest(string InDomainFile, string InProblemFile, string InPlannerPath, string InPlannerName, int InTimeoutSeconds = 30, int InMaxPlanLength = 20)
        {
            DomainFile = InDomainFile;
            ProblemFile = InProblemFile;
            PlannerPath = InPlannerPath;
            PlannerName = InPlannerName;
            TimeoutSeconds = InTimeoutSeconds;
            MaxPlanLength = InMaxPlanLength;
        }
    }

    // GOAP-specific request
    public class GOAPPlanningRequest : IPlanningRequest
    {
        public string PlanningType => "GOAP";
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxPlanLength { get; set; } = 20;
        
        // GOAP-specific state and goals (key-value pairs)
        public Dictionary<string, object> InitialState { get; set; } = new Dictionary<string, object>();  // World state as key-value pairs
        public Dictionary<string, object> Goals { get; set; } = new Dictionary<string, object>();         // Goal state as key-value pairs
        public List<string> AvailableActions { get; set; } = new List<string>();            // GOAP needs available actions
        
        // Construction domain specific properties
        public string Domain { get; set; } = "Construction";  // Domain type (Construction, Manufacturing, etc.)
        public bool EnableDebugLogging { get; set; } = false;  // Enable detailed logging
        public float HeuristicWeight { get; set; } = 1.0f;    // A* heuristic weight for GOAP
        
        // Action definitions for GOAP (optional - can be defined in the planner)
        public List<GOAPActionDefinition> ActionDefinitions { get; set; } = new List<GOAPActionDefinition>();
        
        // Validation and constraints
        public bool ValidatePreconditions { get; set; } = true;  // Validate action preconditions
        public bool ValidatePostconditions { get; set; } = true; // Validate action postconditions
        public int MaxSearchDepth { get; set; } = 50;            // Maximum search depth for GOAP
    }
    
    // GOAP Action Definition for construction domain
    public class GOAPActionDefinition
    {
        public string Name { get; set; } = "";
        public float Cost { get; set; } = 1.0f;
        public Dictionary<string, object> Preconditions { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Postconditions { get; set; } = new Dictionary<string, object>();
        public string Description { get; set; } = "";
        
        // Construction domain specific
        public string ToolRequired { get; set; } = "";  // Required tool for this action
        public string ObjectType { get; set; } = "";    // Type of object this action works with
        public bool IsSequential { get; set; } = true;  // Whether this action must be sequential
    }

    // StateChart-specific request
    public class StateChartPlanningRequest : IPlanningRequest
    {
        public string PlanningType => "StateChart";
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxPlanLength { get; set; } = 20;
        
        // StateChart-specific state and goals
        public string CurrentState { get; set; }   // Current state machine state
        public string TargetState { get; set; }    // Target state to reach
        public List<string> AvailableTransitions { get; set; }  // Available state transitions
    }

    // Reinforcement Learning request
    public class RLPlanningRequest : IPlanningRequest
    {
        public string PlanningType => "RL";
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxSteps { get; set; } = 100;
        
        // RL-specific parameters
        public string EnvironmentState { get; set; }  // Current environment state
        public string Objective { get; set; }         // Learning objective
        public Dictionary<string, object> Parameters { get; set; }  // RL parameters (epsilon, learning rate, etc.)
    }

    
    // Response classes for receiving from external planner
    public class PlanningResult
    {
        public bool Success { get; set; }
        public string Plan { get; set; } // Plan as string (like NodeGraph format)
        public string Error { get; set; } // Error as string
        public double PlanningTimeSeconds { get; set; }
        public int PlanLength { get; set; }
        public string PlannerUsed { get; set; }
    }

    // Enum for planning types
    public enum PlanningType
    {
        PDDL,
        GOAP,
        StateChart,
        ReinforcementLearning
    }
}

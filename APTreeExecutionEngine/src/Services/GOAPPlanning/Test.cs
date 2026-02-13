using System;
using System.Collections.Generic;
using System.Linq;

namespace BehaviorTreeMainProject.Services.GOAPPlanning
{
    /// <summary>
    /// Simple GOAP example demonstrating the concept with a robot agent
    /// This can be adapted to use the Mountain GOAP library once the correct API is known
    /// </summary>
    public class RobotGOAPExample
    {
        public static void RunExample()
        {
            Console.WriteLine("🤖 Starting Robot GOAP Example...\n");

            // Create the robot agent
            var robot = CreateRobotAgent();

            // Set initial state
            var initialState = new Dictionary<string, object>
            {
                ["robot_location"] = "workshop",
                ["robot_has_tool"] = false,
                ["robot_holding_item"] = null,
                ["workshop_has_wrench"] = true,
                ["workshop_has_screwdriver"] = true,
                ["warehouse_has_box"] = true,
                ["warehouse_has_manual"] = true,
                ["assembly_line_has_components"] = true
            };

            // Set the agent's initial state
            robot.CurrentState = new Dictionary<string, object>(initialState);

            // Set goals - robot wants to be at assembly line with a tool and holding components
            var goals = new List<Goal>
            {
                new Goal("robot_location", "assembly_line"),
                new Goal("robot_has_tool", true),
                new Goal("robot_holding_item", "components")
            };

            robot.Goals = goals;

            Console.WriteLine("📋 Initial State:");
            PrintState(initialState);

            Console.WriteLine("\n🎯 Goals:");
            foreach (var goal in goals)
            {
                Console.WriteLine($"  - {goal}");
            }

            Console.WriteLine("\n🚀 Starting GOAP Planning...\n");

            // Run the agent
            for (int i = 0; i < 10; i++)
            {
                robot.Step();
                
                Console.WriteLine($"\n--- Step {i + 1} ---");
                Console.WriteLine($"Current State:");
                PrintState(robot.CurrentState);
                
                if (robot.Goals.Count == 0)
                {
                    Console.WriteLine("✅ All goals achieved!");
                    break;
                }
                
                System.Threading.Thread.Sleep(1000); // Pause to see the progression
            }

            // Print action execution summary
            robot.PrintActionSummary();

            Console.WriteLine("\n🏁 Robot GOAP Example Complete!");
        }

            /// <summary>
    /// Simple test method to demonstrate the GOAP example
    /// </summary>
    public static void Test()
    {
        Console.WriteLine("🧪 Testing Robot GOAP Example...\n");
        RunExample();
    }
    


        private static RobotAgent CreateRobotAgent()
        {
            var agent = new RobotAgent();

            // Add actions to the agent
            agent.AddAction(new TravelAction());
            agent.AddAction(new EquipAction());
            agent.AddAction(new DeequipAction());
            agent.AddAction(new PickupAction());

            return agent;
        }

        private static void PrintState(Dictionary<string, object> state)
        {
            foreach (var kvp in state)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
        }
    }

    /// <summary>
    /// Simple goal representation
    /// </summary>
    public class Goal
    {
        public string StateKey { get; set; }
        public object DesiredValue { get; set; }

        public Goal(string stateKey, object desiredValue)
        {
            StateKey = stateKey;
            DesiredValue = desiredValue;
        }

        public override string ToString()
        {
            return $"{StateKey} = {DesiredValue}";
        }
    }

    /// <summary>
    /// Simple action representation
    /// </summary>
    public abstract class Action
    {
        public string Name { get; set; }
        public float Cost { get; set; }

        protected Action(string name, float cost = 1.0f)
        {
            Name = name;
            Cost = cost;
        }

        public abstract bool CanExecute(Dictionary<string, object> state);
        public abstract Dictionary<string, object> Execute(Dictionary<string, object> state);
        public abstract Dictionary<string, object> GetPreconditions();
        public abstract Dictionary<string, object> GetPostconditions();
    }

    /// <summary>
    /// Travel action - robot moves from one location to another
    /// </summary>
    public class TravelAction : Action
    {
        public TravelAction() : base("Travel", 1.0f) { }

        public override bool CanExecute(Dictionary<string, object> state)
        {
            return state.ContainsKey("robot_location");
        }

        public override Dictionary<string, object> Execute(Dictionary<string, object> state)
        {
            var currentLocation = state["robot_location"].ToString();
            var locations = new List<string> { "workshop", "warehouse", "assembly_line" };
            
            // Try to travel to a location that helps achieve goals
            string targetLocation = null;
            
            // If we need to be at assembly_line, go there
            if (currentLocation != "assembly_line")
            {
                targetLocation = "assembly_line";
            }
            // If we need to be at warehouse to get components, go there
            else if (currentLocation == "assembly_line" && state.GetValueOrDefault("robot_holding_item") == null)
            {
                targetLocation = "warehouse";
            }
            // Otherwise, go to workshop to get tools
            else if (currentLocation != "workshop" && !(bool)state.GetValueOrDefault("robot_has_tool", false))
            {
                targetLocation = "workshop";
            }
            // If no specific target, go to a different location
            else
            {
                targetLocation = locations.FirstOrDefault(loc => loc != currentLocation);
            }
            
            if (targetLocation != null && targetLocation != currentLocation)
            {
                Console.WriteLine($"🚶 Robot traveling from {currentLocation} to {targetLocation}");
                var newState = new Dictionary<string, object>(state);
                newState["robot_location"] = targetLocation;
                return newState;
            }
            
            return state;
        }

        public override Dictionary<string, object> GetPreconditions()
        {
            return new Dictionary<string, object>();
        }

        public override Dictionary<string, object> GetPostconditions()
        {
            return new Dictionary<string, object>
            {
                ["robot_location"] = "new_location"
            };
        }
    }

    /// <summary>
    /// Equip action - robot equips a tool
    /// </summary>
    public class EquipAction : Action
    {
        public EquipAction() : base("Equip", 0.5f) { }

        public override bool CanExecute(Dictionary<string, object> state)
        {
            var hasTool = state.GetValueOrDefault("robot_has_tool", false);
            var location = state.GetValueOrDefault("robot_location", "").ToString();
            
            return !(bool)hasTool && (location == "workshop");
        }

        public override Dictionary<string, object> Execute(Dictionary<string, object> state)
        {
            var location = state["robot_location"].ToString();
            var tool = location == "workshop" ? "wrench" : "none";
            
            Console.WriteLine($"🔧 Robot equipping {tool} at {location}");
            
            var newState = new Dictionary<string, object>(state);
            newState["robot_has_tool"] = true;
            newState["robot_equipped_tool"] = tool;
            return newState;
        }

        public override Dictionary<string, object> GetPreconditions()
        {
            return new Dictionary<string, object>
            {
                ["robot_has_tool"] = false
            };
        }

        public override Dictionary<string, object> GetPostconditions()
        {
            return new Dictionary<string, object>
            {
                ["robot_has_tool"] = true
            };
        }
    }

    /// <summary>
    /// Deequip action - robot removes a tool
    /// </summary>
    public class DeequipAction : Action
    {
        public DeequipAction() : base("Deequip", 0.3f) { }

        public override bool CanExecute(Dictionary<string, object> state)
        {
            var hasTool = state.GetValueOrDefault("robot_has_tool", false);
            return (bool)hasTool;
        }

        public override Dictionary<string, object> Execute(Dictionary<string, object> state)
        {
            var equippedTool = state.GetValueOrDefault("robot_equipped_tool", "unknown").ToString();
            Console.WriteLine($"🔧 Robot deequipping {equippedTool}");
            
            var newState = new Dictionary<string, object>(state);
            newState["robot_has_tool"] = false;
            newState["robot_equipped_tool"] = null;
            return newState;
        }

        public override Dictionary<string, object> GetPreconditions()
        {
            return new Dictionary<string, object>
            {
                ["robot_has_tool"] = true
            };
        }

        public override Dictionary<string, object> GetPostconditions()
        {
            return new Dictionary<string, object>
            {
                ["robot_has_tool"] = false
            };
        }
    }

    /// <summary>
    /// Pickup action - robot picks up an item
    /// </summary>
    public class PickupAction : Action
    {
        public PickupAction() : base("Pickup", 0.5f) { }

        public override bool CanExecute(Dictionary<string, object> state)
        {
            var holdingItem = state.GetValueOrDefault("robot_holding_item");
            var location = state.GetValueOrDefault("robot_location", "").ToString();
            
            return holdingItem == null && (location == "warehouse" || location == "assembly_line");
        }

        public override Dictionary<string, object> Execute(Dictionary<string, object> state)
        {
            var location = state["robot_location"].ToString();
            var item = location == "warehouse" ? "box" : "components";
            
            Console.WriteLine($"📦 Robot picking up {item} at {location}");
            
            var newState = new Dictionary<string, object>(state);
            newState["robot_holding_item"] = item;
            return newState;
        }

        public override Dictionary<string, object> GetPreconditions()
        {
            return new Dictionary<string, object>
            {
                ["robot_holding_item"] = null
            };
        }

        public override Dictionary<string, object> GetPostconditions()
        {
            return new Dictionary<string, object>
            {
                ["robot_holding_item"] = "item"
            };
        }
    }

    /// <summary>
    /// Simple robot agent that can execute actions and work towards goals
    /// </summary>
    public class RobotAgent
    {
        public Dictionary<string, object> CurrentState { get; set; } = new();
        public List<Goal> Goals { get; set; } = new();
        public List<Action> Actions { get; set; } = new();
        public List<string> ExecutedActions { get; set; } = new(); // Track executed actions

        public void AddAction(Action action)
        {
            Actions.Add(action);
        }

        public void Step()
        {
            // Simple planning: find an action that can be executed and helps achieve a goal
            foreach (var goal in Goals.ToList())
            {
                var currentValue = CurrentState.GetValueOrDefault(goal.StateKey);
                
                // Check if goal is already achieved
                if (Equals(currentValue, goal.DesiredValue))
                {
                    Console.WriteLine($"✅ Goal achieved: {goal}");
                    Goals.Remove(goal);
                    continue;
                }

                // Find an action that can help achieve this goal
                foreach (var action in Actions)
                {
                    if (action.CanExecute(CurrentState))
                    {
                        var postconditions = action.GetPostconditions();
                        if (postconditions.ContainsKey(goal.StateKey))
                        {
                            Console.WriteLine($"🎯 Executing {action.Name} to achieve goal: {goal}");
                            CurrentState = action.Execute(CurrentState);
                            ExecutedActions.Add($"{action.Name} (Goal: {goal})"); // Track the action
                            return;
                        }
                    }
                }
            }

            // If no specific goal action found, try to find actions that help with prerequisites
            foreach (var goal in Goals.ToList())
            {
                if (goal.StateKey == "robot_location" && goal.DesiredValue.ToString() == "assembly_line")
                {
                    // Try to travel to assembly_line
                    var travelAction = Actions.OfType<TravelAction>().FirstOrDefault();
                    if (travelAction != null && travelAction.CanExecute(CurrentState))
                    {
                        Console.WriteLine($"🎯 Executing Travel to get closer to goal: {goal}");
                        CurrentState = travelAction.Execute(CurrentState);
                        ExecutedActions.Add($"Travel (Prerequisite: {goal})");
                        return;
                    }
                }
                else if (goal.StateKey == "robot_has_tool" && (bool)goal.DesiredValue)
                {
                    // Try to equip a tool
                    var equipAction = Actions.OfType<EquipAction>().FirstOrDefault();
                    if (equipAction != null && equipAction.CanExecute(CurrentState))
                    {
                        Console.WriteLine($"🎯 Executing Equip to get closer to goal: {goal}");
                        CurrentState = equipAction.Execute(CurrentState);
                        ExecutedActions.Add($"Equip (Prerequisite: {goal})");
                        return;
                    }
                }
                else if (goal.StateKey == "robot_holding_item" && goal.DesiredValue.ToString() == "components")
                {
                    // Try to pickup components
                    var pickupAction = Actions.OfType<PickupAction>().FirstOrDefault();
                    if (pickupAction != null && pickupAction.CanExecute(CurrentState))
                    {
                        Console.WriteLine($"🎯 Executing Pickup to get closer to goal: {goal}");
                        CurrentState = pickupAction.Execute(CurrentState);
                        ExecutedActions.Add($"Pickup (Prerequisite: {goal})");
                        return;
                    }
                }
            }

            // If no specific goal action found, execute a random valid action
            var validActions = Actions.Where(a => a.CanExecute(CurrentState)).ToList();
            if (validActions.Any())
            {
                var randomAction = validActions[new Random().Next(validActions.Count)];
                Console.WriteLine($"🔄 Executing random action: {randomAction.Name}");
                CurrentState = randomAction.Execute(CurrentState);
                ExecutedActions.Add($"{randomAction.Name} (Random)"); // Track the action
            }
        }

        /// <summary>
        /// Get a summary of all executed actions
        /// </summary>
        public void PrintActionSummary()
        {
            Console.WriteLine("\n📋 ACTION EXECUTION SUMMARY:");
            Console.WriteLine("=".PadRight(50, '='));
            
            if (ExecutedActions.Count == 0)
            {
                Console.WriteLine("No actions were executed.");
                return;
            }

            Console.WriteLine($"Total actions executed: {ExecutedActions.Count}");
            Console.WriteLine();
            
            for (int i = 0; i < ExecutedActions.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {ExecutedActions[i]}");
            }
            
            Console.WriteLine();
            Console.WriteLine("📊 Action Statistics:");
            var actionCounts = ExecutedActions
                .Select(action => action.Split(' ')[0]) // Get just the action name
                .GroupBy(name => name)
                .Select(group => new { Action = group.Key, Count = group.Count() })
                .OrderByDescending(x => x.Count);
                
            foreach (var stat in actionCounts)
            {
                Console.WriteLine($"   {stat.Action}: {stat.Count} times");
            }
        }
    }
}


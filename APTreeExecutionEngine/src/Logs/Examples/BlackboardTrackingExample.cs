using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject.Log.Examples
{
    /// <summary>
    /// Example demonstrating how to use the BlackboardTrackingLogger
    /// This shows how to track new types, instances, and predicate negations
    /// </summary>
    public static class BlackboardTrackingExample
    {
        public static void RunExample()
        {
            // The logger is automatically initialized when first accessed
            // No need to call Initialize() explicitly
            
            // Example of logging different types of blackboard events
            LogExampleEvents();
            
            // Close the logger and generate statistics
            BlackboardTrackingLogger.Close();
            
            Console.WriteLine($"📁 Blackboard tracking log saved to: {BlackboardTrackingLogger.GetLogFilePath()}");
        }
        
        private static void LogExampleEvents()
        {
            // Log new types being added to blackboard
            BlackboardTrackingLogger.LogNewType("PickUpML", "Action", "Machine learning action for picking up objects");
            BlackboardTrackingLogger.LogNewType("PlaceML", "Action", "Machine learning action for placing objects");
            BlackboardTrackingLogger.LogNewType("IsObjectGraspable", "Predicate", "Predicate to check if object can be grasped");
            BlackboardTrackingLogger.LogNewType("ObjectPosition", "Parameter", "Parameter for object position coordinates");
            
            // Log new instances being created
            BlackboardTrackingLogger.LogNewInstance("PickUpML_r1_ng1", "PickUpML", "RootComposite", "Instance for robot 1, node group 1");
            BlackboardTrackingLogger.LogNewInstance("PlaceML_r1_ng2", "PlaceML", "RootComposite", "Instance for robot 1, node group 2");
            BlackboardTrackingLogger.LogNewInstance("IsObjectGraspable_check1", "IsObjectGraspable", "TravelML", "Graspability check for travel action");
            BlackboardTrackingLogger.LogNewInstance("ObjectPosition_target1", "ObjectPosition", "PickUpML_r1_ng1", "Target position for pickup action");
            
            // Log predicate negation changes
            BlackboardTrackingLogger.LogPredicateNegation("IsObjectGraspable_check1", false, true, "TravelML", "Object became graspable");
            BlackboardTrackingLogger.LogPredicateNegation("IsObjectGraspable_check1", true, false, "PickUpML", "Object no longer graspable");
            BlackboardTrackingLogger.LogPredicateNegation("IsObjectGraspable_check2", false, true, "PlaceML", "New object is graspable");
            BlackboardTrackingLogger.LogPredicateNegation("IsObjectGraspable_check1", false, true, "TravelML", "Object graspable again");
            
            // Log more instances
            BlackboardTrackingLogger.LogNewInstance("PickUpML_r2_ng1", "PickUpML", "RootComposite", "Instance for robot 2, node group 1");
            BlackboardTrackingLogger.LogNewInstance("PlaceML_r2_ng2", "PlaceML", "RootComposite", "Instance for robot 2, node group 2");
            
            // Log more predicate negations
            BlackboardTrackingLogger.LogPredicateNegation("IsObjectGraspable_check2", true, false, "PlaceML", "Object no longer graspable");
            BlackboardTrackingLogger.LogPredicateNegation("IsObjectGraspable_check3", false, true, "PickUpML_r2_ng1", "New object is graspable");
        }
        
        /// <summary>
        /// Example of how to integrate with existing blackboard code
        /// (These would be called from your actual blackboard implementation)
        /// </summary>
        public static void ExampleIntegration()
        {
            // When adding a new type to blackboard:
            // BlackboardTrackingLogger.LogNewType("NewActionType", "Action", "Description of the action");
            
            // When creating a new instance:
            // BlackboardTrackingLogger.LogNewInstance("instanceName", "instanceType", "parentContext", "additional info");
            
            // When predicate negation changes:
            // BlackboardTrackingLogger.LogPredicateNegation("predicateName", oldValue, newValue, "context", "reason");
            
            // Get current statistics:
            var (types, instances, negations) = BlackboardTrackingLogger.GetCurrentCounts();
            Console.WriteLine($"Current: {types} types, {instances} instances, {negations} negations");
        }
    }
}

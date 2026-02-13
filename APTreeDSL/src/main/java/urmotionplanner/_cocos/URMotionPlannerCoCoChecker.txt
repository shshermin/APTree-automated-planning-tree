package urmotionplanner._cocos;

import urmotionplanner._ast.ASTURMotionPlannerDefinition;

/**
 * Checker that coordinates all Context Conditions for URMotionPlanner grammar
 */
public class URMotionPlannerCoCoChecker extends URMotionPlannerCoCoCheckerTOP {
    
    /**
     * Get a checker with all standard CoCos registered
     */
    public static URMotionPlannerCoCoChecker getCheckerForAllCoCos() {
        URMotionPlannerCoCoChecker checker = new URMotionPlannerCoCoChecker();
        
        // Register all CoCos
        checker.addCoCo(new ColladaObjectsMatchParametersCoCo());
        checker.addCoCo(new ParameterInstancesMatchTypesCoCo());
        checker.addCoCo(new PredicateInstancesMatchTypesCoCo());
        checker.addCoCo(new PDDLActionsMatchDefinitionsCoCo());
        
        return checker;
    }
}

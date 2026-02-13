package CoCos.DynamicBTFlowNode;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

import dynamicbtflownode._ast.ASTDynamicFlowNode;
import dynamicbtflownode._ast.ASTGraphNode;
import dynamicbtflownode._ast.ASTRelation;
import dynamicbtflownode._cocos.DynamicBTFlowNodeASTDynamicFlowNodeCoCo;
import behaviortree._ast.ASTActionNode;
import de.se_rwth.commons.logging.Log;

/**
 * CoCo rule: Detects shared resources between parallel action sequences.
 * 
 * When multiple actions in a NodeGraph use the same resource (e.g., robot r1),
 * they cannot execute in parallel or overlap. This CoCo identifies such conflicts.
 * 
 * Resources are identified by parameter values. If two actions share the same
 * parameter value (especially for entities like robots, grippers), they conflict.
 * 
 * Error code: 0xDF006
 */
public class SharedResourceConflictCoCo implements DynamicBTFlowNodeASTDynamicFlowNodeCoCo {

    private static final Set<Integer> RESOURCE_PARAM_POSITIONS = new HashSet<>();
    static {
        // The robot/client parameter position - looking at the extracted parameters,
        // it appears at index 1 for both PickUpHL(obj, loc, client) and PlaceHL(obj, loc, client)
        // But we extract (obj, client) = [obj, client] so it's at index 1
        RESOURCE_PARAM_POSITIONS.add(1); // robot/client position
    }

    @Override
    public void check(ASTDynamicFlowNode node) {
        if (node == null || node.getNodeGraph() == null) {
            return;
        }

        List<ASTGraphNode> graphNodes = node.getNodeGraph().getNodesList();
        System.out.println("[DEBUG] Checking NodeGraph in FlowNode '" + node.getName() + "' for shared resources");

        // Extract action information with graphNode references
        List<ActionInfo> actions = new ArrayList<>();
        for (ASTGraphNode graphNode : graphNodes) {
            if (graphNode.getNode() instanceof ASTActionNode) {
                ASTActionNode actionNode = (ASTActionNode) graphNode.getNode();
                ActionInfo info = extractActionInfo(actionNode, graphNode);
                if (info != null) {
                    actions.add(info);
                }
            }
        }

        // Check for shared resources between non-sequential actions
        for (int i = 0; i < actions.size(); i++) {
            for (int j = i + 1; j < actions.size(); j++) {
                ActionInfo action1 = actions.get(i);
                ActionInfo action2 = actions.get(j);

                // Check if these actions are sequential (connected by relations)
                if (isSequential(graphNodes, action1.graphNode, action2.graphNode)) {
                    System.out.println("[DEBUG]     '" + action1.instanceName + "' and '" + action2.instanceName + 
                        "' are sequential - no conflict");
                    continue;
                }

                // Check for shared resources
                Set<String> sharedResources = findSharedResources(action1, action2);
                if (!sharedResources.isEmpty()) {
                    Log.error(String.format("0xDF006 Shared resource conflict: Actions '%s' and '%s' in FlowNode '%s' " +
                        "both use resources %s. These actions cannot execute in parallel.",
                        action1.instanceName, action2.instanceName, node.getName(), sharedResources));
                }
            }
        }
    }

    /**
     * Extract action instance information including parameter values
     */
    private ActionInfo extractActionInfo(ASTActionNode actionNode, ASTGraphNode graphNode) {
        String instanceName = actionNode.getName();
        String actionType = extractActionType(actionNode);
        
        // Extract parameters using reflection on the action node object
        List<String> params = new ArrayList<>();
        Set<String> resourceParams = new HashSet<>();
        
        try {
            // Try to access parameters directly via reflection
            // Most AST nodes have getter methods like getObj(), getPos(), getClient(), getVg()
            Class<?> clazz = actionNode.getClass();
            
            // For PickUpHL: obj, pos, client
            // For PlaceHL: obj, pos, client
            // Resource is typically at index 2 (client/robot)
            
            // Try common parameter getter names
            String[] paramGetters = {"getObj", "getPos", "getClient", "getVg", "getElement", "getLocation", "getRobot"};
            int paramIndex = 0;
            
            for (String getterName : paramGetters) {
                try {
                    java.lang.reflect.Method method = clazz.getMethod(getterName);
                    Object value = method.invoke(actionNode);
                    if (value != null) {
                        String paramValue = value.toString();
                        params.add(paramValue);
                        System.out.println("[DEBUG]     Param[" + paramIndex + "]: " + paramValue + " (via " + getterName + ")");
                        
                        // Resource parameters are: client/robot, or vg/gripper
                        if (getterName.equals("getClient") || getterName.equals("getRobot") || getterName.equals("getVg")) {
                            resourceParams.add(paramValue);
                            System.out.println("[DEBUG]       -> Identified as RESOURCE");
                        }
                        paramIndex++;
                    }
                } catch (NoSuchMethodException e) {
                    // Method doesn't exist, continue
                }
            }
            
            if (params.isEmpty()) {
                System.out.println("[DEBUG]     No parameters extracted for " + instanceName);
            }
            
        } catch (Exception e) {
            System.out.println("[DEBUG] Error extracting parameters from " + instanceName + ": " + e.getMessage());
        }

        ActionInfo info = new ActionInfo(instanceName, actionType, params, resourceParams, graphNode);
        System.out.println("[DEBUG]   Action: " + info.instanceName + " (type: " + info.actionType + 
            ") | Parameters: " + info.parameters + " | Resources: " + info.resourceParams);
        return info;
    }

    /**
     * Check if two actions are connected by a relation (sequential)
     */
    private boolean isSequential(List<ASTGraphNode> graphNodes, ASTGraphNode node1, ASTGraphNode node2) {
        // Check if node1 has a relation to node2
        for (ASTGraphNode gnode : graphNodes) {
            if (gnode.getNode() == node1.getNode()) {
                for (ASTRelation rel : gnode.getSuccessorsList()) {
                    String targetName = rel.getTarget();
                    if (targetName.equals(((ASTActionNode) node2.getNode()).getName())) {
                        return true; // node1 -> node2
                    }
                }
            }
            if (gnode.getNode() == node2.getNode()) {
                for (ASTRelation rel : gnode.getSuccessorsList()) {
                    String targetName = rel.getTarget();
                    if (targetName.equals(((ASTActionNode) node1.getNode()).getName())) {
                        return true; // node2 -> node1
                    }
                }
            }
        }
        return false;
    }

    /**
     * Find shared resources between two actions
     */
    private Set<String> findSharedResources(ActionInfo action1, ActionInfo action2) {
        Set<String> shared = new HashSet<>(action1.resourceParams);
        shared.retainAll(action2.resourceParams);
        return shared;
    }

    /**
     * Extract action type from class name
     */
    private String extractActionType(ASTActionNode actionNode) {
        String className = actionNode.getClass().getSimpleName();
        if (className.startsWith("AST")) {
            return className.substring(3);
        }
        return className;
    }

    /**
     * Helper class to store action information
     */
    private static class ActionInfo {
        String instanceName;
        String actionType;
        List<String> parameters;
        Set<String> resourceParams;
        ASTGraphNode graphNode;

        ActionInfo(String instanceName, String actionType, List<String> parameters, Set<String> resourceParams, ASTGraphNode graphNode) {
            this.instanceName = instanceName;
            this.actionType = actionType;
            this.parameters = parameters;
            this.resourceParams = resourceParams;
            this.graphNode = graphNode;
        }
    }
}

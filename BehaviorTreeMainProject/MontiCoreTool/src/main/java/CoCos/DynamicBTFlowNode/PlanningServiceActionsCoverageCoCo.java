package CoCos.DynamicBTFlowNode;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import dynamicbtflownode._ast.ASTDynamicFlowNode;
import dynamicbtflownode._ast.ASTGraphNode;
import dynamicbtflownode._ast.ASTNodeGraph;
import dynamicbtflownode._cocos.DynamicBTFlowNodeASTDynamicFlowNodeCoCo;
import behaviortree._ast.ASTActionNode;
import behaviortree._ast.ASTService;
import de.se_rwth.commons.logging.Log;

/**
 * CoCo rule: Validates that all actions used in the behavior tree are defined in the planning service domain.
 * 
 * When a DynamicFlowNode has a PlanningService, checks that every ActionNode in the subtree
 * is defined in that service's PDDL domain.
 * 
 * Error code: 0xDF005
 */
public class PlanningServiceActionsCoverageCoCo implements DynamicBTFlowNodeASTDynamicFlowNodeCoCo {

    private static final String PLANNERS_PATH = "src/test/resources/valid/Planners/";
    private Map<String, Set<String>> domainDefinedActions = new HashMap<>();

    @Override
    public void check(ASTDynamicFlowNode node) {
        if (node == null || node.getServiceList().isEmpty()) {
            return; // No planning service, skip check
        }

        // Get the first planning service (assuming one per flow node)
        ASTService service = node.getServiceList().get(0);
        String serviceName = service.getName();
        String domainName = extractDomainName(service);
        
        System.out.println("[DEBUG] Checking PlanningService '" + serviceName + "' for domain: " + domainName);

        // Load defined actions for this domain (cached)
        if (!domainDefinedActions.containsKey(domainName)) {
            loadDomainActions(domainName);
        }

        Set<String> definedActions = domainDefinedActions.getOrDefault(domainName, new HashSet<>());
        System.out.println("[DEBUG]   Defined actions in domain: " + definedActions);

        // Get all action nodes in this flow node's subtree
        Set<String> usedActionTypes = new HashSet<>();
        collectActionTypes(node, usedActionTypes);
        System.out.println("[DEBUG]   Used action types in subtree: " + usedActionTypes);

        // Check for missing actions
        Set<String> undefinedActions = new HashSet<>(usedActionTypes);
        undefinedActions.removeAll(definedActions);

        if (!undefinedActions.isEmpty()) {
            Log.error(String.format("0xDF005 Planning service coverage violation: FlowNode '%s' uses actions %s " +
                "but domain '%s' only defines %s. Missing: %s",
                node.getName(), usedActionTypes, domainName, definedActions, undefinedActions));
        }
    }

    /**
     * Extract domain name from the planning service declaration.
     * The service reference just gives us the service name (e.g., MyPddlPlanner1).
     * We need to look up the actual domain in PDDLPlanner.bt.
     * For now, we extract from looking at the full domain definitions in the planner file.
     */
    private String extractDomainName(ASTService service) {
        // The service has a type/reference. We'll parse PDDLPlanner.bt directly
        // and extract the domain name from there
        try {
            String plannerFile = PLANNERS_PATH + "PDDLPlanner.bt";
            String content = new String(Files.readAllBytes(Paths.get(plannerFile)));
            
            // Pattern: Domain <name> ...
            Pattern domainPattern = Pattern.compile("Domain\\s+(\\w+)\\s+");
            Matcher matcher = domainPattern.matcher(content);
            if (matcher.find()) {
                return matcher.group(1);
            }
        } catch (IOException e) {
            System.err.println("[DEBUG] Could not read planner file: " + e.getMessage());
        }
        return "UnknownDomain";
    }

    /**
     * Load all action names defined in a PDDL domain file.
     */
    private void loadDomainActions(String domainName) {
        Set<String> actions = new HashSet<>();
        try {
            // Try to find the domain definition in PDDLPlanner.bt
            String plannerFile = PLANNERS_PATH + "PDDLPlanner.bt";
            String content = new String(Files.readAllBytes(Paths.get(plannerFile)));
            
            // Pattern to extract actions from: Domain PickAndPlaceHL ... {PickUpHL, PlaceHL}
            Pattern actionPattern = Pattern.compile(
                "Domain\\s+" + domainName + "\\s+.*?\\{([^}]*)\\}",
                Pattern.DOTALL | Pattern.CASE_INSENSITIVE
            );
            Matcher matcher = actionPattern.matcher(content);
            
            if (matcher.find()) {
                String actionsList = matcher.group(1);
                // Split by comma and whitespace
                for (String action : actionsList.split(",")) {
                    String trimmed = action.trim();
                    if (!trimmed.isEmpty()) {
                        actions.add(trimmed);
                        System.out.println("[DEBUG]     Found action: " + trimmed);
                    }
                }
            }
            
            domainDefinedActions.put(domainName, actions);
            System.out.println("[DEBUG] Loaded " + actions.size() + " actions for domain '" + domainName + "'");
            
        } catch (IOException e) {
            Log.error("Failed to load domain actions for '" + domainName + "': " + e.getMessage());
            domainDefinedActions.put(domainName, new HashSet<>());
        }
    }

    /**
     * Recursively collect all action types used in the flow node's subtree.
     */
    private void collectActionTypes(ASTDynamicFlowNode node, Set<String> actionTypes) {
        if (node.getNodeGraph() == null) {
            return;
        }

        for (ASTGraphNode graphNode : node.getNodeGraph().getNodesList()) {
            if (graphNode.getNode() instanceof ASTActionNode) {
                ASTActionNode actionNode = (ASTActionNode) graphNode.getNode();
                String actionType = extractActionType(actionNode);
                actionTypes.add(actionType);
                System.out.println("[DEBUG]     Found action instance: " + actionNode.getName() + " (type: " + actionType + ")");
            }
        }

        // Recursively check nested flow nodes
        for (ASTGraphNode graphNode : node.getNodeGraph().getNodesList()) {
            if (graphNode.getNode() instanceof ASTDynamicFlowNode) {
                ASTDynamicFlowNode nestedFlowNode = (ASTDynamicFlowNode) graphNode.getNode();
                collectActionTypes(nestedFlowNode, actionTypes);
            }
        }
    }

    /**
     * Extract action type from class name (ASTPickUpHL -> PickUpHL)
     */
    private String extractActionType(ASTActionNode actionNode) {
        String className = actionNode.getClass().getSimpleName();
        if (className.startsWith("AST")) {
            return className.substring(3);
        }
        return className;
    }
}

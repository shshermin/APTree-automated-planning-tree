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

import dynamicbtflownode._ast.ASTGraphNode;
import dynamicbtflownode._ast.ASTRelation;
import dynamicbtflownode._cocos.DynamicBTFlowNodeASTGraphNodeCoCo;
import behaviortree._ast.ASTActionNode;
import de.se_rwth.commons.logging.Log;

/**
 * CoCo rule: Validates causal links between actions.
 * 
 * For each relation between actions, checks that the source action's effects
 * satisfy the target action's preconditions. Preconditions and effects are
 * dynamically parsed from CRFActionTypes.bt.
 * 
 * Also builds a mapping of action instance names to their action types.
 * 
 * Error code: 0xDF004
 */
public class CausalLinkValidator implements DynamicBTFlowNodeASTGraphNodeCoCo {

    private static final String ACTION_TYPES_PATH = "src/test/resources/valid/CRFTypes/CRFActionTypes.bt";
    private static Map<String, ActionDef> actionTypeDefinitions = new HashMap<>();
    private static Map<String, String> actionInstanceToType = new HashMap<>();
    private static boolean initialized = false;

    @Override
    public void check(ASTGraphNode node) {
        if (node == null || node.getNode() == null) {
            return;
        }

        // Initialize action definitions on first use
        if (!initialized) {
            loadActionDefinitions();
            initialized = true;
        }

        // Get source action from this GraphNode
        if (!(node.getNode() instanceof ASTActionNode)) {
            return;
        }

        ASTActionNode sourceAction = (ASTActionNode) node.getNode();
        String sourceActionName = sourceAction.getName();
        String sourceActionType = getActionType(sourceAction);

        System.out.println("[DEBUG] Checking GraphNode for action: " + sourceActionName + " (type: " + sourceActionType + ")");

        // Check all relations from this action
        for (ASTRelation relation : node.getSuccessorsList()) {
            String targetActionName = relation.getTarget();
            System.out.println("[DEBUG]   -> Relation " + relation.getTemptype() + " to: " + targetActionName);

            validateCausalLink(sourceActionName, sourceActionType, targetActionName);
        }
    }

    private String getActionType(ASTActionNode actionNode) {
        // Get the class name to determine action type (e.g., ASTPickUpHL -> PickUpHL)
        String className = actionNode.getClass().getSimpleName();
        if (className.startsWith("AST")) {
            return className.substring(3);  // Remove "AST" prefix
        }
        return className;
    }

    private void validateCausalLink(String sourceActionName, String sourceActionType, String targetActionName) {
        // Get source action type definition
        ActionDef sourceActionDef = actionTypeDefinitions.get(sourceActionType);
        if (sourceActionDef == null) {
            System.out.println("[DEBUG]     Source action type '" + sourceActionType + "' not found in definitions");
            return;
        }

        // Get target action type (need to look it up by instance name)
        String targetActionType = actionInstanceToType.get(targetActionName);
        if (targetActionType == null) {
            System.out.println("[DEBUG]     Target action instance '" + targetActionName + "' type not found");
            return;
        }

        ActionDef targetActionDef = actionTypeDefinitions.get(targetActionType);
        if (targetActionDef == null) {
            System.out.println("[DEBUG]     Target action type '" + targetActionType + "' not found in definitions");
            return;
        }

        // Check if source's effects satisfy target's preconditions
        System.out.println("[DEBUG]     Source effects (positive): " + sourceActionDef.positiveEffects);
        System.out.println("[DEBUG]     Source effects (negative): " + sourceActionDef.negativeEffects);
        System.out.println("[DEBUG]     Target preconditions (positive): " + targetActionDef.positivePreconditions);
        System.out.println("[DEBUG]     Target preconditions (negative): " + targetActionDef.negativePreconditions);

        // Check that all positive preconditions are in source's positive effects
        Set<String> unsatisfiedPosPreconditions = new HashSet<>(targetActionDef.positivePreconditions);
        unsatisfiedPosPreconditions.removeAll(sourceActionDef.positiveEffects);

        // Check that all negative preconditions are either:
        // - in source's negative effects, OR
        // - NOT in source's positive effects
        Set<String> unsatisfiedNegPreconditions = new HashSet<>();
        for (String negPred : targetActionDef.negativePreconditions) {
            // Negative precondition is satisfied if:
            // 1. The predicate is in the negative effects (explicitly negated), OR
            // 2. The predicate is NOT in the positive effects
            if (!sourceActionDef.negativeEffects.contains(negPred) && 
                sourceActionDef.positiveEffects.contains(negPred)) {
                unsatisfiedNegPreconditions.add(negPred);
            }
        }

        if (!unsatisfiedPosPreconditions.isEmpty() || !unsatisfiedNegPreconditions.isEmpty()) {
            Set<String> allUnsatisfied = new HashSet<>();
            allUnsatisfied.addAll(unsatisfiedPosPreconditions);
            allUnsatisfied.addAll(unsatisfiedNegPreconditions);
            
            Log.error(String.format("0xDF004 Causal link violation: Action '%s' (type: %s) -> '%s' (type: %s). " +
                "Unsatisfied preconditions: %s",
                sourceActionName, sourceActionType, targetActionName, targetActionType, allUnsatisfied));
        } else {
            System.out.println("[DEBUG]     Causal link valid!");
        }
    }

    private void loadActionDefinitions() {
        try {
            String content = new String(Files.readAllBytes(Paths.get(ACTION_TYPES_PATH)));
            parseActionTypes(content);
            System.out.println("[DEBUG] Loaded " + actionTypeDefinitions.size() + " action type definitions from " + ACTION_TYPES_PATH);
        } catch (IOException e) {
            Log.error("Failed to load action definitions from " + ACTION_TYPES_PATH + ": " + e.getMessage());
        }
    }

    private void parseActionTypes(String content) {
        // Pattern to match: Define Action ActionName { ... Preconditions { ... } ... Effects { ... } ... }
        // Using a more flexible approach that handles nested braces
        Pattern actionPattern = Pattern.compile(
            "Define\\s+Action\\s+(\\w+)\\s*\\{",
            Pattern.DOTALL | Pattern.CASE_INSENSITIVE
        );

        // Find each action definition
        Matcher actionMatcher = actionPattern.matcher(content);
        System.out.println("[DEBUG] Searching for action definitions in CRFActionTypes...");

        int matchCount = 0;
        while (actionMatcher.find()) {
            String actionName = actionMatcher.group(1);
            int startPos = actionMatcher.end();
            
            // Find the matching closing brace for this action definition
            int braceCount = 1;
            int endPos = startPos;
            for (int i = startPos; i < content.length() && braceCount > 0; i++) {
                if (content.charAt(i) == '{') braceCount++;
                else if (content.charAt(i) == '}') braceCount--;
                if (braceCount == 0) endPos = i;
            }
            
            String actionBody = content.substring(startPos, endPos);
            
            // Extract Preconditions section
            Pattern precPattern = Pattern.compile("Preconditions\\s*\\{([^}]*)\\}", Pattern.CASE_INSENSITIVE);
            Matcher precMatcher = precPattern.matcher(actionBody);
            Map<String, Set<String>> preconditions = new HashMap<>();
            preconditions.put("positive", new HashSet<>());
            preconditions.put("negative", new HashSet<>());
            if (precMatcher.find()) {
                parsePredicateWithNegation(precMatcher.group(1), preconditions);
            }
            
            // Extract Effects section
            Pattern effPattern = Pattern.compile("Effects\\s*\\{([^}]*)\\}", Pattern.CASE_INSENSITIVE);
            Matcher effMatcher = effPattern.matcher(actionBody);
            Map<String, Set<String>> effects = new HashMap<>();
            effects.put("positive", new HashSet<>());
            effects.put("negative", new HashSet<>());
            if (effMatcher.find()) {
                parsePredicateWithNegation(effMatcher.group(1), effects);
            }
            
            if (!preconditions.get("positive").isEmpty() || !preconditions.get("negative").isEmpty() ||
                !effects.get("positive").isEmpty() || !effects.get("negative").isEmpty()) {
                actionTypeDefinitions.put(actionName, new ActionDef(actionName, preconditions, effects));
                System.out.println("[DEBUG] Parsed action type: " + actionName +
                    " | Preconditions (pos): " + preconditions.get("positive") +
                    " | Preconditions (neg): " + preconditions.get("negative") +
                    " | Effects (pos): " + effects.get("positive") +
                    " | Effects (neg): " + effects.get("negative"));
                matchCount++;
            }
        }
        System.out.println("[DEBUG] Found " + matchCount + " action type definitions");
    }

    private void parsePredicateWithNegation(String text, Map<String, Set<String>> result) {
        // Extract predicates, tracking whether they are negated
        // Pattern captures optional '!' and then the predicate name
        Pattern predPattern = Pattern.compile("(!?)\\s*(\\w+)\\s*\\(");
        Matcher predMatcher = predPattern.matcher(text);

        while (predMatcher.find()) {
            String negation = predMatcher.group(1);
            String pred = predMatcher.group(2).toLowerCase(); // Normalize to lowercase
            
            if (negation.equals("!")) {
                result.get("negative").add(pred);
            } else {
                result.get("positive").add(pred);
            }
        }
    }

    private Set<String> parsePredicate(String text) {
        Set<String> predicates = new HashSet<>();
        // Extract predicate names (with or without ! prefix)
        Pattern predPattern = Pattern.compile("!?\\s*(\\w+)\\s*\\(");
        Matcher predMatcher = predPattern.matcher(text);

        while (predMatcher.find()) {
            String pred = predMatcher.group(1);
            predicates.add(pred.toLowerCase()); // Normalize to lowercase
        }

        return predicates;
    }

    /**
     * Register an action instance to its type mapping.
     * Called during AST traversal to build the instance-to-type map.
     * 
     * @param instanceName The action instance name (e.g., "PickupPlate1")
     * @param typeName The action type name (e.g., "PickUpHL")
     */
    public static void registerActionInstance(String instanceName, String typeName) {
        actionInstanceToType.put(instanceName, typeName);
    }

    /**
     * Helper class to store action type definitions with positive and negative predicates
     */
    private static class ActionDef {
        String name;
        Set<String> positivePreconditions;
        Set<String> negativePreconditions;
        Set<String> positiveEffects;
        Set<String> negativeEffects;

        ActionDef(String name, Map<String, Set<String>> preconditions, Map<String, Set<String>> effects) {
            this.name = name;
            this.positivePreconditions = preconditions.get("positive");
            this.negativePreconditions = preconditions.get("negative");
            this.positiveEffects = effects.get("positive");
            this.negativeEffects = effects.get("negative");
        }
    }
}

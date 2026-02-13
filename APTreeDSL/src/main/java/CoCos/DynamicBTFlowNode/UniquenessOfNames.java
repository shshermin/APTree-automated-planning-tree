package CoCos.DynamicBTFlowNode;

import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;

import behaviortree._ast.ASTDecorator;
import behaviortree._ast.ASTService;
import behaviortree._cocos.BehaviorTreeASTDecoratorCoCo;
import behaviortree._cocos.BehaviorTreeASTServiceCoCo;
import de.se_rwth.commons.logging.Log;

/**
 * CoCo rule: Decorator and service names must be unique within a behavior tree.
 * 
 * Uses object identity to avoid processing the same node instance multiple times
 * during traversal (MontiCore's traverser may visit nodes through different paths).
 * 
 * Error code: 0xDF003
 */
public class UniquenessOfNames implements BehaviorTreeASTDecoratorCoCo, BehaviorTreeASTServiceCoCo {

    private static final ThreadLocal<Map<String, String>> nameRegistry = ThreadLocal.withInitial(HashMap::new);
    private static final ThreadLocal<Set<Integer>> visitedNodes = ThreadLocal.withInitial(HashSet::new);
    
    @Override
    public void check(ASTDecorator node) {
        if (node != null && node.getName() != null) {
            checkUniqueness(node.getName(), "Decorator", node);
        }
    }
    
    @Override
    public void check(ASTService node) {
        if (node != null && node.getName() != null) {
            checkUniqueness(node.getName(), "Service", node);
        }
    }
    
    private void checkUniqueness(String name, String type, Object node) {
        int nodeId = System.identityHashCode(node);
        Set<Integer> visited = visitedNodes.get();
        
        // Skip if we've already processed this exact node instance
        if (visited.contains(nodeId)) {
            return;
        }
        visited.add(nodeId);
        
        Map<String, String> registry = nameRegistry.get();
        de.monticore.ast.ASTNode astNode = (de.monticore.ast.ASTNode) node;
        String sourcePos = astNode.get_SourcePositionStart() != null ? 
            astNode.get_SourcePositionStart().toString() : "unknown";
        
        System.out.println("[DEBUG] Visiting " + type + ": '" + name + "' at " + sourcePos + " (id: " + nodeId + ")");
        
        if (registry.containsKey(name)) {
            String previousType = registry.get(name);
            System.out.println("[DEBUG] -> DUPLICATE FOUND: '" + name + "' was already registered as " + previousType);
            Log.error(
                String.format("0xDF003 Duplicate name '%s': Already used as a %s. Current element is a %s.", 
                    name, previousType, type),
                astNode.get_SourcePositionStart()
            );
        } else {
            registry.put(name, type);
            System.out.println("[DEBUG] -> Added to registry as " + type);
        }
    }
}

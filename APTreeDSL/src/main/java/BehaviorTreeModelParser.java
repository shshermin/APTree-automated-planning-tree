import behaviortree.BehaviorTreeMill;
import behaviortree._ast.ASTBehaviorTree;
import behaviortree._ast.ASTBTNode;
import behaviortree._ast.ASTFlowNode;
import behaviortree._ast.ASTActionNode;
import behaviortree._ast.ASTDecorator;
import behaviortree._ast.ASTService;
import behaviortree._parser.BehaviorTreeParser;
import de.se_rwth.commons.logging.Log;

import java.io.*;
import java.util.Optional;

/**
 * BehaviorTreeModelParser - Parses .bt files using the BehaviorTree grammar.
 *
 * Usage:
 *   gradle runBTModelParser -PmodelName=BehaviorTree.bt
 *
 * Default model: BehaviorTree.bt
 */
public class BehaviorTreeModelParser {

    private static final String BASE_DIR = "src/test/resources/valid/behavior_trees/";
    private static final String DEFAULT_FILE = "BehaviorTree.bt";

    public static void main(String[] args) {
        try {
            System.out.println("=== BEHAVIOR TREE MODEL PARSER ===\n");

            // Initialize MontiCore mill and logging
            BehaviorTreeMill.init();
            Log.init();
            Log.enableFailQuick(false);

            // Get model file from args or use default
            String fileName = args.length > 0 ? args[0] : DEFAULT_FILE;
            String modelPath = BASE_DIR + fileName;

            System.out.println("Parsing model: " + modelPath + "\n");

            ASTBehaviorTree bt = parseModel(modelPath);

            if (bt != null) {
                System.out.println("SUCCESS: Parsed BehaviorTree '" + bt.getName() + "'\n");
                printTree(bt);
                System.out.println("\nPARSING COMPLETED SUCCESSFULLY");
            } else {
                System.err.println("ERROR: Failed to parse model");
            }

        } catch (Exception e) {
            System.err.println("ERROR: " + e.getMessage());
            e.printStackTrace();
        }
    }

    /**
     * Parse a BehaviorTree model file.
     * @param modelPath Path to the .bt file
     * @return Parsed ASTBehaviorTree or null if parsing failed
     */
    public static ASTBehaviorTree parseModel(String modelPath) throws IOException {
        File modelFile = new File(modelPath);
        if (!modelFile.exists()) {
            System.err.println("ERROR: Model file not found: " + modelPath);
            System.err.println("  Current working directory: " + System.getProperty("user.dir"));
            return null;
        }

        BehaviorTreeParser parser = BehaviorTreeMill.parser();
        Optional<ASTBehaviorTree> result = parser.parse(modelPath);

        if (result.isPresent()) {
            return result.get();
        } else {
            System.err.println("Parsing failed. Errors:");
            Log.getFindings().forEach(f ->
                System.err.println("  " + f.buildMsg())
            );
            return null;
        }
    }

    /**
     * Print the tree structure of a parsed BehaviorTree.
     */
    public static void printTree(ASTBehaviorTree bt) {
        System.out.println("=== TREE STRUCTURE ===");
        System.out.println("BehaviorTree: " + bt.getName());

        ASTFlowNode root = bt.getRoot();
        printNode(root, 1);
    }

    /**
     * Recursively print a BTNode and its children.
     */
    private static void printNode(ASTBTNode node, int depth) {
        String indent = "  ".repeat(depth);
        String nodeType = node.getClass().getSimpleName().replace("AST", "");

        System.out.println(indent + nodeType + ": " + node.getName());

        // Print decorators
        if (node instanceof ASTFlowNode) {
            ASTFlowNode flowNode = (ASTFlowNode) node;
            for (ASTDecorator dec : flowNode.getDecoratorList()) {
                System.out.println(indent + "  [Decorator] " + dec.getName());
            }
            for (ASTService svc : flowNode.getServiceList()) {
                System.out.println(indent + "  [Service] " + svc.getName());
            }
            // Recurse into children
            for (ASTBTNode child : flowNode.getChildrenList()) {
                printNode(child, depth + 1);
            }
        } else if (node instanceof ASTActionNode) {
            ASTActionNode actionNode = (ASTActionNode) node;
            for (ASTDecorator dec : actionNode.getDecoratorList()) {
                System.out.println(indent + "  [Decorator] " + dec.getName());
            }
            for (ASTService svc : actionNode.getServiceList()) {
                System.out.println(indent + "  [Service] " + svc.getName());
            }
        }
    }
}

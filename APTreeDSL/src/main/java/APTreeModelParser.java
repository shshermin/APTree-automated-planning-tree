import behaviortree._ast.ASTActionNode;
import behaviortree._ast.ASTBTNode;
import behaviortree._ast.ASTDecorator;
import behaviortree._ast.ASTFlowNode;
import behaviortree._ast.ASTService;
import de.se_rwth.commons.logging.Log;
import dynamicbtflownode.DynamicBTFlowNodeMill;
import dynamicbtflownode._ast.ASTAPTree;
import dynamicbtflownode._ast.ASTDynamicFlowNode;
import dynamicbtflownode._ast.ASTFinalWorld;
import dynamicbtflownode._ast.ASTGraphNode;
import dynamicbtflownode._ast.ASTNodeGraph;
import dynamicbtflownode._ast.ASTRelation;

import java.io.*;
import java.util.Optional;

/**
 * APTreeModelParser - Parses APTree .bt models using the DynamicBTFlowNode grammar.
 *
 * This parser handles APTree models that contain DynamicFlowNodes, NodeGraphs,
 * ServicePlanning, temporal relations, etc.
 *
 * Usage:
 *   gradle runAPTreeModelParser
 *   gradle runAPTreeModelParser "-PmodelName=APTreeLivematFinal.bt"
 *
 * Default model: APTreeLiveMat.bt
 */
public class APTreeModelParser {

    private static final String BASE_DIR = "src/test/resources/valid/behavior_trees/";
    private static final String DEFAULT_FILE = "APTreeLiveMat.bt";

    public static void main(String[] args) {
        try {
            System.out.println("=== APTREE MODEL PARSER ===\n");

            // Initialize MontiCore mill and logging
            DynamicBTFlowNodeMill.init();
            Log.init();
            Log.enableFailQuick(false);

            // Get model file from args or use default
            String fileName = args.length > 0 ? args[0] : DEFAULT_FILE;
            String modelPath = BASE_DIR + fileName;

            System.out.println("Parsing model: " + modelPath + "\n");

            ASTFinalWorld finalWorld = parseModel(modelPath);

            if (finalWorld != null) {
                System.out.println("SUCCESS: Parsed " + finalWorld.getAPTreeList().size() + " behavior tree(s)\n");
                for (ASTAPTree tree : finalWorld.getAPTreeList()) {
                    printTree(tree);
                }
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
     * Parse an APTree model file and return the ASTFinalWorld.
     * @param modelPath Path to the .bt file
     * @return Parsed ASTFinalWorld or null if parsing failed
     */
    public static ASTFinalWorld parseModel(String modelPath) throws IOException {
        File modelFile = new File(modelPath);
        if (!modelFile.exists()) {
            System.err.println("ERROR: Model file not found: " + modelPath);
            System.err.println("  Current working directory: " + System.getProperty("user.dir"));
            return null;
        }

        Optional<ASTFinalWorld> result = DynamicBTFlowNodeMill.parser().parseFinalWorld(modelPath);

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
     * Print the full tree structure of a parsed APTree.
     */
    public static void printTree(ASTAPTree tree) {
        System.out.println("=== TREE STRUCTURE ===");
        System.out.println("BehaviorTree: " + tree.getName());

        ASTFlowNode root = tree.getRoot();
        printNode(root, 1);
    }

    /**
     * Recursively print a BTNode and its children.
     */
    private static void printNode(ASTBTNode node, int depth) {
        String indent = "  ".repeat(depth);
        String nodeType = node.getClass().getSimpleName().replace("AST", "");

        System.out.println(indent + nodeType + ": " + node.getName());

        // Handle DynamicFlowNode (FlowNode with NodeGraph, SuccessCriteria, etc.)
        if (node instanceof ASTDynamicFlowNode) {
            ASTDynamicFlowNode dynNode = (ASTDynamicFlowNode) node;

            // Print services and decorators
            for (ASTService svc : dynNode.getServiceList()) {
                System.out.println(indent + "  [Service] " + svc.getName());
            }
            for (ASTDecorator dec : dynNode.getDecoratorList()) {
                System.out.println(indent + "  [Decorator] " + dec.getName());
            }

            System.out.println(indent + "  SuccessCriteria: " + dynNode.getSuccri());
            System.out.println(indent + "  ChildType: " + dynNode.getChildType());

            // Print NodeGraph contents
            ASTNodeGraph nodeGraph = dynNode.getNodeGraph();
            if (nodeGraph != null) {
                printNodeGraph(nodeGraph, depth + 1);
            }

        } else if (node instanceof ASTFlowNode) {
            ASTFlowNode flowNode = (ASTFlowNode) node;
            for (ASTService svc : flowNode.getServiceList()) {
                System.out.println(indent + "  [Service] " + svc.getName());
            }
            for (ASTDecorator dec : flowNode.getDecoratorList()) {
                System.out.println(indent + "  [Decorator] " + dec.getName());
            }
            for (ASTBTNode child : flowNode.getChildrenList()) {
                printNode(child, depth + 1);
            }

        } else if (node instanceof ASTActionNode) {
            ASTActionNode actionNode = (ASTActionNode) node;
            for (ASTService svc : actionNode.getServiceList()) {
                System.out.println(indent + "  [Service] " + svc.getName());
            }
            for (ASTDecorator dec : actionNode.getDecoratorList()) {
                System.out.println(indent + "  [Decorator] " + dec.getName());
            }
        }
    }

    /**
     * Print the contents of a NodeGraph.
     */
    private static void printNodeGraph(ASTNodeGraph nodeGraph, int depth) {
        String indent = "  ".repeat(depth);

        if (nodeGraph.getNodesList().isEmpty()) {
            System.out.println(indent + "NodeGraph: (empty)");
            return;
        }

        System.out.println(indent + "NodeGraph:");
        for (ASTGraphNode graphNode : nodeGraph.getNodesList()) {
            printNode(graphNode.getNode(), depth + 1);

            // Print temporal relations
            for (ASTRelation rel : graphNode.getSuccessorsList()) {
                String relIndent = "  ".repeat(depth + 2);
                System.out.println(relIndent + "--[" + rel.getTemptype() + "]--> " + rel.getTarget());
            }
        }
    }
}

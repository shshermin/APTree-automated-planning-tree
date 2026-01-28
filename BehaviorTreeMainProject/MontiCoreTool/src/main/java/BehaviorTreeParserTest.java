import java.io.File;
import java.util.Optional;

import behaviortree.BehaviorTreeMill;
import behaviortree._ast.ASTBTNode;
import behaviortree._ast.ASTBehaviorTree;
import behaviortree._ast.ASTFlowNode;
import behaviortree._parser.BehaviorTreeParser;

public class BehaviorTreeParserTest {
    
    public static void main(String[] args) {
        try {
            System.out.println("=== BEHAVIOR TREE PARSER TEST ===");
            System.out.println("Parsing behavior tree model...\n");
            
            // Initialize MontiCore mill for the grammar
            BehaviorTreeMill.init();
            
            // Define the file to parse
            String filePath = "src/test/resources/valid/behavior_trees/BehaviorTree.bt";
            
            // Check if file exists
            File testFile = new File(filePath);
            if (!testFile.exists()) {
                System.err.println("✗ ERROR: Test file not found: " + filePath);
                System.err.println("   Current working directory: " + System.getProperty("user.dir"));
                System.err.println("   Please create the test file first.");
                return;
            }
            
            // Create parser instance
            BehaviorTreeParser parser = new BehaviorTreeParser();
            
            // Parse the file
            Optional<ASTBehaviorTree> result = parser.parse(filePath);
            
            if (result.isPresent()) {
                ASTBehaviorTree behaviorTree = result.get();
                System.out.println("✓ SUCCESS: Parsed behavior tree: '" + behaviorTree.getName() + "'\n");
                
                // Analyze the parsed tree
                analyzeBehaviorTree(behaviorTree);
                System.out.println("\n✓ PARSING COMPLETED SUCCESSFULLY");
                
            } else {
                System.out.println("✗ ERROR: Failed to parse " + filePath);
                System.out.println("   Please check the syntax of your behavior tree file.");
            }
            
        } catch (Exception e) {
            System.err.println("✗ EXCEPTION: " + e.getMessage());
            e.printStackTrace();
        }
    }
    
    public static void analyzeBehaviorTree(ASTBehaviorTree behaviorTree) {
        System.out.println("=== BEHAVIOR TREE STRUCTURE ===");
        System.out.println("Name: " + behaviorTree.getName());
        
        // Get root FlowNode
        ASTFlowNode rootNode = behaviorTree.getRoot();
        if (rootNode == null) {
            System.out.println("⚠ WARNING: No root FlowNode found");
            return;
        }
        
        System.out.println("Root Node Type: " + rootNode.getClass().getSimpleName());
        
        // Print tree structure
        System.out.println("\n=== TREE HIERARCHY ===");
        printNodeStructure(rootNode, 0);
    }
    
    private static void printNodeStructure(ASTBTNode node, int indentLevel) {
        String indent = createIndent(indentLevel);
        String nodeType = getNodeTypeName(node);
        String nodeName = getNodeName(node);
        
        System.out.println(indent + "├─ " + nodeType + (nodeName.isEmpty() ? "" : ": " + nodeName));
        
        // Print child nodes recursively - this will depend on your grammar structure
        // Adjust based on actual getter methods in your AST classes
        if (node instanceof ASTFlowNode) {
            ASTFlowNode flowNode = (ASTFlowNode) node;
            // Add logic to print child FlowNodes and other children if they exist
        }
    }
    
    private static String getNodeTypeName(ASTBTNode node) {
        String fullName = node.getClass().getSimpleName();
        // Remove "AST" prefix if present
        return fullName.startsWith("AST") ? fullName.substring(3) : fullName;
    }
    
    private static String getNodeName(ASTBTNode node) {
        // Try to get name property
        try {
            var method = node.getClass().getMethod("getName");
            Object nameObj = method.invoke(node);
            return nameObj != null ? nameObj.toString() : "";
        } catch (Exception e) {
            return "";
        }
    }
    
    // Helper method to create indentation
    private static String createIndent(int indentLevel) {
        StringBuilder indent = new StringBuilder();
        for (int i = 0; i < indentLevel; i++) {
            indent.append("  ");
        }
        return indent.toString();
    }
}

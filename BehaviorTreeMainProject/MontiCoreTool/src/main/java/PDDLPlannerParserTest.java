import planningservice._parser.PlanningServiceParser;
import planningservice._ast.ASTWorld;
import planningservice._ast.ASTPDDLPlannerService;
import planningservice.PlanningServiceMill;

import java.util.Optional;
import java.io.*;

/**
 * PDDLPlannerParserTest - Parses and displays PDDLPlannerService models
 */
public class PDDLPlannerParserTest {
    
    private static final String DEFAULT_PATH = "src/test/resources/valid/Planners/PDDLPlanner.bt";
    
    public static void main(String[] args) {
        try {
            System.out.println("=== PDDL PLANNER PARSER TEST ===");
            System.out.println("Parsing PDDLPlanner model...\n");
            
            // Initialize MontiCore mill for the grammar
            PlanningServiceMill.init();
            
            // Define the file to parse
            String filePath = args.length > 0 ? args[0] : DEFAULT_PATH;
            
            // Check if file exists
            File testFile = new File(filePath);
            if (!testFile.exists()) {
                System.err.println("✗ ERROR: Test file not found: " + filePath);
                System.err.println("   Current working directory: " + System.getProperty("user.dir"));
                return;
            }
            
            // Create parser instance
            PlanningServiceParser parser = new PlanningServiceParser();
            
            // Parse the file as World
            Optional<ASTWorld> result = parser.parse(filePath);
            
            if (result.isPresent()) {
                ASTWorld ast = result.get();
                System.out.println("✓ SUCCESS: Parsed file: " + filePath + "\n");
                
                // Display the parsed model
                // displayPlannerService(plannerService);
                
                System.out.println("\n✓ PARSING COMPLETED SUCCESSFULLY");
            } else {
                System.err.println("✗ FAILED: Could not parse model");
                if (parser.hasErrors()) {
                    System.err.println("   Parser errors occurred.");
                }
            }
            
        } catch (Exception e) {
            System.err.println("✗ ERROR: " + e.getMessage());
            e.printStackTrace();
        }
    }
    
    /**
     * Display the parsed PDDLPlannerService
     */
    public static void displayPlannerService(ASTPDDLPlannerService plannerService) {
        System.out.println("========================================");
        System.out.println("PDDL PLANNER SERVICE");
        System.out.println("========================================\n");
        
        System.out.println("Planner Name: " + plannerService.getPlanner());
        // Domain and problem are properties of the resolved PDDLPlanner symbol, 
        // which requires symbol table resolution not available in this simple print.
        
        System.out.println("\n========================================");
    }
}

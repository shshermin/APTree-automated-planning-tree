import crftypescon._parser.CRFTypesConParser;
import crftypescon._ast.ASTWorld;
import crftypesdef._ast.ASTProperty;
import crftypescon.CRFTypesConMill;
import de.se_rwth.commons.logging.Log;

import java.io.*;
import java.util.Optional;

/**
 * ConcreteBTInstanceParser - Parses CRFConcreteInstances.bt model
 * 
 * This parser reads instance definitions like:
 *   Beam beam1 (35.0 red)
 *   Robot robot1 (1)
 *   FirstPosition fp1 (1)
 * 
 * And validates them against the ConcreteBT grammar.
 */
public class ConcreteBTInstanceParser {

    private static final String DEFAULT_MODEL_PATH = "src/test/resources/valid/CRFConcrete/CRFConcreteInstances.bt";
    
    public static void main(String[] args) {
        try {
            System.out.println("=== CONCRETE BT INSTANCE PARSER ===");
            System.out.println("Parsing CRF Concrete Instances model...\n");
            
            // Initialize MontiCore mill for the grammar
            CRFTypesConMill.init();
            
            // Get model path from args or use default
            String modelPath = args.length > 0 ? args[0] : DEFAULT_MODEL_PATH;
            
            // Parse the model
            ASTWorld world = parseModel(modelPath);
            
            if (world != null) {
                System.out.println("✓ SUCCESS: Parsed model successfully\n");
                analyzeModel(world);
                System.out.println("\n✓ PARSING COMPLETED SUCCESSFULLY");
            } else {
                System.err.println("✗ ERROR: Failed to parse model");
                System.err.println("   Please check the syntax of your .bt file");
                System.err.println("   Model path: " + modelPath);
            }
            
        } catch (Exception e) {
            System.err.println("✗ ERROR: " + e.getMessage());
            e.printStackTrace();
        }
    }
    
    /**
     * Parse the ConcreteBT model file
     * @param modelPath Path to the .bt file
     * @return Parsed ASTWorld or null if parsing failed
     */
    public static ASTWorld parseModel(String modelPath) throws IOException {
        // Check if file exists
        File modelFile = new File(modelPath);
        if (!modelFile.exists()) {
            System.err.println("✗ ERROR: Model file not found: " + modelPath);
            System.err.println("   Current working directory: " + System.getProperty("user.dir"));
            return null;
        }
        
        System.out.println("Reading file: " + modelPath);
        
        // Create parser instance
        CRFTypesConParser parser = new CRFTypesConParser();
        
        // Parse the file - ConcreteBT extends CRFTypeDef which has World as root
        Optional<ASTWorld> result = parser.parse(modelPath);
        
        if (result.isPresent()) {
            return result.get();
        } else {
            System.err.println("✗ Parsing failed. Errors:");
            Log.getFindings().forEach(finding -> 
                System.err.println("   " + finding.buildMsg())
            );
            return null;
        }
    }
    
    /**
     * Analyze and display the parsed model
     * @param world The parsed AST
     */
    public static void analyzeModel(ASTWorld world) {
        System.out.println("=== MODEL ANALYSIS ===");
        
        int beamCount = 0;
        int plateCount = 0;
        int locationCount = 0;
        int robotCount = 0;
        int otherCount = 0;
        
        // Count different types of instances
        // Note: The actual AST structure depends on your grammar
        // You may need to adjust this based on how instances are represented
        
        if (world.getPropertyTypeDefinitionList() != null) {
            System.out.println("PropertyTypeDefinitions: " + world.getPropertyTypeDefinitionList().size());
        }
        
        if (world.getPropertyList() != null) {
            System.out.println("Properties: " + world.getPropertyList().size());
            for (ASTProperty prop : world.getPropertyList()) {
                System.out.println("  - " + prop.getName() + " : " + prop.getType().getName());
            }
        }
        
        // Check for Beam instances specifically
        System.out.println("\nSearching for concrete instances...");
        
        // The actual way to access instances depends on your grammar structure
        // If Beam, Robot, etc. are separate node types in the AST:
        // You would iterate through them here
        
        System.out.println("\nNote: To see concrete instances, the grammar must");
        System.out.println("      include them in the World production rule.");
        System.out.println("      Current ConcreteBT grammar extends CRFTypeDef's World rule.");
    }
    
    /**
     * Validate a specific instance exists and has correct structure
     * @param world The parsed model
     * @param instanceName Name to look for (e.g., "beam1")
     * @return true if found and valid
     */
    public static boolean validateInstance(ASTWorld world, String instanceName) {
        // Implementation depends on how instances are stored in the AST
        // This is a placeholder for instance validation logic
        return false;
    }
    
    /**
     * Get all instances of a specific type
     * @param world The parsed model
     * @param typeName Type to filter (e.g., "Beam", "Robot")
     * @return List of instances of that type
     */
    public static java.util.List<String> getInstancesByType(ASTWorld world, String typeName) {
        java.util.List<String> instances = new java.util.ArrayList<>();
        // Implementation depends on AST structure
        return instances;
    }
}

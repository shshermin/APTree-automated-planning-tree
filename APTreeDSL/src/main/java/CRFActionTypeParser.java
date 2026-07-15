import domaintypesdef._parser.DomainTypesDefParser;
import domaintypesdef._ast.ASTWorld;
import domaintypesdef.DomainTypesDefMill;

import java.io.*;
import java.nio.file.*;
import java.util.Optional;

/**
 * CRFActionTypeParser - Reads CRFActionTypes model.
 */
public class CRFActionTypeParser {

    private static final String BASE_DIR = "src/test/resources/valid/DomainTypes/";
    private static final String DEFAULT_FILE = "CRFActionTypes.bt";
    
    public static void main(String[] args) {
        try {
            System.out.println("=== CRF ACTION TYPE PARSER ===");
            
            // Initialize MontiCore mill
            DomainTypesDefMill.init();
            
            // Parse the CRFActionTypes model
            String fileName = args.length > 0 ? args[0] : DEFAULT_FILE;
            String modelPath = BASE_DIR + fileName;
            
            System.out.println("Parsing model: " + modelPath + "\n");
            
            ASTWorld world = parseModel(modelPath);
            
            System.out.println("\n✓ Model successfully parsed.");
            
        } catch (Exception e) {
            System.err.println(" ERROR: " + e.getMessage());
            e.printStackTrace();
        }
    }
    
    /**
     * Parse the CRFActionTypes model and return the ASTWorld
     */
    public static ASTWorld parseModel(String modelPath) throws IOException {
        // Check if file exists
        File modelFile = new File(modelPath);
        if (!modelFile.exists()) {
            throw new FileNotFoundException("Model file not found: " + modelPath);
        }
        
        // Create parser and parse
        DomainTypesDefParser parser = new DomainTypesDefParser();
        Optional<ASTWorld> result = parser.parse(modelPath);
        
        if (!result.isPresent()) {
            throw new RuntimeException("Failed to parse model: " + modelPath);
        }
        
        ASTWorld world = result.get();
        System.out.println(" Parsed model: " + modelPath);
        System.out.println("  Found " + world.getActionTypeDefinitionList().size() + " ActionTypeDefinitions");
        
        return world;
    }
}

import crftypescon.CRFTypesConMill;
import crftypescon._ast.ASTWorld;
import crftypescon._parser.CRFTypesConParser;
import de.se_rwth.commons.logging.Log;

import java.io.*;
import java.nio.file.*;
import java.util.Optional;

/**
 * CRFConPredicateParser - Parses predicate models (CRFTypesCon grammar)
 * 
 * Reads a predicate model file containing predicate instance/state definitions.
 */
public class CRFConPredicateParser {

    private static final String BASE_DIR = "src/test/resources/valid/CRFConcrete/";
    private static final String DEFAULT_FILE = "LiveMatInitialState.bt";
    
    public static void main(String[] args) {
        try {
            System.out.println("=== CRF CONCRETE PREDICATE PARSER ===");
            
            // Initialize MontiCore mill
            CRFTypesConMill.init();
            Log.init();
            Log.enableFailQuick(false);
            
            // Parse the predicate model
            String fileName = args.length > 0 ? args[0] : DEFAULT_FILE;
            String modelPath = BASE_DIR + fileName;
            
            System.out.println("Parsing model: " + modelPath + "\n");
            
            ASTWorld world = parseModel(modelPath);
            
            System.out.println("\n Model successfully parsed.");
            
        } catch (Exception e) {
            System.err.println(" ERROR: " + e.getMessage());
            e.printStackTrace();
        }
    }
    
    /**
     * Parse the predicate model and return the ASTWorld
     */
    public static ASTWorld parseModel(String modelPath) throws IOException {
        // Check if file exists
        File modelFile = new File(modelPath);
        if (!modelFile.exists()) {
            throw new FileNotFoundException("Model file not found: " + modelPath);
        }
        
        // Create parser and parse
        CRFTypesConParser parser = CRFTypesConMill.parser();
        Optional<ASTWorld> result = parser.parse(modelPath);
        
        if (!result.isPresent()) {
            throw new IOException("Failed to parse model: " + modelPath);
        }
        
        return result.get();
    }
}

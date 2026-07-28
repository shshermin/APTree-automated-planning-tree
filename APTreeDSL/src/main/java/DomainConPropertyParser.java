import domaintypescon.DomainTypesConMill;
import domaintypescon._ast.ASTWorld;
import domaintypescon._parser.DomainTypesConParser;
import de.se_rwth.commons.logging.Log;

import java.io.*;
import java.nio.file.*;
import java.util.Optional;

/**
 * DomainConPropertyParser - Parses concrete property instances (DomainTypesCon grammar)
 * 
 * Reads a DomainTypesCon file containing concrete property instance definitions.
 */
public class DomainConPropertyParser {

    private static final String BASE_DIR = "src/test/resources/valid/CRFConcrete/";
    private static final String DEFAULT_FILE = "LiveMatSetupObjects.bt";
    
    public static void main(String[] args) {
        try {
            System.out.println("=== CRF CONCRETE PROPERTY PARSER ===");
            
            // Initialize MontiCore mill
            DomainTypesConMill.init();
            Log.init();
            Log.enableFailQuick(false);
            
            // Parse the concrete property model
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
     * Parse the concrete property model and return the ASTWorld
     */
    public static ASTWorld parseModel(String modelPath) throws IOException {
        // Check if file exists
        File modelFile = new File(modelPath);
        if (!modelFile.exists()) {
            throw new FileNotFoundException("Model file not found: " + modelPath);
        }
        
        // Create parser and parse
        DomainTypesConParser parser = DomainTypesConMill.parser();
        Optional<ASTWorld> result = parser.parse(modelPath);
        
        if (!result.isPresent()) {
            throw new IOException("Failed to parse model: " + modelPath);
        }
        
        return result.get();
    }
}


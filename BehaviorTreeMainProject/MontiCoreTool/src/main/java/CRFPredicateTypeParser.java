import crftypesdef._parser.CRFTypesDefParser;
import crftypesdef._ast.ASTWorld;
import crftypesdef._ast.ASTPredicateTypeDefinition;
import crftypesdef._ast.ASTProperty;
import crftypesdef.CRFTypesDefMill;
import de.se_rwth.commons.logging.Log;

import java.io.*;
import java.nio.file.*;
import java.util.Optional;
import java.util.List;
import java.util.ArrayList;

/**
 * CRFPredicateParser - Reads CRFTypes model and generates predicate grammar rules for ConcreteBT.mc4
 * 
 * For each PredicateTypeDefinition like:
 *   define predicatetype Holding {
 *     item : Element
 *     agent : Agent
 *   }
 * 
 * Generates a grammar rule:
 *   Holding extends Predicate = "Holding" name:Name? "(" item:Element agent:Agent ")";
 */
public class CRFPredicateTypeParser {

    private static final String CRFTYPES_PATH = "src/test/resources/valid/CRFTypes/CRFPredicateTypes.bt";
    private static final String CONCRETE_BT_PATH = "src/main/grammars/ConcreteBT.mc4";
    
    private static final String START_MARKER = "// === GENERATED PREDICATE RULES (DO NOT EDIT BELOW) ===";
    private static final String END_MARKER = "// === END GENERATED PREDICATE RULES ===";
    
    public static void main(String[] args) {
        try {
            System.out.println("=== CRF PREDICATE PARSER ===");
            System.out.println("Generating predicate grammar rules from CRFTypes model...\n");
            
            // Initialize MontiCore mill
            CRFTypesDefMill.init();
            
            // Parse the CRFTypes model
            String modelPath = args.length > 0 ? args[0] : CRFTYPES_PATH;
            List<String> grammarRules = parseAndGenerateRules(modelPath);
            
            if (grammarRules.isEmpty()) {
                System.out.println("No PredicateTypeDefinitions found to generate.");
                return;
            }
            
            // Display generated rules
            System.out.println("=== GENERATED PREDICATE GRAMMAR RULES ===");
            for (String rule : grammarRules) {
                System.out.println(rule);
            }
            
            // Write to ConcreteBT.mc4
            String grammarPath = args.length > 1 ? args[1] : CONCRETE_BT_PATH;
            writeRulesToGrammar(grammarRules, grammarPath);
            
            System.out.println("\n✓ Predicate grammar rules successfully written to " + grammarPath);
            
        } catch (Exception e) {
            System.err.println("✗ ERROR: " + e.getMessage());
            e.printStackTrace();
        }
    }
    
    /**
     * Parse the CRFTypes model and generate grammar rules for each PredicateTypeDefinition
     */
  public static List<String> parseAndGenerateRules(String modelPath) throws IOException {
    List<String> rules = new ArrayList<>();
    
    // 1. Check if file exists
    File modelFile = new File(modelPath);
    if (!modelFile.exists()) {
        throw new FileNotFoundException("Model file not found: " + modelPath);
    }
    
    // 2. Create parser and parse
    CRFTypesDefParser parser = new CRFTypesDefParser();
    Optional<ASTWorld> result = parser.parse(modelPath);
    
    if (!result.isPresent()) {
        throw new RuntimeException("Failed to parse model: " + modelPath);
    }
    
    ASTWorld world = result.get();
    System.out.println("✓ Parsed model: " + modelPath);


    System.out.println("  Found " + world.getPredicateTypeDefinitionList().size() + " PredicateTypeDefinitions\n");
    
    // 7. Generate a rule for each PredicateTypeDefinition
    for (ASTPredicateTypeDefinition predTypeDef : world.getPredicateTypeDefinitionList()) {
        String rule = generateGrammarRule(predTypeDef);
        rules.add(rule);
    }
    
    return rules;
}
    
    /**
     * Generate a grammar rule from a PredicateTypeDefinition
     * 
     * Input:
     *   define predicatetype Holding {
     *     item : Element
     *     agent : Agent
     *   }
     * 
     * Output:
     *   Holding extends Predicate = "Holding" name:Name? "(" item:Element agent:Agent ")";
     */
    public static String generateGrammarRule(ASTPredicateTypeDefinition predTypeDef) {
        String predicateName = predTypeDef.getName();
        List<ASTProperty> properties = predTypeDef.getPropertyList();
        
        StringBuilder rule = new StringBuilder();
        
        // Start: PredicateName extends Predicate = "PredicateName" name:Name? "("
        rule.append(predicateName)
            .append(" extends Predicate = \"")
            .append(predicateName)
            .append("\" name:Name? \"(\"");
        
        // Add properties: propName:Name@propType
        for (int i = 0; i < properties.size(); i++) {
            ASTProperty prop = properties.get(i);
            String propName = prop.getName();
            String propType = prop.getType();
            
            rule.append(" ")
                .append(propName)
                .append(":Name@")
                .append(propType);
        }
        
        // End: ")";
        rule.append(" \")\";");
        
        return rule.toString();
    }
    
    /**
     * Write the generated rules to the ConcreteBT.mc4 grammar file
     * Inserts the rules between the predicate marker comments
     */
    public static void writeRulesToGrammar(List<String> rules, String grammarPath) throws IOException {
        Path path = Paths.get(grammarPath);
        
        if (!Files.exists(path)) {
            throw new FileNotFoundException("Grammar file not found: " + grammarPath);
        }
        
        // Read the current grammar content
        String content = new String(Files.readAllBytes(path));
        
        // Build the rules block
        StringBuilder rulesBlock = new StringBuilder();
        rulesBlock.append("\n\n").append(START_MARKER).append("\n");
        for (String rule : rules) {
            rulesBlock.append(rule).append("\n");
        }
        rulesBlock.append(END_MARKER).append("\n");
        
        // Check if we already have generated predicate rules - replace them
        if (content.contains(START_MARKER)) {
            int startIdx = content.indexOf(START_MARKER);
            int endIdx = content.indexOf(END_MARKER);
            if (endIdx > startIdx) {
                endIdx += END_MARKER.length();
                content = content.substring(0, startIdx) + rulesBlock.toString().trim() + content.substring(endIdx);
            }
        } else {
            // Insert before the last closing brace
            int lastBrace = content.lastIndexOf('}');
            if (lastBrace == -1) {
                throw new RuntimeException("Invalid grammar file: no closing brace found");
            }
            content = content.substring(0, lastBrace) + rulesBlock.toString() + content.substring(lastBrace);
        }
        
        // Write back
        Files.write(path, content.getBytes());
    }
}

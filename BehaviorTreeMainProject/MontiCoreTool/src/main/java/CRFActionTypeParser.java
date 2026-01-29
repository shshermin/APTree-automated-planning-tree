import crftypesdef._parser.CRFTypesDefParser;
import crftypesdef._ast.ASTWorld;
import crftypesdef._ast.ASTActionTypeDefinition;
import crftypesdef._ast.ASTProperty;
import crftypesdef._ast.ASTPredicateRef;
import crftypesdef._ast.ASTActionLevel;
import crftypesdef.CRFTypesDefMill;

import java.io.*;
import java.nio.file.*;
import java.util.Optional;
import java.util.List;
import java.util.ArrayList;

/**
 * CRFActionTypeParser - Reads CRFActionTypes model and generates action grammar rules for ConcreteBT.mc4
 * 
 * For each ActionTypeDefinition like:
 *   Action pickUpHL {
 *       acttype: HIGHLEVEL
 *       parameters {
 *           obj: Element
 *           grabPos: Location
 *           client: Robot
 *       }
 *       preconditions { ... }
 *       effects { ... }
 *   }
 * 
 * Generates a grammar rule:
 *   PickUpHL extends PActionNode = "Action" "PickUpHL" name:Name "(" obj:Name@Element grabPos:Name@Location client:Name@Robot ")" ("{" (Decorator | Service)* "}")? ;
 *   astrule PickUpHL = actLevel:ActionLevel = ActionLevel.HIGHLEVEL ;
 */
public class CRFActionTypeParser {

    private static final String CRFTYPES_PATH = "src/test/resources/valid/CRFTypes/CRFActionTypes.bt";
    private static final String CONCRETE_BT_PATH = "src/main/grammars/ConcreteBT.mc4";
    
    private static final String START_MARKER = "// === GENERATED ACTION RULES (DO NOT EDIT BELOW) ===";
    private static final String END_MARKER = "// === END GENERATED ACTION RULES ===";
    
    public static void main(String[] args) {
        try {
            System.out.println("=== CRF ACTION TYPE PARSER ===");
            System.out.println("Generating action grammar rules from CRFActionTypes model...\n");
            
            // Initialize MontiCore mill
            CRFTypesDefMill.init();
            
            // Parse the CRFActionTypes model
            String modelPath = args.length > 0 ? args[0] : CRFTYPES_PATH;
            List<String> grammarRules = parseAndGenerateRules(modelPath);
            
            if (grammarRules.isEmpty()) {
                System.out.println("No ActionTypeDefinitions found to generate.");
                return;
            }
            
            // Display generated rules
            System.out.println("=== GENERATED ACTION GRAMMAR RULES ===");
            for (String rule : grammarRules) {
                System.out.println(rule);
                System.out.println();
            }
            
            // Write to ConcreteBT.mc4
            String grammarPath = args.length > 1 ? args[1] : CONCRETE_BT_PATH;
            writeRulesToGrammar(grammarRules, grammarPath);
            
            System.out.println("\n✓ Action grammar rules successfully written to " + grammarPath);
            
        } catch (Exception e) {
            System.err.println("✗ ERROR: " + e.getMessage());
            e.printStackTrace();
        }
    }
    
    /**
     * Parse the CRFActionTypes model and generate grammar rules for each ActionTypeDefinition
     */
    public static List<String> parseAndGenerateRules(String modelPath) throws IOException {
        List<String> rules = new ArrayList<>();
        
        // Check if file exists
        File modelFile = new File(modelPath);
        if (!modelFile.exists()) {
            throw new FileNotFoundException("Model file not found: " + modelPath);
        }
        
        // Create parser and parse
        CRFTypesDefParser parser = new CRFTypesDefParser();
        Optional<ASTWorld> result = parser.parse(modelPath);
        
        if (!result.isPresent()) {
            throw new RuntimeException("Failed to parse model: " + modelPath);
        }
        
        ASTWorld world = result.get();
        System.out.println("✓ Parsed model: " + modelPath);
        System.out.println("  Found " + world.getActionTypeDefinitionList().size() + " ActionTypeDefinitions\n");
        
        // Generate a rule for each ActionTypeDefinition
        for (ASTActionTypeDefinition actionDef : world.getActionTypeDefinitionList()) {
            String rule = generateGrammarRule(actionDef);
            rules.add(rule);
        }
        
        return rules;
    }
    
    /**
     * Capitalize the first letter of a string
     */
    private static String capitalize(String s) {
        if (s == null || s.isEmpty()) return s;
        return Character.toUpperCase(s.charAt(0)) + s.substring(1);
    }
    
    /**
     * Generate a grammar rule from an ActionTypeDefinition
     * 
     * Input:
     *   Action pickUpHL {
     *       acttype: HIGHLEVEL
     *       parameters { obj: Element grabPos: Location client: Robot }
     *       preconditions { ... }
     *       effects { ... }
     *   }
     * 
     * Output:
     *   PickUpHL extends PActionNode = "Action" "PickUpHL" name:Name "(" obj:Name@Element grabPos:Name@Location client:Name@Robot ")" ("@" subtreeAnnotation:Name)? ("{" (Decorator | Service)* "}")? ;
     *   astrule PickUpHL = method public crftypedef._ast.ASTActionLevel getActLevel() { if(actLevel==null){ actLevel=crftypedef._ast.ASTActionLevel.HIGHLEVEL; } return actLevel; } ;
     */
    public static String generateGrammarRule(ASTActionTypeDefinition actionDef) {
        String typeName = actionDef.getName();
        String capitalizedName = capitalize(typeName);
        ASTActionLevel actionLevel = actionDef.getActLevel();
        List<ASTProperty> parameters = actionDef.getPropertyList();
        
        StringBuilder rule = new StringBuilder();
        
        // Main rule: CapitalizedName extends PActionNode = "Action" "CapitalizedName" name:Name "("
        rule.append(capitalizedName)
            .append(" extends PActionNode = \"Action\" \"")
            .append(capitalizedName)
            .append("\" name:Name \"(\"");
        
        // Add parameters: paramName:Name@ParamType
        for (int i = 0; i < parameters.size(); i++) {
            ASTProperty param = parameters.get(i);
            String paramName = param.getName();
            String paramType = param.getType().getName();
            
            rule.append(" ")
                .append(paramName)
                .append(":Name@")
                .append(paramType);
        }
        
        // End with optional subtree annotation and optional decorators/services block
        rule.append(" \")\" (\"@\" subtreeAnnotation:Name)? (\"{\" (Decorator | Service)* \"}\")? ;");
        
        // Add astrule with method for default actLevel value
        rule.append("\nastrule ")
            .append(capitalizedName)
            .append(" = method public crftypedef._ast.ASTActionLevel getActLevel() { ")
            .append("if(actLevel==null){ actLevel=crftypedef._ast.ASTActionLevel.")
            .append(actionLevel.name())
            .append("; } return actLevel; } ;");
        
        return rule.toString();
    }
    
    /**
     * Write the generated rules to the ConcreteBT.mc4 grammar file
     * Inserts the rules between the action marker comments
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
            rulesBlock.append(rule).append("\n\n");
        }
        rulesBlock.append(END_MARKER).append("\n");
        
        // Check if we already have generated action rules - replace them
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

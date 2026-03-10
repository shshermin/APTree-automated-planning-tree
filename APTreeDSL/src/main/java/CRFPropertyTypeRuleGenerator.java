import java.io.File;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.regex.Pattern;

import crftypesdef.CRFTypesDefMill;
import crftypesdef._ast.ASTProperty;
import crftypesdef._ast.ASTWorld;
import crftypesdef._parser.CRFTypesDefParser;
import de.se_rwth.commons.logging.Log;

/**
 * CRFPropertyTypeRuleGenerator - Reads CRFTypes model and generates grammar rules for CRFTypesCon.mc4
 * 
 * Usage: Provide input model path and output grammar path as arguments.
 * Defaults:
 *  - Input: src/test/resources/valid/CRFTypes/LiveMatPropertyTypes.bt
 *  - Output: src/main/grammars/CRFTypesCon.mc4
 */
public class CRFPropertyTypeRuleGenerator {

    private static final String DEFAULT_INPUT_PATH = "src/test/resources/valid/CRFTypes/LiveMatPropertyTypes.bt";
    private static final String DEFAULT_OUTPUT_PATH = "src/main/grammars/CRFTypesCon.mc4";
    
    // Markers to identify the generated section in the target grammar file
    private static final String START_MARKER = "// === GENERATED RULES (DO NOT EDIT BELOW) ===";
    private static final String END_MARKER = "// === END GENERATED RULES ===";
    
    // Prefix for the World rule
    private static final String WORLD_RULE_PREFIX = "World = (PropertyTypeDefinition | Property | PredicateTypeDefinition | ActionTypeDefinition";

    public static void main(String[] args) {
        try {
            System.out.println("=== CRF PROPERTY TYPE RULE GENERATOR ===");
            
            String inputPath = args.length > 0 ? args[0] : DEFAULT_INPUT_PATH;
            String outputPath = args.length > 1 ? args[1] : DEFAULT_OUTPUT_PATH;

            System.out.println("Input Model:   " + inputPath);
            System.out.println("Output Grammar: " + outputPath);

            // 1. Initialize MontiCore Mill
            CRFTypesDefMill.init();
            
            // 2. Parse the input model
            File modelFile = new File(inputPath);
            if (!modelFile.exists()) {
                throw new FileNotFoundException("Input model file not found: " + inputPath);
            }
            
            CRFTypesDefParser parser = new CRFTypesDefParser();
            Optional<ASTWorld> result = parser.parse(inputPath);
            
            if (!result.isPresent()) {
                System.err.println("✗ Failed to parse model.");
                if (Log.getErrorCount() > 0) {
                     Log.getFindings().forEach(f -> System.out.println(f.toString()));
                }
                return;
            }
            
            ASTWorld world = result.get();
            System.out.println("✓ Parsed model successfully.");
            
            // 3. Generate grammar rules from AST
            List<String> generatedRules = generateRules(world);
            
            // Collect generated type names for World rule update
            List<String> typeNames = new ArrayList<>();
            world.getPropertyTypeDefinitionList().forEach(def -> typeNames.add(def.getName()));
            
            if (generatedRules.isEmpty()) {
                System.out.println("No property definitions found. No rules generated.");
            } else {
                System.out.println("Generated " + generatedRules.size() + " rules.");
                for (String r : generatedRules) {
                    System.out.println("  " + r);
                }
            }

            // 4. Inject rules into the grammar file and update World rule
            updateGrammarFile(outputPath, generatedRules, typeNames);
            
            System.out.println("✓ Grammar file updated successfully.");

        } catch (Exception e) {
            System.err.println("✗ ERROR: " + e.getMessage());
            e.printStackTrace();
            System.exit(1);
        }
    }
    
    private static List<String> generateRules(ASTWorld world) {
        List<String> rules = new ArrayList<>();
        
        // Iterate over all PropertyTypeDefinitions in the World
        // World -> (PropertyTypeDefinition | ...)*
        // We need to filter for ASTPropertyTypeDefinition
        
        world.getPropertyTypeDefinitionList().forEach(def -> {
            StringBuilder rule = new StringBuilder();
            
            // Extract data from AST
            String typeName = def.getName();
            String superType = def.getSuperType(); // e.g., Element, Location
            List<ASTProperty> properties = def.getPropertyList();
            
            // Format: symbol <Name> extends <SuperType> = "<Name>" name:Name "(" <props> ")";
            rule.append("symbol ")
                .append(typeName)
                .append(" extends ")
                .append(superType)
                .append(" = \"")
                .append(typeName)
                .append("\" name:Name \"(\""); // Opening paren for properties
            
            // No longer inject inherited 'loc' property for any type. All properties must be explicitly listed in the model.
            
            // Append properties
            for (ASTProperty prop : properties) {
               String pName = prop.getName();
               String pType = prop.getType().getName();
               boolean isOptional = prop.isIsOptional();
               
               // Map 'STRING_VALUE' or 'string' to 'Name' as requested
               if ("STRING_VALUE".equals(pType) || "string".equalsIgnoreCase(pType)) {
                   pType = "Name";
               }
               // Map 'BOOLEAN_VALUE', 'Boolean', or 'boolean' to 'Boolean' as in the grammar
               if ("BOOLEAN_VALUE".equalsIgnoreCase(pType) || "Boolean".equalsIgnoreCase(pType)) {
                   pType = "Boolean";
               }

               // Determine if this is a primitive type or a reference type
               boolean isPrimitive = isPrimitiveType(pType);
               
               // Build the field fragment
               String field;
               if (isPrimitive) {
                   field = pName + ":" + pType;
               } else {
                   field = pName + ":Name@" + pType;
               }
               
               // Check if it's a list (marked by + in the model)
               if (prop.isIsList()) {
                   field += "+";
               }

               rule.append(" ");
               if (isOptional) {
                   rule.append("(").append(field).append(")?");
               } else {
                   rule.append(field);
               }
            }
            
            rule.append(" \")\";");
            rules.add(rule.toString());
        });
        
        return rules;
    }
    
    /**
     * Determine if a type is a primitive type (Boolean, Integer, Name) or a reference type (Element, Location, etc.)
     * Primitive types are used directly, while reference types use Name@Type format for symbol references.
     */
    private static boolean isPrimitiveType(String typeName) {
        return typeName.equals("Boolean") || 
               typeName.equals("Integer") || 
               typeName.equals("Name") ||
               typeName.equals("String") ||
               typeName.equals("Double") ||
               typeName.equals("Coordinate");
    }
    
    private static void updateGrammarFile(String grammarPath, List<String> newRules, List<String> typeNames) throws IOException {
        Path path = Paths.get(grammarPath);
        if (!Files.exists(path)) {
            throw new FileNotFoundException("Target grammar file not found: " + grammarPath);
        }
        
        String content = new String(Files.readAllBytes(path));
        
        // --- Merge logic: append new rules, overwrite if same name exists ---
        int startIdx = content.indexOf(START_MARKER);
        int endIdx = content.indexOf(END_MARKER);
        
        // Parse existing rules from the block (keyed by rule name)
        java.util.LinkedHashMap<String, String> mergedRules = new java.util.LinkedHashMap<>();
        
        if (startIdx >= 0 && endIdx > startIdx) {
            String existingBlock = content.substring(startIdx + START_MARKER.length(), endIdx).trim();
            for (String line : existingBlock.split("\\r?\\n")) {
                String trimmed = line.trim();
                if (!trimmed.isEmpty()) {
                    String ruleName = extractRuleName(trimmed);
                    if (ruleName != null) {
                        mergedRules.put(ruleName, trimmed);
                    }
                }
            }
        }
        
        // Merge new rules: overwrite existing by name, add new ones
        for (String rule : newRules) {
            String trimmed = rule.trim();
            if (!trimmed.isEmpty()) {
                String ruleName = extractRuleName(trimmed);
                if (ruleName != null) {
                    if (mergedRules.containsKey(ruleName)) {
                        System.out.println("  Overwriting existing rule: " + ruleName);
                    } else {
                        System.out.println("  Adding new rule: " + ruleName);
                    }
                    mergedRules.put(ruleName, trimmed);
                }
            }
        }
        
        // Rebuild the block
        StringBuilder newBlock = new StringBuilder();
        newBlock.append(START_MARKER).append(System.lineSeparator());
        for (String rule : mergedRules.values()) {
            newBlock.append(rule).append(System.lineSeparator());
        }
        newBlock.append(END_MARKER);
        
        if (startIdx >= 0 && endIdx > startIdx) {
            // Replace existing block
            System.out.println("Updating existing rule block (merge mode)...");
            String before = content.substring(0, startIdx);
            String after = content.substring(endIdx + END_MARKER.length());
            content = before + newBlock.toString() + after;
        } else {
            // Append at the end (before the last closing brace)
             System.out.println("Inserting new rule block...");
            int lastBrace = content.lastIndexOf("}");
            if (lastBrace < 0) {
                throw new RuntimeException("Malformed grammar file: Missing closing brace '}'");
            }
            String before = content.substring(0, lastBrace);
            String after = content.substring(lastBrace);
            
            content = before + System.lineSeparator() + newBlock.toString() + System.lineSeparator() + after;
        }

        // Collect ALL type names from merged rules for the World rule update
        List<String> allTypeNames = new ArrayList<>(mergedRules.keySet());

        // Update the World rule (Append-only mode to preserve other generated types)
        String worldPattern = "(World\\s*=\\s*\\()(.*?)(\\)\\*;)";
        java.util.regex.Matcher m = Pattern.compile(worldPattern, Pattern.DOTALL).matcher(content);
        
        if (m.find()) {
            System.out.println("Updating World rule with properties...");
            String prefix = m.group(1);
            String existingContent = m.group(2);
            String suffix = m.group(3);
            
            StringBuilder additionalContent = new StringBuilder();
            for (String name : allTypeNames) {
                 if (!existingContent.contains(name)) { 
                    additionalContent.append(" | ").append(name);
                 }
            }
            
            content = content.substring(0, m.start()) + prefix + existingContent + additionalContent.toString() + suffix + content.substring(m.end());
        } else {
            System.out.println("Warning: World rule not marked for update (pattern not found).");
        }
        
        Files.write(path, content.getBytes());
    }
    
    /**
     * Extract the rule name (first word) from a grammar rule line.
     * E.g., "symbol Beam extends Element = ..." -> "Beam"
     * Handles both "symbol X ..." and "X extends ..." patterns.
     */
    private static String extractRuleName(String ruleLine) {
        String trimmed = ruleLine.trim();
        String[] tokens = trimmed.split("\\s+");
        if (tokens.length < 2) return null;
        // If the line starts with "symbol", the name is the second token
        if ("symbol".equals(tokens[0])) {
            return tokens[1];
        }
        // Otherwise first token is the name (e.g., "Beam extends ...")
        return tokens[0];
    }
}

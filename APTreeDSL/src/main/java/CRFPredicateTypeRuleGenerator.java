
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
 * CRFPredicateTypeRuleGenerator - Reads CRFPredicateTypes model and generates grammar rules for CRFTypesCon.mc4
 */
public class CRFPredicateTypeRuleGenerator {

    private static final String DEFAULT_INPUT_PATH = "src/test/resources/valid/CRFTypes/LiveMatPredicaetTypes.bt";
    private static final String DEFAULT_OUTPUT_PATH = "src/main/grammars/CRFTypesCon.mc4";

    // Markers to identify the generated section in the target grammar file
    private static final String START_MARKER = "// === GENERATED PREDICATE RULES (DO NOT EDIT BELOW) ===";
    private static final String END_MARKER = "// === END GENERATED PREDICATE RULES ===";

    // Prefix for the World rule
    private static final String WORLD_RULE_PREFIX = "World = (PropertyTypeDefinition | Property | PredicateTypeDefinition | ActionTypeDefinition";

    // We do NOT update the World rule here, assuming Predicates are used inside Actions/Preconditions/Effects
    // But wait, the World rule DOES contain PredicateTypeDefinition, but usually we don't put 'Holding' or 'AtPlace' 
    // directly in the World sequence of the grammar unless they are top-level constructs.
    // In CRFTypesCon.mc4, Predicates inherit from Predicate. They are usually referenced via Name@Predicate.
    // However, if we want them to be parsable as individual lines (maybe for testing?), we might need them in World.
    // The previous request didn't ask to update World, but I'll make it consistent if needed. 
    // Looking at CRFTypesCon.mc4: World = (... | PredicateTypeDefinition | ...) 
    // It doesn't seem the concrete predicates (Holding, AtPlace) are in the World rule in the current file.
    // So I won't touch World rule for now.

    public static void main(String[] args) {
        try {
            System.out.println("=== CRF PREDICATE TYPE RULE GENERATOR ===");

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
            List<String> predNames = new ArrayList<>();
            world.getPredicateTypeDefinitionList().forEach(def -> predNames.add(def.getName()));

            if (generatedRules.isEmpty()) {
                System.out.println("No predicate definitions found. No rules generated.");
            } else {
                System.out.println("Generated " + generatedRules.size() + " rules.");
                for (String r : generatedRules) {
                    System.out.println("  " + r);
                }
            }

            // 4. Inject rules into the grammar file
            updateGrammarFile(outputPath, generatedRules, predNames);

            System.out.println("✓ Grammar file updated successfully.");

        } catch (Exception e) {
            System.err.println("✗ ERROR: " + e.getMessage());
            e.printStackTrace();
            System.exit(1);
        }
    }

    private static List<String> generateRules(ASTWorld world) {
        List<String> rules = new ArrayList<>();

        // Iterate over all PredicateTypeDefintion in the World
        world.getPredicateTypeDefinitionList().forEach(def -> {
            StringBuilder rule = new StringBuilder();

            // Extract data from AST
            String predName = def.getName();
            List<ASTProperty> properties = def.getPropertyList();

            // Format: RuleName extends Predicate = "!"? "RuleName" "(" arg1:Name@Type arg2:Name@Type ")";
            // Example: Holding extends Predicate = "!"? "Holding" "(" item:Name@Element agent:Name@Agent ")";
            
            rule.append(predName)
                .append(" extends Predicate = not:[\"!\"]? \"")
                .append(predName)
                .append("\" \"(\"");

            // Append arguments
            for (ASTProperty prop : properties) {
                String pName = prop.getName();
                String pType = prop.getType().getName();
                boolean isOptional = prop.isIsOptional();
                boolean isGeom = prop.isIsGeom();

                // Build the field fragment: pName:Name@pType
                String field = pName + ":Name@" + pType;
                if (prop.isIsList()) {
                    field += "+";
                }

                rule.append(" ");
                if (isOptional) {
                    rule.append("(").append(field).append(")?");
                } else {
                    rule.append(field);
                }

                if (isGeom) {
                    System.out.println("  [geom] " + predName + "." + pName + " marked as geometric (excluded from task planning)");
                }
            }

            rule.append(" \")\";");
            rules.add(rule.toString());
        });

        return rules;
    }

    private static void updateGrammarFile(String grammarPath, List<String> newRules, List<String> predNames) throws IOException {
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
                        System.out.println("  Overwriting existing predicate: " + ruleName);
                    } else {
                        System.out.println("  Adding new predicate: " + ruleName);
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
            // Replace existing block with merged content
            System.out.println("Updating existing predicate rule block (merge mode)...");
            String before = content.substring(0, startIdx);
            String after = content.substring(endIdx + END_MARKER.length());
            content = before + newBlock.toString() + after;
        } else {
            // Append at the end (before the last closing brace)
            System.out.println("Inserting new predicate rule block...");
            int lastBrace = content.lastIndexOf("}");
            if (lastBrace < 0) {
                throw new RuntimeException("Malformed grammar file: Missing closing brace '}'");
            }
            String before = content.substring(0, lastBrace);
            String after = content.substring(lastBrace);

            content = before + System.lineSeparator() + newBlock.toString() + System.lineSeparator() + after;
        }
        
        // Collect ALL predicate names from merged rules for the World rule update
        List<String> allPredNames = new ArrayList<>(mergedRules.keySet());
        
        // Update the World rule
        String worldPattern = "(World\\s*=\\s*\\()(.*?)(\\)\\*;)";
        java.util.regex.Matcher m = Pattern.compile(worldPattern, Pattern.DOTALL).matcher(content);
        
        if (m.find()) {
            System.out.println("Updating World rule with predicates...");
            String prefix = m.group(1);
            String existingContent = m.group(2);
            String suffix = m.group(3);
            
            StringBuilder additionalContent = new StringBuilder();
            for (String name : allPredNames) {
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
     * E.g., "Holding extends Predicate = ..." -> "Holding"
     */
    private static String extractRuleName(String ruleLine) {
        String trimmed = ruleLine.trim();
        String[] tokens = trimmed.split("\\s+");
        if (tokens.length < 1) return null;
        return tokens[0];
    }
}


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

    // Concrete predicate rules (Holding, AtPlace, etc.) extend Predicate and are referenced via Name@Predicate.
    // They are included in the World rule of CRFTypesCon.mc4 to enable top-level parsing.
    // This generator only updates the predicate rules section; the World rule is managed separately.

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
                // Each property adds a space before it
                rule.append(" ");

                String pName = prop.getName();
                String pType = prop.getType().getName();

                // rule: pName:Name@pType
                rule.append(pName)
                    .append(":Name@")
                    .append(pType);

                if (prop.isIsList()) {
                    rule.append("+");
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

        // Construct the new block content
        StringBuilder newBlock = new StringBuilder();
        newBlock.append(START_MARKER).append(System.lineSeparator());
        for (String rule : newRules) {
            newBlock.append(rule).append(System.lineSeparator());
        }
        newBlock.append(END_MARKER);

        String newContent;

        // Logic to replace existing block or insert new one
        int startIdx = content.indexOf(START_MARKER);
        int endIdx = content.indexOf(END_MARKER);

        if (startIdx >= 0 && endIdx > startIdx) {
            // Replace existing block
            System.out.println("Updating existing predicate rule block...");
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

            // Add a newline before the block if needed
            content = before + System.lineSeparator() + newBlock.toString() + System.lineSeparator() + after;
        }
        
        // Update the World rule by appending new predicate names.
        // The World rule is expected to follow the pattern: World = ( ... )*;
        
        // Regex to match existing World rule content inside parens
        // World = ( ... )*;
        String worldPattern = "(World\\s*=\\s*\\()(.*?)(\\)\\*;)";
        java.util.regex.Matcher m = Pattern.compile(worldPattern, Pattern.DOTALL).matcher(content);
        
        if (m.find()) {
            System.out.println("Updating World rule with predicates...");
            String prefix = m.group(1);
            String existingContent = m.group(2);
            String suffix = m.group(3);
            
            StringBuilder additionalContent = new StringBuilder();
            for (String name : predNames) {
                // adding if not already present to avoid duplicates
                // simplistic check: if " | Name" or "Name |" or "Name" exists
                 if (!existingContent.contains(name)) { // simple check, might have false positive if substring
                    additionalContent.append(" | ").append(name);
                 }
            }
            
            content = content.substring(0, m.start()) + prefix + existingContent + additionalContent.toString() + suffix + content.substring(m.end());
        } else {
             System.out.println("Warning: World rule not marked for update (pattern not found).");
        }

        Files.write(path, content.getBytes());
    }
}

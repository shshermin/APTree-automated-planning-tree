import crftypesdef.CRFTypesDefMill;
import crftypesdef._ast.ASTProperty;
import crftypesdef._ast.ASTWorld;
import crftypesdef._parser.CRFTypesDefParser;
import de.se_rwth.commons.logging.Log;

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

/**
 * CRFActionTypeRuleGenerator - Reads CRFActionTypes model and generates grammar rules for CRFTypesCon.mc4
 *
 * Usage: Provide input model path and output grammar path as arguments.
 * Defaults:
 *  - Input: src/test/resources/valid/CRFTypes/CRFActionTypes.bt
 *  - Output: src/main/grammars/CRFTypesCon.mc4
 */
public class CRFActionTypeRuleGenerator {

    private static final String DEFAULT_INPUT_PATH = "src/test/resources/valid/CRFTypes/CRFActionTypes.bt";
    private static final String DEFAULT_OUTPUT_PATH = "src/main/grammars/CRFTypesCon.mc4";

    // Markers to identify the generated section in the target grammar file
    private static final String START_MARKER = "// === GENERATED ACTION RULES (DO NOT EDIT BELOW) ===";
    private static final String END_MARKER = "// === END GENERATED ACTION RULES ===";

    // Prefix for the World rule
    private static final String WORLD_RULE_PREFIX = "World = (PropertyTypeDefinition | Property | PredicateTypeDefinition | ActionTypeDefinition";

    public static void main(String[] args) {
        try {
            System.out.println("=== CRF ACTION TYPE RULE GENERATOR ===");

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
                throw new IOException("Failed to parse input model: " + inputPath);
            }

            ASTWorld world = result.get();

            // 3. Generate rules for each ActionTypeDefinition in MontiCore action format
            List<String> rules = new ArrayList<>();
            world.getActionTypeDefinitionList().forEach(def -> {
                StringBuilder rule = new StringBuilder();
                // Use the typeName as the action name
                String actionName = def.getName();
                rule.append(actionName)
                    .append(" extends PActionNode = \"Action\" \"")
                    .append(actionName)
                    .append("\" name:Name (");

                List<String> params = new ArrayList<>();
                for (ASTProperty prop : def.getPropertyList()) {
                    // paramName:Name@Type
                    String propType = prop.getType().getName();
                    params.add(prop.getName() + ":Name@" + propType);
                }

                rule.append(String.join(" ", params));
                rule.append(") ");
                rule.append("(Decorator | Service)* ");
                rule.append("(@ subtreeAnnotation:Name@BehaviorTree)?;");
                rules.add(rule.toString());

                // Add astrule for getActLevel
                String actLevel = "HIGHLEVEL";
                try {
                    // Try to get the actLevel value from the model (if available)
                    java.lang.reflect.Method m = def.getClass().getMethod("getActLevel");
                    Object levelObj = m.invoke(def);
                    if (levelObj != null) {
                        actLevel = levelObj.toString().toUpperCase();
                    }
                } catch (Exception e) {
                    // Default to HIGHLEVEL if not found
                }
                String astrule = "astrule " + actionName + " = method public crftypesdef._ast.ASTActionLevel getActLevel() { if(actLevel==null){ actLevel=crftypesdef._ast.ASTActionLevel." + actLevel + "; } return actLevel; } ;";
                rules.add(astrule);
            });

            // 4. Read the output grammar file
            Path grammarPath = Paths.get(outputPath);
            String content = new String(Files.readAllBytes(grammarPath));

            // 5. Replace the generated section
            Pattern pattern = Pattern.compile(Pattern.quote(START_MARKER) + ".*?" + Pattern.quote(END_MARKER), Pattern.DOTALL);
            StringBuilder newBlock = new StringBuilder();
            newBlock.append(START_MARKER).append("\n");
            for (String rule : rules) {
                newBlock.append(rule).append("\n");
            }
            newBlock.append(END_MARKER);
            String updatedContent = pattern.matcher(content).replaceFirst(newBlock.toString());

            // 6. Write back to the grammar file
            Files.write(grammarPath, updatedContent.getBytes());
            System.out.println("[OK] Action type rules generated and written to: " + outputPath);
        } catch (Exception e) {
            Log.error("[ERROR] " + e.getMessage());
            e.printStackTrace();
        }
    }
}

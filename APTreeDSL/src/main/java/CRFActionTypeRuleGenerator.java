import java.io.File;
import java.io.FileNotFoundException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * CRFActionTypeRuleGenerator - Reads CRFActionTypes model and generates grammar rules for DomainTypesCon.mc4
 * Uses regex-based parsing to handle custom types (Stack, Cassette, etc.) without grammar constraints.
 *
 * Usage: Provide input model path and output grammar path as arguments.
 * Defaults:
 *  - Input: src/test/resources/valid/DomainTypes/LiveMatActionTypes.bt
 *  - Output: src/main/grammars/DomainTypesCon.mc4
 */
public class CRFActionTypeRuleGenerator {

    private static final String DEFAULT_INPUT_PATH = "src/test/resources/valid/DomainTypes/LiveMatActionTypes.bt";
    private static final String DEFAULT_OUTPUT_PATH = "src/main/grammars/DomainTypesCon.mc4";

    // Markers to identify the generated section in the target grammar file
    private static final String START_MARKER = "// === GENERATED ACTION RULES (DO NOT EDIT BELOW) ===";
    private static final String END_MARKER = "// === END GENERATED ACTION RULES ===";

    public static void main(String[] args) {
        try {
            System.out.println("=== CRF ACTION TYPE RULE GENERATOR ===");

            String inputPath = args.length > 0 ? args[0] : DEFAULT_INPUT_PATH;
            String outputPath = args.length > 1 ? args[1] : DEFAULT_OUTPUT_PATH;

            System.out.println("Input Model:   " + inputPath);
            System.out.println("Output Grammar: " + outputPath);

            // 1. Read the input model file as text
            File modelFile = new File(inputPath);
            if (!modelFile.exists()) {
                throw new FileNotFoundException("Input model file not found: " + inputPath);
            }

            String content = new String(Files.readAllBytes(modelFile.toPath()));

            // 2. Extract action definitions using regex
            List<ActionDef> actions = extractActionDefinitions(content);
            System.out.println("✓ Extracted " + actions.size() + " action definitions");

            // 3. Generate grammar rules
            List<String> rules = new ArrayList<>();
            for (int i = 0; i < actions.size(); i++) {
                ActionDef action = actions.get(i);
                StringBuilder rule = new StringBuilder();
                rule.append(action.name)
                    .append(" extends PActionNode = \"Action\" \"")
                    .append(action.name)
                    .append("\" name:Name \"(\" ");

                // Add parameters
                List<String> params = new ArrayList<>();
                for (PropertyDef prop : action.properties) {
                    params.add(prop.name + ":Name@" + prop.type);
                }
                rule.append(String.join(" ", params));
                rule.append(" \")\" (\"{\" (Decorator | Service)* \"}\")?");
                rule.append(" (\"@\" subtreeAnnotation:Name)? ;");
                rules.add(rule.toString());

                // Add astrule for getActLevel
                String actLevel = action.actLevel.toUpperCase().replace("LEVEL", "LEVEL");
                if (!actLevel.endsWith("LEVEL")) {
                    actLevel = actLevel + "LEVEL";
                }
                String astrule = "astrule " + action.name + " = method public domaintypesdef._ast.ASTActionLevel getActLevel() { if(actLevel==null){ actLevel=domaintypesdef._ast.ASTActionLevel." + actLevel + "; } return actLevel; } ;";
                rules.add(astrule);
                
                // Add blank line between action groups (except after the last one)
                if (i < actions.size() - 1) {
                    rules.add("");
                }
            }
            System.out.println("  Total rules entries (including blank lines): " + rules.size());

            // 4. Read the output grammar file
            Path grammarPath = Paths.get(outputPath);
            String grammarContent = new String(Files.readAllBytes(grammarPath));

            // 5. Build the new action rules block
            StringBuilder newBlock = new StringBuilder();
            newBlock.append(START_MARKER).append("\n");
            for (String rule : rules) {
                if (rule.isEmpty()) {
                    newBlock.append("\n");  // Add extra newline for blank lines
                } else {
                    newBlock.append(rule).append("\n");
                }
            }
            newBlock.append(END_MARKER).append("\n");

            // 6. Replace the generated section
            // First, try to match if both markers exist
            Pattern pattern = Pattern.compile(Pattern.quote(START_MARKER) + ".*?" + Pattern.quote(END_MARKER), Pattern.DOTALL);
            String updatedContent = pattern.matcher(grammarContent).replaceFirst(newBlock.toString());
            
            // If no replacement happened (markers not yet present), find START_MARKER and replace to end of file
            if (updatedContent.equals(grammarContent)) {
                int startPos = grammarContent.indexOf(START_MARKER);
                if (startPos != -1) {
                    // Find the closing brace of the grammar (last char that matters)
                    int endPos = grammarContent.lastIndexOf('}');
                    if (endPos > startPos) {
                        updatedContent = grammarContent.substring(0, startPos) 
                            + newBlock.toString()
                            + grammarContent.substring(endPos);
                    }
                } else {
                    // If START_MARKER not found, just append before the closing brace
                    int endPos = grammarContent.lastIndexOf('}');
                    if (endPos != -1) {
                        updatedContent = grammarContent.substring(0, endPos) 
                            + newBlock.toString()
                            + grammarContent.substring(endPos);
                    }
                }
            }

            // 7. Write back to the grammar file
            Files.write(grammarPath, updatedContent.getBytes());
            System.out.println("[OK] Action type rules generated and written to: " + outputPath);

        } catch (Exception e) {
            System.err.println("[ERROR] " + e.getMessage());
            e.printStackTrace();
        }
    }

    private static List<ActionDef> extractActionDefinitions(String content) {
        List<ActionDef> actions = new ArrayList<>();

        // Find all "Define Action" blocks, handling nested braces
        int start = 0;
        while (true) {
            int definePos = content.indexOf("Define Action ", start);
            if (definePos == -1) break;

            // Extract action name
            int nameStart = definePos + "Define Action ".length();
            int nameEnd = nameStart;
            while (nameEnd < content.length() && Character.isLetterOrDigit(content.charAt(nameEnd))) {
                nameEnd++;
            }
            String actionName = content.substring(nameStart, nameEnd);

            // Find opening brace
            int braceStart = content.indexOf('{', nameEnd);
            if (braceStart == -1) {
                start = nameEnd;
                continue;
            }

            // Find matching closing brace by counting brace depth
            int braceDepth = 1;
            int pos = braceStart + 1;
            int braceEnd = -1;
            while (pos < content.length() && braceDepth > 0) {
                if (content.charAt(pos) == '{') braceDepth++;
                else if (content.charAt(pos) == '}') braceDepth--;
                if (braceDepth == 0) {
                    braceEnd = pos;
                    break;
                }
                pos++;
            }

            if (braceEnd == -1) {
                start = pos;
                continue;
            }

            String actionBody = content.substring(braceStart + 1, braceEnd);

            ActionDef action = new ActionDef();
            action.name = actionName;

            // Extract ActLevel
            Pattern actLevelPattern = Pattern.compile("ActLevel:\\s*(\\w+)");
            Matcher actLevelMatcher = actLevelPattern.matcher(actionBody);
            if (actLevelMatcher.find()) {
                action.actLevel = actLevelMatcher.group(1);
            } else {
                action.actLevel = "HighLevel";
            }

            // Extract Properties
            Pattern propsPattern = Pattern.compile("Properties\\s*\\{([^}]*)\\}");
            Matcher propsMatcher = propsPattern.matcher(actionBody);
            if (propsMatcher.find()) {
                String propsBody = propsMatcher.group(1);
                Pattern propPattern = Pattern.compile("(\\w+):\\s*(\\w+)");
                Matcher propMatcher = propPattern.matcher(propsBody);
                while (propMatcher.find()) {
                    PropertyDef prop = new PropertyDef();
                    prop.name = propMatcher.group(1);
                    prop.type = propMatcher.group(2);
                    action.properties.add(prop);
                }
            }

            actions.add(action);
            start = braceEnd + 1;
        }

        return actions;
    }

    private static class ActionDef {
        String name;
        String actLevel = "HighLevel";
        List<PropertyDef> properties = new ArrayList<>();
    }

    private static class PropertyDef {
        String name;
        String type;
    }
}

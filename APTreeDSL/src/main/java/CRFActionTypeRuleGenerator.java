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
 * CRFActionTypeRuleGenerator - Reads CRFActionTypes model and generates grammar rules for CRFTypesCon.mc4
 * Uses regex-based parsing to handle custom types (Stack, Cassette, etc.) without grammar constraints.
 *
 * Usage: Provide input model path and output grammar path as arguments.
 * Defaults:
 *  - Input: src/test/resources/valid/CRFTypes/LiveMatActionTypes.bt
 *  - Output: src/main/grammars/CRFTypesCon.mc4
 */
public class CRFActionTypeRuleGenerator {

    private static final String DEFAULT_INPUT_PATH = "src/test/resources/valid/CRFTypes/LiveMatActionTypes.bt";
    private static final String DEFAULT_OUTPUT_PATH = "src/main/grammars/CRFTypesCon.mc4";

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
                    if (isPrimitiveType(prop.type)) {
                        // String → Name in MontiCore grammar; other primitives keep their type
                        String grammarType = prop.type.equals("String") ? "Name" : prop.type;
                        params.add(prop.name + ":" + grammarType);
                    } else {
                        params.add(prop.name + ":Name@" + prop.type);
                    }
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
                String astrule = "astrule " + action.name + " = method public crftypesdef._ast.ASTActionLevel getActLevel() { if(actLevel==null){ actLevel=crftypesdef._ast.ASTActionLevel." + actLevel + "; } return actLevel; } ;";
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

            // 5. Merge new rules with existing ones (append new, overwrite same name)
            int startPos = grammarContent.indexOf(START_MARKER);
            int endPos = grammarContent.indexOf(END_MARKER);
            
            // Parse existing rules keyed by action name (each action = rule line + astrule line)
            java.util.LinkedHashMap<String, String> mergedRules = new java.util.LinkedHashMap<>();
            
            if (startPos >= 0 && endPos > startPos) {
                String existingBlock = grammarContent.substring(startPos + START_MARKER.length(), endPos).trim();
                // Group lines by action name: rule line and its astrule line
                String currentName = null;
                StringBuilder currentGroup = new StringBuilder();
                for (String line : existingBlock.split("\\r?\\n")) {
                    String trimmed = line.trim();
                    if (trimmed.isEmpty()) continue;
                    
                    if (trimmed.startsWith("astrule ")) {
                        // Belongs to current action, append to group
                        if (currentName != null) {
                            currentGroup.append(trimmed).append("\n");
                        }
                    } else {
                        // New rule line - save previous group first
                        if (currentName != null) {
                            mergedRules.put(currentName, currentGroup.toString().trim());
                        }
                        // Extract name: "ActionName extends PActionNode ..."
                        String[] tokens = trimmed.split("\\s+");
                        currentName = tokens.length > 0 ? tokens[0] : null;
                        currentGroup = new StringBuilder();
                        currentGroup.append(trimmed).append("\n");
                    }
                }
                // Save last group
                if (currentName != null) {
                    mergedRules.put(currentName, currentGroup.toString().trim());
                }
            }
            
            // Merge new rules (overwrite same name, append new)
            String currentNewName = null;
            StringBuilder currentNewGroup = new StringBuilder();
            for (String rule : rules) {
                String trimmed = rule.trim();
                if (trimmed.isEmpty()) {
                    // Save current group on blank separator
                    if (currentNewName != null) {
                        if (mergedRules.containsKey(currentNewName)) {
                            System.out.println("  Overwriting existing action: " + currentNewName);
                        } else {
                            System.out.println("  Adding new action: " + currentNewName);
                        }
                        mergedRules.put(currentNewName, currentNewGroup.toString().trim());
                        currentNewName = null;
                        currentNewGroup = new StringBuilder();
                    }
                    continue;
                }
                if (trimmed.startsWith("astrule ")) {
                    currentNewGroup.append(trimmed).append("\n");
                } else {
                    // New rule line - save previous group
                    if (currentNewName != null) {
                        if (mergedRules.containsKey(currentNewName)) {
                            System.out.println("  Overwriting existing action: " + currentNewName);
                        } else {
                            System.out.println("  Adding new action: " + currentNewName);
                        }
                        mergedRules.put(currentNewName, currentNewGroup.toString().trim());
                    }
                    String[] tokens = trimmed.split("\\s+");
                    currentNewName = tokens.length > 0 ? tokens[0] : null;
                    currentNewGroup = new StringBuilder();
                    currentNewGroup.append(trimmed).append("\n");
                }
            }
            // Save last new group
            if (currentNewName != null) {
                if (mergedRules.containsKey(currentNewName)) {
                    System.out.println("  Overwriting existing action: " + currentNewName);
                } else {
                    System.out.println("  Adding new action: " + currentNewName);
                }
                mergedRules.put(currentNewName, currentNewGroup.toString().trim());
            }
            
            // Build merged block
            StringBuilder newBlock = new StringBuilder();
            newBlock.append(START_MARKER).append("\n");
            boolean first = true;
            for (String ruleGroup : mergedRules.values()) {
                if (!first) newBlock.append("\n");
                newBlock.append(ruleGroup).append("\n");
                first = false;
            }
            newBlock.append(END_MARKER).append("\n");

            // 6. Replace or insert the block
            String updatedContent;
            if (startPos >= 0 && endPos > startPos) {
                updatedContent = grammarContent.substring(0, startPos) 
                    + newBlock.toString()
                    + grammarContent.substring(endPos + END_MARKER.length());
            } else {
                // Append before the last closing brace
                int lastBrace = grammarContent.lastIndexOf('}');
                if (lastBrace != -1) {
                    updatedContent = grammarContent.substring(0, lastBrace) 
                        + newBlock.toString()
                        + grammarContent.substring(lastBrace);
                } else {
                    updatedContent = grammarContent + "\n" + newBlock.toString();
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

    private static final java.util.Set<String> PRIMITIVE_TYPES = java.util.Set.of(
        "String", "Boolean", "Integer", "Double", "Float", "Long", "int", "boolean", "double", "float"
    );

    private static boolean isPrimitiveType(String type) {
        return PRIMITIVE_TYPES.contains(type);
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

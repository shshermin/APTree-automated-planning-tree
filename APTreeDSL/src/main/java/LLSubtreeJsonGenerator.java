import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.nio.file.Files;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import org.json.simple.JSONArray;
import org.json.simple.JSONObject;

/**
 * LLSubtreeJsonGenerator - Parses LL subtree .bt files and generates JSON
 * for the C# execution engine.
 *
 * Each BehaviorTree block maps to one ML action's LL expansion. The output
 * JSON contains the subtree name, the list of LL steps with their action type,
 * instance name, ML parameter references, and Cont (configuration) values.
 *
 * Usage:
 *   gradle runLLSubtreeJsonGenerator -PtreeName=Demonstrator
 *   gradle runLLSubtreeJsonGenerator -PinputModel=path/to/LLSubtrees.bt -PoutputPath=path/to/output.json
 *
 * Output format:
 * {
 *   "llSubtrees": [
 *     {
 *       "name": "PickUpLLSubtree",
 *       "mlAction": "PickUpML",
 *       "steps": [
 *         {
 *           "actionType": "MoveToLL",
 *           "instanceName": "moveToPickPosition",
 *           "mlParams": ["p", "client"],
 *           "contParams": { "MoveType": "movel" }
 *         },
 *         ...
 *       ]
 *     },
 *     ...
 *   ]
 * }
 */
public class LLSubtreeJsonGenerator {

    private static final String DEFAULT_INPUT = "src/test/resources/valid/behavior_trees/DemonstratorLLSubtrees.bt";
    private static final String DEFAULT_OUTPUT = "../APTreeExecutionEngine/src/ModelLoader/LLSubtrees.json";

    // LL action type definitions: action name → ordered list of (paramName, isCont)
    // Built from DemonstratorActionTypes.bt LL definitions
    private static final Map<String, List<ParamDef>> LL_ACTION_SCHEMAS = new LinkedHashMap<>();

    static {
        // MoveToLL: target:Location, client:Robot, cont moveType:String
        addSchema("MoveToLL", new String[]{"target", "client"}, new String[]{"moveType"});
        // CloseGripperLL: client:Robot, cont DigitalOutput:Integer
        addSchema("CloseGripperLL", new String[]{"client"}, new String[]{"DigitalOutput"});
        // OpenGripperLL: client:Robot, cont DigitalOutput:Integer
        addSchema("OpenGripperLL", new String[]{"client"}, new String[]{"DigitalOutput"});
        // LiftLL: client:Robot, cont moveType:String
        addSchema("LiftLL", new String[]{"client"}, new String[]{"moveType"});
        // EquipToolLL: client:Robot, tool:Tool
        addSchema("EquipToolLL", new String[]{"client", "tool"}, new String[]{});
        // DeequipToolLL: client:Robot, tool:Tool
        addSchema("DeequipToolLL", new String[]{"client", "tool"}, new String[]{});
        // NailingLL: client:Robot, tool:StaplerGun, nailloc:Location, cont moveType:String
        addSchema("NailingLL", new String[]{"client", "tool", "nailloc"}, new String[]{"moveType"});
        // LowerLL: client:Robot, obj:Element, cont moveType:String
        addSchema("LowerLL", new String[]{"client", "obj"}, new String[]{"moveType"});
        // RetractLL: client:Robot, cont moveType:String
        addSchema("RetractLL", new String[]{"client"}, new String[]{"moveType"});
        // DeactivateToolLL: client:Robot, tool:Tool
        addSchema("DeactivateToolLL", new String[]{"client", "tool"}, new String[]{});
        // InitializeLL: client:Robot
        addSchema("InitializeLL", new String[]{"client"}, new String[]{});
    }

    private static void addSchema(String actionName, String[] mlParams, String[] contParams) {
        List<ParamDef> params = new ArrayList<>();
        for (String p : mlParams) params.add(new ParamDef(p, false));
        for (String p : contParams) params.add(new ParamDef(p, true));
        LL_ACTION_SCHEMAS.put(actionName, params);
    }

    // ──────────────────────────────────────────────────────────────────────────

    public static void main(String[] args) {
        String inputPath = DEFAULT_INPUT;
        String outputPath = DEFAULT_OUTPUT;

        if (args.length >= 1) inputPath = args[0];
        if (args.length >= 2) outputPath = args[1];

        System.out.println("=== LL SUBTREE JSON GENERATOR ===");
        System.out.println("Input:  " + inputPath);
        System.out.println("Output: " + outputPath);

        try {
            LLSubtreeJsonGenerator generator = new LLSubtreeJsonGenerator();
            generator.generate(inputPath, outputPath);
        } catch (Exception e) {
            System.err.println("[ERROR] " + e.getMessage());
            e.printStackTrace();
        }
    }

    @SuppressWarnings("unchecked")
    public void generate(String inputPath, String outputPath) throws IOException {
        File inputFile = new File(inputPath);
        if (!inputFile.exists()) {
            throw new IOException("Input file not found: " + inputPath);
        }

        String content = new String(Files.readAllBytes(inputFile.toPath()));

        List<LLSubtreeDef> subtrees = parseSubtrees(content);
        System.out.println("✓ Parsed " + subtrees.size() + " LL subtree(s)");

        // Build JSON
        JSONObject root = new JSONObject();
        JSONArray subtreesArray = new JSONArray();

        for (LLSubtreeDef subtree : subtrees) {
            JSONObject subtreeJson = new JSONObject();
            subtreeJson.put("name", subtree.name);
            subtreeJson.put("mlAction", subtree.mlAction);

            JSONArray stepsArray = new JSONArray();
            for (LLStepDef step : subtree.steps) {
                JSONObject stepJson = new JSONObject();
                stepJson.put("actionType", step.actionType);
                stepJson.put("instanceName", step.instanceName);

                if (!step.paramBindings.isEmpty()) {
                    JSONObject bindingsJson = new JSONObject();
                    for (Map.Entry<String, String> entry : step.paramBindings.entrySet()) {
                        bindingsJson.put(entry.getKey(), entry.getValue());
                    }
                    stepJson.put("paramBindings", bindingsJson);
                }

                if (!step.contParams.isEmpty()) {
                    JSONObject contJson = new JSONObject();
                    for (Map.Entry<String, String> entry : step.contParams.entrySet()) {
                        contJson.put(entry.getKey(), entry.getValue());
                    }
                    stepJson.put("contParams", contJson);
                }

                stepsArray.add(stepJson);
            }

            subtreeJson.put("steps", stepsArray);
            subtreesArray.add(subtreeJson);

            System.out.println("  ✓ " + subtree.name + " → " + subtree.mlAction
                + " (" + subtree.steps.size() + " steps)");
        }

        root.put("llSubtrees", subtreesArray);
        root.put("subtreeCount", subtreesArray.size());
        root.put("generatedAt", java.time.LocalDateTime.now().toString());
        root.put("sourceFile", inputPath);

        // Write JSON
        File outputFile = new File(outputPath);
        outputFile.getParentFile().mkdirs();
        try (FileWriter writer = new FileWriter(outputFile)) {
            writer.write(prettyPrint(root));
        }

        System.out.println("[OK] Generated " + outputPath);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Parsing
    // ──────────────────────────────────────────────────────────────────────────

    /**
     * Parse all BehaviorTree blocks from the .bt file.
     * Infers the parent ML action name from the subtree name (e.g. PickUpLLSubtree → PickUpML).
     */
    private List<LLSubtreeDef> parseSubtrees(String content) {
        List<LLSubtreeDef> subtrees = new ArrayList<>();

        // Match BehaviorTree blocks
        Pattern treePattern = Pattern.compile(
            "BehaviorTree\\s+(\\w+)\\s*\\{(.+?)\\n\\}",
            Pattern.DOTALL
        );

        Matcher treeMatcher = treePattern.matcher(content);
        while (treeMatcher.find()) {
            String treeName = treeMatcher.group(1);
            String treeBody = treeMatcher.group(2);

            LLSubtreeDef subtree = new LLSubtreeDef();
            subtree.name = treeName;
            subtree.mlAction = inferMLAction(treeName);
            subtree.steps = parseSteps(treeBody);

            subtrees.add(subtree);
        }

        return subtrees;
    }

    /**
     * Infer the parent ML action name from the subtree name.
     * PickUpLLSubtree → PickUpML, StackLLSubtree → StackML, etc.
     */
    private String inferMLAction(String subtreeName) {
        // Remove "LLSubtree" suffix and add "ML"
        if (subtreeName.endsWith("LLSubtree")) {
            return subtreeName.substring(0, subtreeName.length() - "LLSubtree".length()) + "ML";
        }
        return subtreeName;
    }

    /**
     * Parse Action lines within a BehaviorTree body.
     * Matches: Action ActionType instanceName (param1 param2 "literal" 42)
     */
    private List<LLStepDef> parseSteps(String body) {
        List<LLStepDef> steps = new ArrayList<>();

        // Match Action lines: Action <Type> <name> (<params>)
        Pattern actionPattern = Pattern.compile(
            "Action\\s+(\\w+)\\s+(\\w+)\\s*\\(([^)]*)\\)"
        );

        Matcher actionMatcher = actionPattern.matcher(body);
        while (actionMatcher.find()) {
            String actionType = actionMatcher.group(1);
            String instanceName = actionMatcher.group(2);
            String paramsStr = actionMatcher.group(3).trim();

            LLStepDef step = new LLStepDef();
            step.actionType = actionType;
            step.instanceName = instanceName;

            // Parse positional parameters and classify as ML refs or Cont values
            classifyParams(step, actionType, paramsStr);

            steps.add(step);
        }

        return steps;
    }

    /**
     * Classify positional parameters into ML references and Cont configuration values
     * using the LL action schema.
     *
     * ML references are bare names (e.g. "client", "p", "objposition").
     * Cont values are quoted strings ("movel") or bare integers (0, 1).
     */
    private void classifyParams(LLStepDef step, String actionType, String paramsStr) {
        if (paramsStr.isEmpty()) return;

        // Tokenize: quoted strings stay as one token, otherwise split on whitespace
        List<String> tokens = tokenize(paramsStr);

        List<ParamDef> schema = LL_ACTION_SCHEMAS.get(actionType);
        if (schema == null) {
            System.err.println("  ⚠ Unknown LL action type: " + actionType + ", treating all params as ML refs");
            for (String tok : tokens)
                step.paramBindings.put(tok, "{" + tok + "}");
            return;
        }

        int tokenIdx = 0;
        for (ParamDef paramDef : schema) {
            if (tokenIdx >= tokens.size()) break;

            String token = tokens.get(tokenIdx);
            if (paramDef.isCont) {
                // Cont parameter — store as configuration value (strip quotes if present)
                String value = stripQuotes(token);
                step.contParams.put(paramDef.name, value);
            } else {
                // ML parameter — map LL schema param name → {mlPropName} placeholder
                step.paramBindings.put(paramDef.name, "{" + token + "}");
            }
            tokenIdx++;
        }

        // Warn if there are extra tokens
        if (tokenIdx < tokens.size()) {
            System.err.println("  ⚠ Extra parameters for " + actionType + ": "
                + tokens.subList(tokenIdx, tokens.size()));
        }
    }

    /**
     * Tokenize parameter string, keeping quoted strings as single tokens.
     * E.g. 'p client "movel"' → ["p", "client", "\"movel\""]
     */
    private List<String> tokenize(String input) {
        List<String> tokens = new ArrayList<>();
        Pattern tokenPattern = Pattern.compile("\"[^\"]*\"|\\S+");
        Matcher m = tokenPattern.matcher(input);
        while (m.find()) {
            tokens.add(m.group());
        }
        return tokens;
    }

    private String stripQuotes(String s) {
        if (s.startsWith("\"") && s.endsWith("\"") && s.length() >= 2) {
            return s.substring(1, s.length() - 1);
        }
        return s;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // JSON formatting
    // ──────────────────────────────────────────────────────────────────────────

    /**
     * Simple JSON pretty-printer (2-space indent).
     */
    private String prettyPrint(JSONObject json) {
        return prettyPrint(json, 0);
    }

    @SuppressWarnings("unchecked")
    private String prettyPrint(Object obj, int indent) {
        String pad = " ".repeat(indent);
        String padInner = " ".repeat(indent + 2);
        StringBuilder sb = new StringBuilder();

        if (obj instanceof JSONObject) {
            JSONObject json = (JSONObject) obj;
            sb.append("{\n");
            List<String> keys = new ArrayList<>(json.keySet());
            for (int i = 0; i < keys.size(); i++) {
                String key = keys.get(i);
                sb.append(padInner).append("\"").append(key).append("\": ");
                sb.append(prettyPrint(json.get(key), indent + 2));
                if (i < keys.size() - 1) sb.append(",");
                sb.append("\n");
            }
            sb.append(pad).append("}");
        } else if (obj instanceof JSONArray) {
            JSONArray arr = (JSONArray) obj;
            if (arr.isEmpty()) {
                sb.append("[]");
            } else if (arr.size() == 1 && !(arr.get(0) instanceof JSONObject) && !(arr.get(0) instanceof JSONArray)) {
                sb.append("[").append(prettyPrint(arr.get(0), 0)).append("]");
            } else {
                sb.append("[\n");
                for (int i = 0; i < arr.size(); i++) {
                    sb.append(padInner).append(prettyPrint(arr.get(i), indent + 2));
                    if (i < arr.size() - 1) sb.append(",");
                    sb.append("\n");
                }
                sb.append(pad).append("]");
            }
        } else if (obj instanceof String) {
            sb.append("\"").append(obj).append("\"");
        } else if (obj instanceof Number) {
            sb.append(obj);
        } else {
            sb.append("\"").append(obj).append("\"");
        }

        return sb.toString();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Data classes
    // ──────────────────────────────────────────────────────────────────────────

    private static class ParamDef {
        String name;
        boolean isCont;
        ParamDef(String name, boolean isCont) {
            this.name = name;
            this.isCont = isCont;
        }
    }

    private static class LLSubtreeDef {
        String name;
        String mlAction;
        List<LLStepDef> steps = new ArrayList<>();
    }

    private static class LLStepDef {
        String actionType;
        String instanceName;
        Map<String, String> paramBindings = new LinkedHashMap<>();  // llParamName → {mlPropName}
        Map<String, String> contParams = new LinkedHashMap<>();
    }
}

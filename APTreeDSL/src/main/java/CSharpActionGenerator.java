import java.io.File;
import java.io.FileNotFoundException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Optional;

import crftypesdef.CRFTypesDefMill;
import crftypesdef._ast.ASTProperty;
import crftypesdef._ast.ASTWorld;
import crftypesdef._parser.CRFTypesDefParser;
import de.se_rwth.commons.logging.Log;

/**
 * CSharpActionGenerator
 * Generates C# classes from MontiCore Action Type Definitions (.bt files).
 *
 * Usage:
 *   java CSharpActionGenerator {@code <input-bt-file> <output-csharp-dir>}
 */
public class CSharpActionGenerator {

    private static final String DEFAULT_INPUT_PATH = "src/test/resources/valid/CRFTypes/LiveMatActionTypes.bt";
    private static final String DEFAULT_OUTPUT_DIR = "generated_csharp/actions/";
    private static final String DEFAULT_NAMESPACE = "BehaviorTreeMainProject";

    public static void main(String[] args) {
        try {
            System.out.println("=== CSHARP ACTION GENERATOR ===");

            String inputPath = args.length > 0 ? args[0] : DEFAULT_INPUT_PATH;
            String outputDir = args.length > 1 ? args[1] : DEFAULT_OUTPUT_DIR;
            String namespace = DEFAULT_NAMESPACE;

            System.out.println("Input Model: " + inputPath);
            System.out.println("Output Dir:  " + outputDir);

            // 1. Initialize Mill
            CRFTypesDefMill.init();

            // 2. Parse input
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

            // 3. Ensure output directory exists
            Files.createDirectories(Paths.get(outputDir));

            // 4. Generate C# Classes
            int count = 0;
            // Iterate over Action Type Definitions
            for (var def : world.getActionTypeDefinitionList()) {
                String className = def.getName();
                String superClass = "PActionNode"; 

                StringBuilder cs = new StringBuilder();

                // Namespace
                cs.append("using System;\n");
                cs.append("using System.Collections.Generic;\n");
                cs.append("using ModelLoader.ParameterTypes;\n");
                cs.append("using ModelLoader.PredicateTypes;\n\n");
                cs.append("namespace ").append(namespace).append("\n");
                cs.append("{\n");
                cs.append("    public class ").append(className).append(" : ").append(superClass).append("\n");
                cs.append("    {\n");

                // Properties/Arguments of the action with private set
                java.util.List<ASTProperty> propertyList = def.getPropertyList();
                for (ASTProperty prop : propertyList) {
                    String propName = prop.getName();
                    String propType = mapTypeToCSharp(prop.getType().getName());
                    boolean isList = prop.isIsList();

                    cs.append("        // Parameter: ").append(propName).append(" of type ").append(propType).append("\n");
                    cs.append("        public ");
                    if (isList) {
                        cs.append("List<").append(propType).append("> ");
                    } else {
                        cs.append(propType).append(" ");
                    }
                    cs.append(propName).append(" { get; private set; }\n\n");
                }

                // Add preconditions and effects state fields
                cs.append("        // Preconditions and Effects as State objects\n");
                cs.append("        private State preconditions;\n");
                cs.append("        private State effects;\n\n");

                // Constructor
                cs.append("        public ").append(className).append("(string actionType, string instanceName, Blackboard<FastName> blackboard");
                for (ASTProperty prop : propertyList) {
                    String propName = prop.getName();
                    String propType = mapTypeToCSharp(prop.getType().getName());
                    boolean isList = prop.isIsList();
                    cs.append(", ");
                    if (isList) {
                        cs.append("List<").append(propType).append("> ");
                    } else {
                        cs.append(propType).append(" ");
                    }
                    cs.append(propName);
                }
                cs.append(")\n");
                cs.append("            : base(actionType, instanceName, blackboard)\n");
                cs.append("        {\n");
                
                // Assign properties
                for (ASTProperty prop : propertyList) {
                    cs.append("            this.").append(prop.getName()).append(" = ").append(prop.getName()).append(";\n");
                }
                cs.append("            InitializePredicates();\n");
                cs.append("        }\n\n");

                // InitializePredicates method
                cs.append("        private void InitializePredicates()\n");
                cs.append("        {\n");
                cs.append("            // Initialize preconditions\n");
                cs.append("            preconditions = new State(StateType.Precondition, new FastName(\"").append(className.toLowerCase()).append("_preconditions\"));\n");
                cs.append("            // Preconditions are populated at runtime from PDDL domain definitions\n");
                cs.append("            // preconditions.AddPredicate(new FastName(\"pre_0\"), new PredicateName(param1, param2, false));\n\n");
                cs.append("            // Initialize effects\n");
                cs.append("            effects = new State(StateType.Effect, new FastName(\"").append(className.toLowerCase()).append("_effects\"));\n");
                cs.append("            // Effects are populated at runtime from PDDL domain definitions\n");
                cs.append("            // effects.AddPredicate(new FastName(\"eff_0\"), new PredicateName(param1, param2, true));\n");
                cs.append("        }\n\n");

                // Property overrides
                cs.append("        protected override State Preconditions => preconditions;\n");
                cs.append("        protected override State Effects => effects;\n");
                
                cs.append("    }\n");
                cs.append("}\n");

                // Write to file
                Path filePath = Paths.get(outputDir, className + ".cs");
                Files.write(filePath, cs.toString().getBytes());
                System.out.println("  Generated: " + filePath);
                count++;
            }

            System.out.println("✓ Generated " + count + " C# action classes.");

        } catch (Exception e) {
            System.err.println("✗ Error: " + e.getMessage());
            e.printStackTrace();
            System.exit(1);
        }
    }

    private static boolean isBasicType(String mcType) {
        if (mcType == null) return false;
        switch (mcType) {
            case "Name":
            case "String":
            case "string":
            case "STRING_VALUE":
            case "int":
            case "Integer":
            case "int32":
            case "boolean":
            case "Boolean":
            case "bool":
            case "BOOLEAN_VALUE":
            case "double":
            case "Double":
            case "float":
            case "Float":
            case "ActionLevel":
                return true;
            default:
                return false;
        }
    }

    /**
     * Maps MontiCore/Basic types to C# types.
     */
    private static String mapTypeToCSharp(String mcType) {
        if (mcType == null) return "object";
        
        switch (mcType) {
            case "Name":
            case "String":
            case "string":
            case "STRING_VALUE":
                return "string";
            
            case "int":
            case "Integer":
            case "int32":
                return "int";
                
            case "boolean":
            case "Boolean":
            case "bool":
            case "BOOLEAN_VALUE":
                return "bool";
                
            case "double":
            case "Double":
                return "double";
                
            case "float":
            case "Float":
                return "float";
                
            case "ActionLevel":
                return "string"; // Enum mapping usually simpler as string in generation unless logic exists

            default:
                // Custom domain type (e.g. Layer) - pass through as-is
                return mcType;
        }
    }
}

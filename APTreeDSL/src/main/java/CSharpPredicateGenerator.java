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
 * CSharpPredicateGenerator
 * Generates C# classes from MontiCore Predicate Type Definitions (.bt files).
 *
 * Usage:
 *   java CSharpPredicateGenerator <input-bt-file> <output-csharp-dir>
 */
public class CSharpPredicateGenerator {

    private static final String DEFAULT_INPUT_PATH = "src/test/resources/valid/CRFTypes/LiveMatPredicaetTypes.bt";
    private static final String DEFAULT_OUTPUT_DIR = "generated_csharp/predicates/";
    private static final String DEFAULT_NAMESPACE = "BehaviorTree.Predicates";

    public static void main(String[] args) {
        try {
            System.out.println("=== CSHARP PREDICATE GENERATOR ===");

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
            // Iterate over Predicate Type Definitions
            for (var def : world.getPredicateTypeDefinitionList()) {
                String className = def.getName();
                // Assuming all predicates extend a base Predicate class in C# or interface
                String superType = "Predicate"; 
                
                StringBuilder cs = new StringBuilder();

                // Namespace
                cs.append("using System;\n");
                cs.append("using System.Collections.Generic;\n");
                cs.append("using BehaviorTree.Types;\n\n"); // Assuming Property Types are here
                cs.append("namespace ").append(namespace).append(" {\n\n");

                // Class Declaration
                cs.append("    public class ").append(className);
                if (superType != null && !superType.isEmpty()) {
                    cs.append(" : ").append(superType);
                }
                cs.append(" {\n");

                // Add not property
                cs.append("        public bool not { get; set; }\n");

                // Properties/Arguments of the predicate
                for (ASTProperty prop : def.getPropertyList()) {
                    String propName = prop.getName();
                    String propType = mapTypeToCSharp(prop.getType().getName());
                    boolean isList = prop.isIsList();

                    cs.append("        public ");
                    if (isList) {
                        cs.append("List<").append(propType).append("> ");
                    } else {
                        cs.append(propType).append(" ");
                    }
                    cs.append(propName).append(" { get; set; }\n");
                }

                // Constructor
                cs.append("\n        public ").append(className).append("(");
                
                // Constructor arguments
                java.util.List<ASTProperty> propertyList = def.getPropertyList();
                for (int i = 0; i < propertyList.size(); i++) {
                    ASTProperty prop = propertyList.get(i);
                    String propName = prop.getName();
                    String propType = mapTypeToCSharp(prop.getType().getName());
                    boolean isList = prop.isIsList();

                    if (isList) {
                        cs.append("List<").append(propType).append("> ");
                    } else {
                        cs.append(propType).append(" ");
                    }
                    cs.append(propName).append(", ");
                }
                cs.append("bool isNegated) : base(isNegated)\n");
                cs.append("        {\n");
                cs.append("            PredicateType = new FastName(\"").append(className.toLowerCase()).append("\");\n");
                
                // Assign properties
                for (ASTProperty prop : propertyList) {
                    cs.append("            this.").append(prop.getName()).append(" = ").append(prop.getName()).append(";\n");
                }
                cs.append("            this.PredicateName = GetUniqueKey();\n");
                cs.append("        }\n");

                // Override GetParameterValues
                cs.append("\n        public override List<string> GetParameterValues()\n");
                cs.append("        {\n");
                cs.append("            return new List<string>\n");
                cs.append("            {\n");
                
                for (int i = 0; i < propertyList.size(); i++) {
                    ASTProperty prop = propertyList.get(i);
                    String propName = prop.getName();
                    String propType = mapTypeToCSharp(prop.getType().getName());
                    boolean isList = prop.isIsList();
                    boolean isBasicType = isBasicType(prop.getType().getName());

                    cs.append("                ");
                    if (isList) {
                         // Handling lists might be tricky with NameKey, assume robust ToString or similar
                         // For now just outputting something basic or skipping deep list handling as per simple example
                         cs.append(propName).append("?.ToString() ?? \"null\"");
                    } else if (isBasicType) {
                         cs.append(propName).append(".ToString()");
                    } else {
                         // Check if likely nullable or complex
                         cs.append(propName).append("?.NameKey?.ToString() ?? \"null\"");
                    }
                    
                    if (i < propertyList.size() - 1) {
                        cs.append(",\n");
                    } else {
                        cs.append("\n");
                    }
                }
                cs.append("            };\n");
                cs.append("        }\n");

                cs.append("    }\n");
                cs.append("}\n");

                // Write to file
                Path filePath = Paths.get(outputDir, className + ".cs");
                Files.write(filePath, cs.toString().getBytes());
                System.out.println("  Generated: " + filePath);
                count++;
            }

            System.out.println("✓ Generated " + count + " C# predicate classes.");

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

            default:
                // Assume it's a custom type (e.g. Layer)
                return mcType;
        }
    }
}

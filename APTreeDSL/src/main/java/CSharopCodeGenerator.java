import crftypesdef.CRFTypesDefMill;
import crftypesdef._ast.ASTProperty;
import crftypesdef._ast.ASTWorld;
import crftypesdef._parser.CRFTypesDefParser;
import de.se_rwth.commons.logging.Log;

import java.io.File;
import java.io.FileNotFoundException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Optional;

/**
 * CSharopCodeGenerator (Intended: CSharpCodeGenerator)
 * Generates C# classes from MontiCore Property Type Definitions (.bt files).
 *
 * Usage:
 *   java CSharopCodeGenerator {@code <input-bt-file> <output-csharp-dir>}
 */
public class CSharopCodeGenerator {

    private static final String DEFAULT_INPUT_PATH = "src/test/resources/valid/CRFTypes/LiveMatPropertyTypes.bt";
    private static final String DEFAULT_OUTPUT_DIR = "generated_csharp/GeneratedPropertyTypes";
    private static final String DEFAULT_NAMESPACE = "BehaviorTree.Types";

    public static void main(String[] args) {
        try {
            System.out.println("=== CSHARP CODE GENERATOR ===");

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
            // Iterate over Property Type Definitions
            for (var def : world.getPropertyTypeDefinitionList()) {
                String className = def.getName();
                String superType = def.getSuperType();
                
                StringBuilder cs = new StringBuilder();

                // Namespace
                cs.append("using System;\n");
                cs.append("using System.Collections.Generic;\n\n");
                cs.append("namespace ").append(namespace).append(" {\n\n");

                // Class Declaration
                cs.append("    public class ").append(className);
                if (superType != null && !superType.isEmpty()) {
                    cs.append(" : ").append(superType);
                }
                cs.append(" {\n");

                // Properties
                for (ASTProperty prop : def.getPropertyList()) {
                    String propName = prop.getName();
                    String propType = mapTypeToCSharp(prop.getType().getName());
                    boolean isList = prop.isIsList(); // Extracted from isList?"+"? in grammar

                    cs.append("        public ");
                    if (isList) {
                        cs.append("List<").append(propType).append("> ");
                    } else {
                        cs.append(propType).append(" ");
                    }
                    cs.append(propName).append(" { get; set; }\n");
                }

                cs.append("    }\n");
                cs.append("}\n");

                // Write to file
                Path filePath = Paths.get(outputDir, className + ".cs");
                Files.write(filePath, cs.toString().getBytes());
                System.out.println("  Generated: " + filePath);
                count++;
            }

            System.out.println("✓ Generated " + count + " C# classes.");

        } catch (Exception e) {
            System.err.println("✗ Error: " + e.getMessage());
            e.printStackTrace();
            System.exit(1);
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
                // Custom domain type (e.g. Layer) - pass through as-is
                return mcType;
        }
    }
}

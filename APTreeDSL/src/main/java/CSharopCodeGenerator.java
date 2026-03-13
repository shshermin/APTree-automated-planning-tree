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
 * CSharopCodeGenerator (Intended: CSharpCodeGenerator)
 * Generates C# classes from MontiCore Property Type Definitions (.bt files).
 *
 * Usage:
 *   java CSharopCodeGenerator {@code <input-bt-file> <output-csharp-dir>}
 */
public class CSharopCodeGenerator {

    private static final String DEFAULT_INPUT_PATH = "src/test/resources/valid/CRFTypes/LiveMatPropertyTypes.bt";
    private static final String DEFAULT_OUTPUT_DIR = "../APTreeExecutionEngine/src/ModelLoader/ParameterTypes";
    private static final String DEFAULT_NAMESPACE = "ModelLoader.ParameterTypes";

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
                java.util.List<ASTProperty> properties = def.getPropertyList();
                
                StringBuilder cs = new StringBuilder();

                // Using statements
                cs.append("using System;\n");
                cs.append("using System.Collections.Generic;\n\n");

                // Namespace (new line brace style)
                cs.append("namespace ").append(namespace).append("\n{\n");

                // Class Declaration
                cs.append("    public class ").append(className);
                if (superType != null && !superType.isEmpty()) {
                    cs.append(" : ").append(superType);
                }
                cs.append("\n    {\n");

                // Properties (PascalCase)
                for (ASTProperty prop : properties) {
                    String propName = toPascalCase(prop.getName());
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

                // Empty line before constructors
                cs.append("\n");

                // Empty constructor
                cs.append("        // Empty constructor - required by CustomProperty\n");
                cs.append("        public ").append(className).append("() : base()\n");
                cs.append("        {\n");
                if (superType != null && !superType.isEmpty()) {
                    cs.append("            BaseType = new FastName(\"").append(superType).append("\");\n");
                    cs.append("            // TypeName is automatically set in base constructor\n");
                }
                cs.append("        }\n");

                // Constructor with parameters (only if there are properties)
                if (!properties.isEmpty()) {
                    cs.append("\n");
                    cs.append("        // Constructor with parameters\n");
                    cs.append("        public ").append(className).append("(");
                    for (int i = 0; i < properties.size(); i++) {
                        if (i > 0) cs.append(", ");
                        String propType = mapTypeToCSharp(properties.get(i).getType().getName());
                        boolean isList = properties.get(i).isIsList();
                        String propName = toCamelCase(properties.get(i).getName());
                        if (isList) {
                            cs.append("List<").append(propType).append("> ");
                        } else {
                            cs.append(propType).append(" ");
                        }
                        cs.append(propName);
                    }
                    cs.append(") : this()\n");
                    cs.append("        {\n");
                    for (ASTProperty prop : properties) {
                        String pascalName = toPascalCase(prop.getName());
                        String camelName = toCamelCase(prop.getName());
                        cs.append("            this.").append(pascalName).append(" = ").append(camelName).append(";\n");
                    }
                    cs.append("        }\n");

                    // Constructor with name and parameters
                    cs.append("\n");
                    cs.append("        // Constructor with name and parameters\n");
                    cs.append("        public ").append(className).append("(string name, ");
                    for (int i = 0; i < properties.size(); i++) {
                        if (i > 0) cs.append(", ");
                        String propType = mapTypeToCSharp(properties.get(i).getType().getName());
                        boolean isList = properties.get(i).isIsList();
                        String propName = toCamelCase(properties.get(i).getName());
                        if (isList) {
                            cs.append("List<").append(propType).append("> ");
                        } else {
                            cs.append(propType).append(" ");
                        }
                        cs.append(propName);
                    }
                    cs.append(") : base(name)\n");
                    cs.append("        {\n");
                    for (ASTProperty prop : properties) {
                        String pascalName = toPascalCase(prop.getName());
                        String camelName = toCamelCase(prop.getName());
                        cs.append("            this.").append(pascalName).append(" = ").append(camelName).append(";\n");
                    }
                    if (superType != null && !superType.isEmpty()) {
                        cs.append("            BaseType = new FastName(\"").append(superType).append("\");\n");
                        cs.append("            // TypeName is automatically set in base constructor\n");
                    }
                    cs.append("        }\n");
                }

                // SetParameters override
                cs.append("\n");
                cs.append("        // Override SetParameters to set ").append(className).append("-specific properties\n");
                cs.append("        public override void SetParameters(Dictionary<string, object> parameters)\n");
                cs.append("        {\n");
                cs.append("            // Call base implementation first\n");
                cs.append("            base.SetParameters(parameters);\n");

                for (ASTProperty prop : properties) {
                    String pascalName = toPascalCase(prop.getName());
                    String camelName = toCamelCase(prop.getName());
                    String propType = mapTypeToCSharp(prop.getType().getName());
                    boolean isList = prop.isIsList();
                    String fullType = isList ? "List<" + propType + ">" : propType;

                    cs.append("\n");
                    cs.append("            // Set ").append(pascalName).append(" property\n");
                    cs.append("            if (parameters.ContainsKey(\"").append(camelName).append("\"))\n");
                    cs.append("            {\n");
                    cs.append(generateSetParameterBody(pascalName, camelName, fullType));
                    cs.append("            }\n");
                }

                cs.append("\n");
                cs.append("        }\n");

                // Close class and namespace
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
     * Generates the body of a SetParameters if-block for a given property.
     */
    private static String generateSetParameterBody(String pascalName, String camelName, String csharpType) {
        switch (csharpType) {
            case "int":
                return "                " + pascalName + " = Convert.ToInt32(parameters[\"" + camelName + "\"]);\n";
            case "double":
                return "                " + pascalName + " = Convert.ToDouble(parameters[\"" + camelName + "\"]);\n";
            case "float":
                return "                " + pascalName + " = Convert.ToSingle(parameters[\"" + camelName + "\"]);\n";
            case "bool":
                return "                " + pascalName + " = Convert.ToBoolean(parameters[\"" + camelName + "\"]);\n";
            case "string":
                return "                " + pascalName + " = parameters[\"" + camelName + "\"].ToString();\n";
            default:
                // Custom type - use cast
                return "                if (parameters[\"" + camelName + "\"] is " + csharpType + " " + camelName + "Value)\n"
                     + "                {\n"
                     + "                    " + pascalName + " = " + camelName + "Value;\n"
                     + "                }\n";
        }
    }

    /**
     * Converts a string to PascalCase (first letter uppercase).
     */
    private static String toPascalCase(String name) {
        if (name == null || name.isEmpty()) return name;
        return Character.toUpperCase(name.charAt(0)) + name.substring(1);
    }

    /**
     * Converts a string to camelCase (first letter lowercase).
     */
    private static String toCamelCase(String name) {
        if (name == null || name.isEmpty()) return name;
        return Character.toLowerCase(name.charAt(0)) + name.substring(1);
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

import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

import org.json.simple.JSONArray;
import org.json.simple.JSONObject;

import crftypescon.CRFTypesConMill;
import crftypescon._ast.ASTWorld;
import crftypescon._parser.CRFTypesConParser;
import de.se_rwth.commons.logging.Log;

/**
 * LiveMatSetupObjectsGenerator - Parses LiveMatSetupObjects.bt using MontiCore grammar
 * 
 * Reads a LiveMatSetupObjects file in APTreeDSL CRFTypesCon grammar format and exports 
 * all parameter instances to JSON format.
 * 
 * Expected grammar format:
 *   Typename instancename (properties)
 *   Example: FirstPos fp1 ()
 *            Robot r1 ()
 *            Beam b1 (fp1)
 */
public class LiveMatSetupObjectsGenerator {

  private static class ParameterInstance {
    String name;
    String type;
    
    ParameterInstance(String type, String name) {
      this.type = type;
      this.name = name;
    }
  }

  public static void main(String[] args) {
    Log.init();
    Log.enableFailQuick(false);
    
    LiveMatSetupObjectsGenerator generator = new LiveMatSetupObjectsGenerator();
    String filePath = args.length > 0 ? args[0] : "src/test/resources/valid/CRFConcrete/LiveMatSetupObjects.bt";
    String outputPath = args.length > 1 ? args[1] : "target/LiveMatSetupObjects.json";
    
    System.out.println("Processing LiveMatSetupObjects file: " + filePath);
    generator.parseAndExport(filePath, outputPath);
  }

  /**
   * Parse LiveMatSetupObjects.bt file using MontiCore grammar and export instances to JSON
   * 
   * @param inputPath Path to the LiveMatSetupObjects.bt file
   * @param outputPath Path where JSON should be written
   */
  public void parseAndExport(String inputPath, String outputPath) {
    try {
      File inputFile = new File(inputPath);
      if (!inputFile.exists()) {
        System.err.println("[X] File not found: " + inputPath);
        return;
      }

      // Initialize MontiCore Mill
      CRFTypesConMill.init();

      // Parse the file using the MontiCore parser
      System.out.println("[DEBUG] Parsing with CRFTypesConParser...");
      CRFTypesConParser parser = CRFTypesConMill.parser();
      Optional<ASTWorld> parseResult = parser.parse(inputPath);

      if (!parseResult.isPresent()) {
        System.err.println("[X] Failed to parse file: " + inputPath);
        if (Log.getErrorCount() > 0) {
          Log.getFindings().forEach(f -> System.err.println("  Error: " + f.buildMsg()));
        }
        return;
      }

      ASTWorld world = parseResult.get();
      System.out.println("[OK] Successfully parsed LiveMatSetupObjects");

      // Extract parameter instances from the AST
      List<ParameterInstance> instances = extractParameterInstances(world);

      if (instances.isEmpty()) {
        System.err.println("[!] No parameter instances found in file");
      } else {
        System.out.println("[OK] Extracted " + instances.size() + " parameter instances");
      }

      // Export to JSON
      exportInstancesToJSON(instances, outputPath);

    } catch (Exception e) {
      System.err.println("[X] ERROR: " + e.getMessage());
      e.printStackTrace();
    }
  }

  /**
   * Extract parameter instances from the parsed AST
   * 
   * @param world The parsed CRFTypesCon AST
   * @return List of ParameterInstance objects
   */
  private List<ParameterInstance> extractParameterInstances(ASTWorld world) {
    List<ParameterInstance> instances = new ArrayList<>();
    
    // Use reflection to discover all getter methods for symbol types
    var scope = CRFTypesConMill.scopesGenitorDelegator().createFromAST(world);
    
    try {
      java.util.Set<String> loadedNames = new java.util.HashSet<>();
      
      // Iterate over all getLocal...Symbols methods to find all symbol collections
      for (java.lang.reflect.Method method : scope.getClass().getMethods()) {
        String mName = method.getName();
        
        if (mName.startsWith("getLocal") && mName.endsWith("Symbols") && 
            !mName.equals("getLocalSymbols") && method.getParameterCount() == 0) {
          
          try {
            Object result = method.invoke(scope);
            if (result instanceof java.util.Collection) {
              for (Object obj : (java.util.Collection<?>) result) {
                if (obj != null) {
                  try {
                    // Get name via reflection
                    java.lang.reflect.Method getNameMethod = obj.getClass().getMethod("getName");
                    String name = (String) getNameMethod.invoke(obj);
                    
                    if (loadedNames.contains(name)) {
                      continue;
                    }
                    loadedNames.add(name);
                    
                    // Determine type from class name
                    String type = obj.getClass().getSimpleName();
                    if (type.endsWith("Symbol")) {
                      type = type.substring(0, type.length() - 6);
                    }
                    
                    System.out.println("  - Found: " + type + " {" + name + "}");
                    instances.add(new ParameterInstance(type, name));
                    
                  } catch (Exception e) {
                    // Skip if can't extract
                  }
                }
              }
            }
          } catch (Exception e) {
            // Skip inaccessible methods
          }
        }
      }
    } catch (Exception e) {
      System.err.println("[!] Warning: Could not extract instances via reflection: " + e.getMessage());
    }
    
    return instances;
  }

  /**
   * Export parameter instances to a JSON file
   * 
   * @param instances List of ParameterInstance objects
   * @param outputPath Path where JSON should be written
   */
  private void exportInstancesToJSON(List<ParameterInstance> instances, String outputPath) {
    try {
      JSONObject rootJson = new JSONObject();
      JSONArray instancesArray = new JSONArray();

      // Export all instances
      for (ParameterInstance inst : instances) {
        JSONObject instObj = new JSONObject();
        instObj.put("name", inst.name);
        instObj.put("type", inst.type);

        instancesArray.add(instObj);
      }

      rootJson.put("instances", instancesArray);
      rootJson.put("count", instancesArray.size());

      // Write to file with pretty-printing
      try (FileWriter file = new FileWriter(outputPath)) {
        String jsonString = rootJson.toJSONString();
        String prettyJson = prettyPrintJson(jsonString);
        file.write(prettyJson);
        file.flush();
        System.out.println("[OK] Exported " + instances.size() + " parameter instances to: " + outputPath);
      }

    } catch (IOException e) {
      System.err.println("[X] ERROR writing JSON file: " + e.getMessage());
      e.printStackTrace();
    }
  }

  /**
   * Pretty-print JSON for readability
   * 
   * @param jsonString The compact JSON string
   * @return Pretty-printed JSON with indentation
   */
  private String prettyPrintJson(String jsonString) {
    StringBuilder prettified = new StringBuilder();
    int indentLevel = 0;
    boolean inString = false;
    char prevChar = 0;

    for (int i = 0; i < jsonString.length(); i++) {
      char c = jsonString.charAt(i);

      // Handle string boundaries
      if (c == '"' && prevChar != '\\') {
        inString = !inString;
      }

      if (!inString) {
        switch (c) {
          case '{':
          case '[':
            prettified.append(c);
            prettified.append('\n');
            indentLevel++;
            appendIndent(prettified, indentLevel);
            break;
          case '}':
          case ']':
            indentLevel--;
            prettified.append('\n');
            appendIndent(prettified, indentLevel);
            prettified.append(c);
            break;
          case ',':
            prettified.append(c);
            prettified.append('\n');
            appendIndent(prettified, indentLevel);
            break;
          case ':':
            prettified.append(c).append(' ');
            break;
          case ' ':
          case '\n':
          case '\r':
          case '\t':
            // Skip whitespace in compact form
            break;
          default:
            prettified.append(c);
        }
      } else {
        prettified.append(c);
      }

      prevChar = c;
    }

    return prettified.toString();
  }

  /**
   * Append indentation spaces
   * 
   * @param sb StringBuilder to append to
   * @param level Indent level (2 spaces per level)
   */
  private void appendIndent(StringBuilder sb, int level) {
    for (int i = 0; i < level * 2; i++) {
      sb.append(' ');
    }
  }
}

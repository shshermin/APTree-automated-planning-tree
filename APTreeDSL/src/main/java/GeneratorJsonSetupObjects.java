import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

import org.json.simple.JSONArray;
import org.json.simple.JSONObject;

import domaintypescon.DomainTypesConMill;
import domaintypescon._ast.ASTWorld;
import domaintypescon._parser.DomainTypesConParser;
import de.se_rwth.commons.logging.Log;

/**
 * GeneratorJsonSetupObjects - Parses a concrete instances .bt file using MontiCore grammar
 * and exports all parameter instances to JSON format.
 *
 * Convention-based usage (preferred):
 *   java GeneratorJsonSetupObjects &lt;treeName&gt;
 *   Resolves:
 *     Input:  src/test/resources/valid/CRFConcrete/{treeName}SceneObjects.bt
 *     Output: ../APTreeExecutionEngine/src/ModelLoader/{treeName}SetupObjects.json
 *
 * Explicit paths (legacy):
 *   java GeneratorJsonSetupObjects &lt;inputPath&gt; &lt;outputPath&gt;
 *
 * Expected grammar format:
 *   Typename instancename (properties)
 *   Example: FirstPos fp1 ()
 *            Robot r1 ()
 *            Beam b1 (fp1)
 */
public class GeneratorJsonSetupObjects {

  private static final String INSTANCES_DIR = "src/test/resources/valid/CRFConcrete/";
  private static final String OUTPUT_DIR = "../APTreeExecutionEngine/src/ModelLoader/";

  private static class ParameterInstance {
    String name;
    String type;
    String extendsType;
    java.util.Map<String, Object> properties;
    
    ParameterInstance(String type, String name, String extendsType) {
      this.type = type;
      this.name = name;
      this.extendsType = extendsType;
      this.properties = new java.util.HashMap<>();
    }
    
    void addProperty(String propName, Object propValue) {
      this.properties.put(propName, propValue);
    }
  }

  public static void main(String[] args) {
    Log.init();
    Log.enableFailQuick(false);
    
    GeneratorJsonSetupObjects generator = new GeneratorJsonSetupObjects();
    String filePath;
    String outputPath;

    if (args.length == 1) {
      // Convention mode: single treeName argument
      String treeName = args[0];
      filePath = INSTANCES_DIR + treeName + "SceneObjects.bt";
      outputPath = OUTPUT_DIR + treeName + "SetupObjects.json";
      System.out.println("[Convention] Tree: " + treeName);
    } else {
      // Legacy mode: explicit paths
      filePath = args.length > 0 ? args[0] : INSTANCES_DIR + "LiveMatSetupObjects.bt";
      outputPath = args.length > 1 ? args[1] : OUTPUT_DIR + "LiveMatSetupObjects.json";
    }
    
    System.out.println("Processing setup objects file: " + filePath);
    System.out.println("Output: " + outputPath);
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
      DomainTypesConMill.init();

      // Parse the file using the MontiCore parser
      System.out.println("[DEBUG] Parsing with DomainTypesConParser...");
      DomainTypesConParser parser = DomainTypesConMill.parser();
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
   * @param world The parsed DomainTypesCon AST
   * @return List of ParameterInstance objects
   */
  private List<ParameterInstance> extractParameterInstances(ASTWorld world) {
    List<ParameterInstance> instances = new ArrayList<>();
    
    // Use reflection to discover all getter methods for symbol types
    var scope = DomainTypesConMill.scopesGenitorDelegator().createFromAST(world);
    
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
                    
                    // Dynamically extract the extends type from the class hierarchy
                    String extendsType = extractSupertypeFromClass(obj);
                    
                    System.out.println("  - Found: " + type + " {" + name + "} extends " + extendsType);
                    ParameterInstance inst = new ParameterInstance(type, name, extendsType);
                    extractSymbolProperties(obj, inst);
                    instances.add(inst);
                    
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
        instObj.put("extends", inst.extendsType);
        
        // Add properties if any
        if (!inst.properties.isEmpty()) {
          JSONObject propsObj = new JSONObject();
          for (java.util.Map.Entry<String, Object> entry : inst.properties.entrySet()) {
            // Additional safety check - only add truly serializable values
            Object val = entry.getValue();
            if (val instanceof String || val instanceof Number || val instanceof Boolean) {
              propsObj.put(entry.getKey(), val);
            } else if (val != null) {
              // For other types, convert to string representation
              propsObj.put(entry.getKey(), val.toString());
            }
          }
          if (!propsObj.isEmpty()) {
            instObj.put("properties", propsObj);
          }
        }

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

  /**
   * Extract the supertype from an AST symbol by examining its class hierarchy
   * 
   * @param symbol The symbol object
   * @return The supertype name or "Unknown"
   */
  private static String extractSupertypeFromClass(Object symbol) {
    try {
      // Get the AST node from the symbol
      Object astNode = null;
      try {
        java.lang.reflect.Method getAstNodeMethod = symbol.getClass().getMethod("getAstNode");
        astNode = getAstNodeMethod.invoke(symbol);
      } catch (Exception e) {
        // No AST node, try using symbol directly
        astNode = symbol;
      }
      
      if (astNode == null) {
        return "Unknown";
      }
      
      Class<?> clazz = astNode.getClass();
      
      // Check interfaces for type information
      Class<?>[] interfaces = clazz.getInterfaces();
      for (Class<?> iface : interfaces) {
        String ifaceName = iface.getSimpleName();
        // Look for IAST interfaces that are not the node itself
        if (ifaceName.startsWith("IAST") && !ifaceName.equals("IASTNode")) {
          // Extract the type name from IAST prefix
          String typeName = ifaceName.substring(4); // Remove "IAST" prefix
          if (!typeName.isEmpty()) {
            return typeName;
          }
        }
      }
      
      // Check superclass (but skip Object and other framework classes)
      Class<?> superClass = clazz.getSuperclass();
      if (superClass != null && !superClass.equals(Object.class)) {
        String superName = superClass.getSimpleName();
        if (superName.startsWith("AST")) {
          // Remove "AST" prefix
          return superName.substring(3);
        }
        if (superName.startsWith("Abstract")) {
          return superName.substring(8);
        }
      }
      
      return "Unknown";
    } catch (Exception e) {
      return "Unknown";
    }
  }

  /**
   * Extract properties from a symbol using reflection.
   * Attempts to get the AST node from the symbol and extract its properties.
   * 
   * @param symbol The symbol object to extract properties from
   * @param instance The ParameterInstance to store the extracted properties
   */
  private static void extractSymbolProperties(Object symbol, ParameterInstance instance) {
    try {
      // Try to get the AST node from the symbol
      Object astNode = null;
      try {
        java.lang.reflect.Method getAstNodeMethod = symbol.getClass().getMethod("getAstNode");
        astNode = getAstNodeMethod.invoke(symbol);
      } catch (Exception e) {
        // Symbol doesn't have an AST node - that's ok
      }
      
      // Extract properties from the AST node if available
      if (astNode != null) {
        extractPropertiesFromObject(astNode, instance);
      }
      
      // Also try to extract from the symbol itself (in case it has useful properties)
      extractPropertiesFromObject(symbol, instance);
      
    } catch (Exception e) {
      System.err.println("[X] Error extracting properties: " + e.getMessage());
    }
  }

  /**
   * Extract properties from an object using reflection.
   * Only extracts simple, semantic properties that are JSON-serializable.
   * 
   * @param obj The object to extract properties from
   * @param instance The ParameterInstance to store the extracted properties
   */
  private static void extractPropertiesFromObject(Object obj, ParameterInstance instance) {
    if (obj == null) return;
    
    try {
      java.lang.reflect.Method[] methods = obj.getClass().getMethods();
      
      for (java.lang.reflect.Method method : methods) {
        String methodName = method.getName();
        
        // Look for getter methods (getXxx)
        if (methodName.startsWith("get") && 
            methodName.length() > 3 &&
            method.getParameterCount() == 0) {
          
          // Skip these methods
          if (methodName.equals("getName") || methodName.equals("getClass") ||
              methodName.equals("getFullName") || methodName.equals("getPackageName") ||
              methodName.equals("getAstNode") || methodName.equals("getEnclosingScope")) {
            continue;
          }
          
          try {
            Object value = method.invoke(obj);
            String propName = methodName.substring(3);
            propName = propName.substring(0, 1).toLowerCase() + propName.substring(1);
            
            if (value != null) {
              // Skip internal framework properties (start with underscore or specific framework names)
              if (propName.startsWith("_") || 
                  propName.startsWith("Pre") ||
                  propName.startsWith("Post") ||
                  propName.equals("stereoinfo") ||
                  propName.equals("sourcePosition") ||
                  propName.equals("locDefinition") ||
                  propName.equals("accessModifier")) {
                continue;
              }
              
              // Only serialize simple, JSON-friendly types
              if (isJsonSerializable(value)) {
                // Special handling for objects with 'name' - extract the name instead of toString()
                if (hasNameMethod(value)) {
                  try {
                    Object nameValue = value.getClass().getMethod("getName").invoke(value);
                    if (nameValue != null) {
                      instance.addProperty(propName, nameValue.toString());
                    }
                  } catch (Exception e) {
                    // Skip if we can't extract the name
                  }
                } else {
                  instance.addProperty(propName, value);
                }
              }
            }
          } catch (Exception e) {
            // Skip properties that can't be read
          }
        }
      }
    } catch (Exception e) {
      System.err.println("[X] Error extracting properties from object: " + e.getMessage());
    }
  }

  /**
   * Check if an object is JSON-serializable (primitive wrapper, String, etc.)
   * 
   * @param value The value to check
   * @return true if the value can be safely serialized to JSON
   */
  private static boolean isJsonSerializable(Object value) {
    if (value == null) return false;
    
    Class<?> clazz = value.getClass();
    
    // Primitive wrappers and String
    if (clazz == String.class ||
        clazz == Integer.class || clazz == Long.class ||
        clazz == Float.class || clazz == Double.class ||
        clazz == Boolean.class || clazz == Byte.class) {
      return true;
    }
    
    // Collections (but they should be empty/simple)
    if (value instanceof java.util.Collection) {
      java.util.Collection<?> col = (java.util.Collection<?>) value;
      return col.isEmpty();  // Only serialize empty collections
    }
    
    if (value instanceof java.util.Map) {
      java.util.Map<?, ?> map = (java.util.Map<?, ?>) value;
      return map.isEmpty();  // Only serialize empty maps
    }
    
    return false;
  }

  /**
   * Check if an object has a getName() method.
   * 
   * @param value The object to check
   * @return true if the object has a public getName() method
   */
  private static boolean hasNameMethod(Object value) {
    try {
      value.getClass().getMethod("getName");
      return true;
    } catch (NoSuchMethodException e) {
      return false;
    }
  }
}

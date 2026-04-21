import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;

import org.json.simple.JSONArray;
import org.json.simple.JSONObject;

import crftypescon.CRFTypesConMill;
import crftypescon._ast.ASTWorld;
import crftypescon._parser.CRFTypesConParser;
import crftypesdef._ast.ASTPredicate;
import de.se_rwth.commons.logging.Log;

/**
 * GoalStateJsonGenerator - Parses goal state predicate models and exports to JSON.
 *
 * Convention-based usage (preferred):
 *   java GoalStateJsonGenerator &lt;treeName&gt;
 *   Resolves:
 *     Input:  src/test/resources/valid/CRFConcrete/{treeName}GoalState.bt
 *     Output: ../APTreeExecutionEngine/src/ModelLoader/{treeName}GoalState.json
 *
 * Explicit paths (legacy):
 *   java GoalStateJsonGenerator &lt;inputPath&gt; &lt;outputPath&gt;
 */
public class GoalStateJsonGenerator {
  private static final String INSTANCES_DIR = "src/test/resources/valid/CRFConcrete/";
  private static final String OUTPUT_DIR = "../APTreeExecutionEngine/src/ModelLoader/";

  private static class PredicateInstance {
    String type;
    Map<String, Object> properties;

    PredicateInstance(String type) {
      this.type = type;
      this.properties = new HashMap<>();
    }

    void addProperty(String propName, Object propValue) {
      this.properties.put(propName, propValue);
    }
  }

  public static void main(String[] args) {
    Log.init();
    Log.enableFailQuick(false);

    GoalStateJsonGenerator generator = new GoalStateJsonGenerator();
    String filePath;
    String outputPath;

    if (args.length == 1 && !args[0].contains("/") && !args[0].contains("\\") && !args[0].endsWith(".bt")) {
      // Convention mode: single treeName argument
      String treeName = args[0];
      filePath = INSTANCES_DIR + treeName + "GoalState.bt";
      outputPath = OUTPUT_DIR + treeName + "GoalState.json";
      System.out.println("[Convention] Tree: " + treeName);
    } else {
      // Legacy mode: explicit paths
      filePath = args.length > 0 ? args[0] : INSTANCES_DIR + "LiveMatGoalState.bt";
      outputPath = args.length > 1 ? args[1] : OUTPUT_DIR + "GoalState.json";
      filePath = resolveInputPath(filePath);
    }

    System.out.println("Processing goal state model: " + filePath);
    System.out.println("Output: " + outputPath);
    generator.parseAndExport(filePath, outputPath);
  }

  private static String resolveInputPath(String filePath) {
    File directPath = new File(filePath);
    if (directPath.exists()) {
      return filePath;
    }

    File baseDirPath = new File(INSTANCES_DIR + filePath);
    if (baseDirPath.exists()) {
      return baseDirPath.getPath();
    }

    return filePath;
  }

  public void parseAndExport(String inputPath, String outputPath) {
    try {
      File inputFile = new File(inputPath);
      if (!inputFile.exists()) {
        System.err.println("[X] File not found: " + inputPath);
        return;
      }

      CRFTypesConMill.init();

      System.out.println("[DEBUG] Parsing with CRFTypesConParser...");
      CRFTypesConParser conParser = CRFTypesConMill.parser();
      Optional<ASTWorld> parseResult = conParser.parse(inputPath);

      if (!parseResult.isPresent()) {
        System.err.println("[X] Failed to parse file: " + inputPath);
        if (Log.getErrorCount() > 0) {
          Log.getFindings().forEach(f -> System.err.println("  Error: " + f.buildMsg()));
        }
        return;
      }

      ASTWorld world = parseResult.get();
      System.out.println("[OK] Successfully parsed goal state model");

      List<PredicateInstance> instances = extractPredicateInstances(world);

      if (instances.isEmpty()) {
        System.err.println("[!] No predicate instances found in file");
      } else {
        System.out.println("[OK] Extracted " + instances.size() + " goal state predicates");
      }

      exportPredicatesToJSON(instances, outputPath);

    } catch (Exception e) {
      System.err.println("[X] ERROR: " + e.getMessage());
      e.printStackTrace();
    }
  }

  private List<PredicateInstance> extractPredicateInstances(ASTWorld world) {
    List<PredicateInstance> instances = new ArrayList<>();

    try {
      for (java.lang.reflect.Method method : world.getClass().getMethods()) {
        String mName = method.getName();

        if (mName.startsWith("get") && method.getParameterCount() == 0) {
          try {
            Object result = method.invoke(world);
            if (result instanceof java.util.Collection) {
              for (Object obj : (java.util.Collection<?>) result) {
                if (obj instanceof ASTPredicate) {
                  String type = obj.getClass().getSimpleName();
                  if (type.startsWith("AST")) {
                    type = type.substring(3);
                  }

                  PredicateInstance inst = new PredicateInstance(type);
                  extractPropertiesFromObject(obj, inst);
                  instances.add(inst);
                }
              }
            }
          } catch (Exception e) {
            // Skip methods that can't be read
          }
        }
      }
    } catch (Exception e) {
      System.err.println("[!] Warning: Could not extract predicates: " + e.getMessage());
    }

    return instances;
  }

  private static void extractPropertiesFromObject(Object obj, PredicateInstance instance) {
    if (obj == null) return;

    try {
      // Check for the 'not' keyword attribute
      try {
        java.lang.reflect.Method isNotMethod = obj.getClass().getMethod("isNot");
        if (isNotMethod.getReturnType() == boolean.class || isNotMethod.getReturnType() == Boolean.class) {
          boolean isNot = (boolean) isNotMethod.invoke(obj);
          instance.addProperty("not", isNot);
        }
      } catch (NoSuchMethodException e) {
        instance.addProperty("not", false);
      }

      java.lang.reflect.Method[] methods = obj.getClass().getMethods();

      for (java.lang.reflect.Method method : methods) {
        String methodName = method.getName();

        if (methodName.startsWith("get") &&
            methodName.length() > 3 &&
            method.getParameterCount() == 0) {

          if (methodName.equals("getClass") ||
              methodName.equals("getFullName") || methodName.equals("getPackageName") ||
              methodName.equals("getAstNode") || methodName.equals("getEnclosingScope") ||
              methodName.equals("getName")) {
            continue;
          }

          try {
            Object value = method.invoke(obj);
            String propName = methodName.substring(3);
            propName = propName.substring(0, 1).toLowerCase() + propName.substring(1);

            if (value != null) {
              if (propName.startsWith("_") ||
                  propName.startsWith("Pre") ||
                  propName.startsWith("Post") ||
                  propName.equals("stereoinfo") ||
                  propName.equals("sourcePosition") ||
                  propName.equals("locDefinition") ||
                  propName.equals("accessModifier")) {
                continue;
              }

              if (isJsonSerializable(value)) {
                if (hasNameMethod(value)) {
                  try {
                    Object nameValue = value.getClass().getMethod("getName").invoke(value);
                    if (nameValue != null) {
                      instance.addProperty(propName, nameValue.toString());
                    }
                  } catch (Exception e) {
                    // Skip
                  }
                } else {
                  instance.addProperty(propName, value);
                }
              }
            }
          } catch (Exception e) {
            // Skip
          }
        }
      }
    } catch (Exception e) {
      System.err.println("[X] Error extracting properties from object: " + e.getMessage());
    }
  }

  private static boolean isJsonSerializable(Object value) {
    if (value == null) return false;

    Class<?> clazz = value.getClass();

    if (clazz == String.class ||
        clazz == Integer.class || clazz == Long.class ||
        clazz == Float.class || clazz == Double.class ||
        clazz == Boolean.class || clazz == Byte.class) {
      return true;
    }

    if (value instanceof java.util.Collection) {
      java.util.Collection<?> col = (java.util.Collection<?>) value;
      return col.isEmpty();
    }

    if (value instanceof java.util.Map) {
      java.util.Map<?, ?> map = (java.util.Map<?, ?>) value;
      return map.isEmpty();
    }

    return false;
  }

  private static boolean hasNameMethod(Object value) {
    try {
      value.getClass().getMethod("getName");
      return true;
    } catch (NoSuchMethodException e) {
      return false;
    }
  }

  private void exportPredicatesToJSON(List<PredicateInstance> instances, String outputPath) {
    try {
      JSONObject rootJson = new JSONObject();
      JSONArray predicatesArray = new JSONArray();

      for (PredicateInstance inst : instances) {
        JSONObject predObj = new JSONObject();
        predObj.put("type", inst.type);

        if (!inst.properties.isEmpty()) {
          JSONObject propsObj = new JSONObject();
          for (Map.Entry<String, Object> entry : inst.properties.entrySet()) {
            Object val = entry.getValue();
            if (val instanceof String || val instanceof Number || val instanceof Boolean) {
              propsObj.put(entry.getKey(), val);
            } else if (val != null) {
              propsObj.put(entry.getKey(), val.toString());
            }
          }
          if (!propsObj.isEmpty()) {
            predObj.put("properties", propsObj);
          }
        }

        predicatesArray.add(predObj);
      }

      rootJson.put("predicates", predicatesArray);
      rootJson.put("count", predicatesArray.size());

      File outputFile = new File(outputPath);
      File parentDir = outputFile.getParentFile();
      if (parentDir != null && !parentDir.exists()) {
        parentDir.mkdirs();
      }

      try (FileWriter file = new FileWriter(outputPath)) {
        String jsonString = rootJson.toJSONString();
        String prettyJson = prettyPrintJson(jsonString);
        file.write(prettyJson);
        file.flush();
        System.out.println("[OK] Exported " + instances.size() + " goal state predicates to: " + outputPath);
      }

    } catch (IOException e) {
      System.err.println("[X] ERROR writing JSON file: " + e.getMessage());
      e.printStackTrace();
    }
  }

  private String prettyPrintJson(String jsonString) {
    StringBuilder prettified = new StringBuilder();
    int indentLevel = 0;
    boolean inString = false;
    char prevChar = 0;

    for (int i = 0; i < jsonString.length(); i++) {
      char c = jsonString.charAt(i);

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

  private void appendIndent(StringBuilder sb, int level) {
    for (int i = 0; i < level * 2; i++) {
      sb.append(' ');
    }
  }
}

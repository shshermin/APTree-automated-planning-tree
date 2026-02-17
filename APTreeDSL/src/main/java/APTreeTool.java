import java.io.FileWriter;
import java.io.IOException;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

import org.json.simple.JSONArray;
import org.json.simple.JSONObject;

import CoCos.CRFTypesCon.ElementExistsCoCo;
import CoCos.DynamicBTFlowNode.ActionNodesCannotHavePlanningService;
import CoCos.DynamicBTFlowNode.CausalLinkValidator;
import CoCos.DynamicBTFlowNode.MustHavePlanningService;
import CoCos.DynamicBTFlowNode.PlanningServiceActionsCoverageCoCo;
import CoCos.DynamicBTFlowNode.SharedResourceConflictCoCo;
import CoCos.DynamicBTFlowNode.UniquenessOfNames;
import behaviortree._ast.ASTActionNode;
import behaviortree._cocos.BehaviorTreeASTDecoratorCoCo;
import behaviortree._cocos.BehaviorTreeASTServiceCoCo;
import crftypescon._ast.ASTPickUpHL;
import crftypescon._ast.ASTPlaceHL;
import crftypescon._ast.ASTWorld;
import crftypescon._cocos.CRFTypesConASTPickUpHLCoCo;
import crftypescon._cocos.CRFTypesConASTPlaceHLCoCo;
import crftypescon._parser.CRFTypesConParser;
import crftypescon._visitor.CRFTypesConVisitor2;
import crftypesdef._cocos.CRFTypesDefASTPActionNodeCoCo;
import crftypesdef._symboltable.ElementSymbol;
import de.se_rwth.commons.logging.Log;
import dynamicbtflownode.DynamicBTFlowNodeMill;
import dynamicbtflownode._ast.ASTAPTree;
import dynamicbtflownode._ast.ASTDynamicBTFlowNodeNode;
import dynamicbtflownode._ast.ASTFinalWorld;
import dynamicbtflownode._ast.ASTGraphNode;
import dynamicbtflownode._cocos.DynamicBTFlowNodeASTGraphNodeCoCo;
import dynamicbtflownode._cocos.DynamicBTFlowNodeCoCoChecker;
import dynamicbtflownode._symboltable.IDynamicBTFlowNodeArtifactScope;
import dynamicbtflownode._symboltable.IDynamicBTFlowNodeGlobalScope;

public class APTreeTool {

  // Store property instances as they are loaded
  private static List<PropertyInstance> loadedInstances = new ArrayList<>();
  
  private static class PropertyInstance {
    String name;
    String type;
    String extendsType;
    java.util.Map<String, Object> properties;
    
    PropertyInstance(String name, String type, String extendsType) {
      this.name = name;
      this.type = type;
      this.extendsType = extendsType;
      this.properties = new java.util.HashMap<>();
    }
    
    void addProperty(String propName, Object propValue) {
      this.properties.put(propName, propValue);
    }
  }

  public static void main(String[] args) {
    // Standard MontiCore logging setup
    Log.init();
    // Configure MontiCore logging to not fail-fast on errors
    // This allows our CoCos to run and provide better error messages
    Log.enableFailQuick(false);
    
    APTreeTool tool = new APTreeTool();
    // If an argument is given, use it as a file; otherwise, default to valid/behavior_trees
    String filePath = args.length > 0 ? args[0] : "src/test/resources/valid/behavior_trees/APTreeLivematFinal.bt";
    tool.run(filePath);
  }

  public void run(String modelFile) {
    System.out.println("Running APTreeTool on: " + modelFile);

    // 1. Initialize the Mill
    DynamicBTFlowNodeMill.init();
    
    // 2. Load concrete instances into symbol table FIRST
    String instancesFile = "src/test/resources/valid/CRFConcrete/LiveMatSetupObjects.bt";
    loadConcreteInstancesIntoGlobalScope(instancesFile);
    
    // 2.5. Export property instances to JSON right after loading symbols
    String outputJsonPath = "target/PropertyInstances.json";
    exportPropertyInstancesToJSON(outputJsonPath);
    
    // 3. Configure Global Scope with symbol path
    DynamicBTFlowNodeMill.globalScope().setSymbolPath(
        new de.monticore.io.paths.MCPath(Paths.get("target", "symbols"))
    );

    try {
        // 4. Parse tree (FinalWorld supports multiple BehaviorTree blocks)
        ASTFinalWorld world = DynamicBTFlowNodeMill.parser().parseFinalWorld(modelFile)
             .orElseThrow(() -> new RuntimeException("Parsing failed for file: " + modelFile));
        if (world.getAPTreeList().isEmpty()) {
            throw new RuntimeException("No BehaviorTree found in file: " + modelFile);
        }
        ASTAPTree ast = world.getAPTree(0);
             
        System.out.println("[OK] SUCCESS: Syntactically parsed '" + ast.getName() + "' (" + world.getAPTreeList().size() + " tree(s))");
    
        // 4.5. PRE-VALIDATION: Check all element references BEFORE symbol table creation
        List<String> validationErrors = validateElementReferences(ast);
        if (!validationErrors.isEmpty()) {
            System.err.println("\n[!] PRE-VALIDATION WARNINGS: Found undefined element references:");
            System.err.println("Available elements: beam1, beam2, lp1, plate1, r1, fp1, fp2, fp3, rp1\n");
            for (String error : validationErrors) {
                System.err.println("  " + error);
            }
        } else {
            System.out.println("[OK] Pre-validation passed: All element references are defined");
        }
        
        // 5. Create Symbol Table
        IDynamicBTFlowNodeGlobalScope gs = DynamicBTFlowNodeMill.globalScope();
        IDynamicBTFlowNodeArtifactScope as;
        
        as = DynamicBTFlowNodeMill.scopesGenitorDelegator().createFromAST(world);
        as.setEnclosingScope(gs);
        
        // 6. Run CoCo Checks
        DynamicBTFlowNodeCoCoChecker checker = new DynamicBTFlowNodeCoCoChecker();
        // Add custom checks (must register for each node type explicitly to avoid ambiguity)
        ElementExistsCoCo elementCheck = new ElementExistsCoCo();
        checker.addCoCo((CRFTypesConASTPickUpHLCoCo) elementCheck);
        checker.addCoCo((CRFTypesConASTPlaceHLCoCo) elementCheck);
        // New: Every FlowNode must have at least one PlanningService
        MustHavePlanningService planningServiceCheck = new MustHavePlanningService();
       // checker.addCoCo(planningServiceCheck);
        // New: Action nodes cannot have PlanningService (generic, works with all ASTPActionNode subclasses)
        ActionNodesCannotHavePlanningService actionNodeCheck = new ActionNodesCannotHavePlanningService();
        checker.addCoCo((CRFTypesDefASTPActionNodeCoCo) actionNodeCheck);
        // New: Decorator and service names must be unique
        UniquenessOfNames uniquenessCheck = new UniquenessOfNames();
        checker.addCoCo((BehaviorTreeASTDecoratorCoCo) uniquenessCheck);
        checker.addCoCo((BehaviorTreeASTServiceCoCo) uniquenessCheck);
        // New: Validate causal links between connected actions
        CausalLinkValidator causalValidator = new CausalLinkValidator();
        checker.addCoCo((DynamicBTFlowNodeASTGraphNodeCoCo) causalValidator);
        
        // New: Check that all actions in behavior tree are defined in planning service domain
        PlanningServiceActionsCoverageCoCo actionsCoverageCheck = new PlanningServiceActionsCoverageCoCo();
        // checker.addCoCo(actionsCoverageCheck);
        // New: Detect shared resources between parallel action sequences
        SharedResourceConflictCoCo sharedResourceCheck = new SharedResourceConflictCoCo();
        // checker.addCoCo(sharedResourceCheck);
        
        // Register all action instances to build type mapping
        registerActionInstances(ast, causalValidator);
        
        System.out.println("[DEBUG] Running CoCo checks on AST...");
        // Add default CoCos here if any exist in the language definition
        checker.checkAll((ASTDynamicBTFlowNodeNode) ast);
        
        // Report all collected errors (pre-validation + CoCos)
        if (!validationErrors.isEmpty() || Log.getErrorCount() > 0) {
            System.err.println("\n[X] VALIDATION FAILED: Found " + (validationErrors.size() + Log.getErrorCount()) + " error(s).");
            if (!validationErrors.isEmpty()) {
                System.err.println("\nPre-validation errors:");
                for (String error : validationErrors) {
                    System.err.println("  " + error);
                }
            }
            System.err.println("\nPlease fix these issues in your behavior tree file.");
            return;
        }
        
        System.out.println("[OK] SUCCESS: Model parsed and symbols checked successfully!");

    } catch (Exception e) {
        System.err.println("tool run failed: " + e.getMessage());
        e.printStackTrace();
    }
  }

  /**
   * Export all property instances to a JSON file.
   * Uses the instances collected during the loading phase.
   * Only exports semantic, meaningful properties filtered for JSON compatibility.
   * 
   * @param outputPath Path where the JSON file should be written
   */
  public void exportPropertyInstancesToJSON(String outputPath) {
    try {
      JSONObject rootJson = new JSONObject();
      JSONArray instances = new JSONArray();
      
      // Export all collected instances
      for (PropertyInstance instance : loadedInstances) {
        JSONObject instObj = new JSONObject();
        instObj.put("name", instance.name);
        instObj.put("type", instance.type);
        instObj.put("extends", instance.extendsType);
        
        // Add properties if any
        if (!instance.properties.isEmpty()) {
          JSONObject propsObj = new JSONObject();
          for (java.util.Map.Entry<String, Object> entry : instance.properties.entrySet()) {
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
        
        instances.add(instObj);
      }
      
      rootJson.put("instances", instances);
      rootJson.put("count", instances.size());
      
      // Write to file with pretty-printing
      try (FileWriter file = new FileWriter(outputPath)) {
        // Pretty-print the JSON for readability
        String jsonString = rootJson.toJSONString();
        // Simple pretty-printing with indentation
        String prettyJson = prettyPrintJson(jsonString);
        file.write(prettyJson);
        file.flush();
        System.out.println("[OK] Exported " + instances.size() + " property instances to: " + outputPath);
      }
      
    } catch (IOException e) {
      System.err.println("[X] ERROR writing instances JSON file: " + e.getMessage());
      e.printStackTrace();
    }
  }

  /**
   * Pretty-print JSON for readability.
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
   * Append indentation spaces.
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
   * Load concrete instances (beam1, beam2, robot1, etc.) into the global scope
   * so they are available when the tree is parsed.
   * 
   * @param instancesFile Path to the concrete instances model file (e.g., CRFConcreteInstances.bt)
   */
  private void loadConcreteInstancesIntoGlobalScope(String instancesFile) {
    System.out.println("Loading concrete instances from: " + instancesFile);
    
    try {
      // Initialize CRFTypesCon mill (separate from DynamicBTFlowNode)
      crftypescon.CRFTypesConMill.init();
      
      // Parse the concrete instances model
      CRFTypesConParser parser = crftypescon.CRFTypesConMill.parser();
      Optional<ASTWorld> result = parser.parse(instancesFile);
      
      if (result.isEmpty()) {
        System.err.println("[!] Failed to parse instances file: " + instancesFile);
        return;
      }
      
      ASTWorld world = result.get();
      System.out.println("[OK] Parsed instances: " + instancesFile);
      
      // Create symbol table from instances AST
      var instanceScope = crftypescon.CRFTypesConMill.scopesGenitorDelegator().createFromAST(world);
      
      // Bridge: add the instance scope into the DynamicBTFlowNode global scope
      // so that resolveElement() etc. can find Beam, Plate, Robot symbols
      DynamicBTFlowNodeMill.globalScope().addSubScope(instanceScope);
      
      // Generic loading of all symbol types from the scope via Reflection
      // Captures ALL symbol types: Elements, Tools, Agents, Locations, and any user-defined types
      // Iterates over all "getLocal...Symbols" accessors to discover all symbol collections dynamically
      int count = 0;
      java.util.Set<String> loadedSymbolNames = new java.util.HashSet<>();
      
      try {
          // Iterate over all methods in the scope to find "getLocal...Symbols" accessors
          for (java.lang.reflect.Method method : instanceScope.getClass().getMethods()) {
              String mName = method.getName();
              
              // Match methods like "getLocalBeamSymbols", "getLocalRobotSymbols", "getLocalLocationSymbols", etc.
              // Skip the generic "getLocalSymbols" to avoid duplicates
              if (mName.startsWith("getLocal") && mName.endsWith("Symbols") && !mName.equals("getLocalSymbols") && method.getParameterCount() == 0) {
                  
                  try {
                      // Invoke getting collection of symbols
                      Object methodResult = method.invoke(instanceScope);
                      if (methodResult instanceof java.util.Collection) {
                          for (Object obj : (java.util.Collection<?>) methodResult) {
                              
                              // Capture ANY symbol object from the scope
                              // This includes framework base types and all user-defined types
                              if (obj != null) {
                                  try {
                                      // Get the name via reflection (all symbols should have getName())
                                      java.lang.reflect.Method getNameMethod = obj.getClass().getMethod("getName");
                                      String symName = (String) getNameMethod.invoke(obj);
                                      
                                      // Avoid duplicates (e.g. if appear in multiple getters)
                                      if (loadedSymbolNames.contains(symName)) {
                                          continue;
                                      }
                                      
                                      loadedSymbolNames.add(symName);
                                      
                                      // Determine type from class name (e.g. BeamSymbol -> Beam, RobotSymbol -> Robot)
                                      String symType = obj.getClass().getSimpleName();
                                      if (symType.endsWith("Symbol")) {
                                          symType = symType.substring(0, symType.length() - 6);
                                      }
                                      
                                      // Determine super type from parent class
                                      // (e.g. BeamSymbol extends ElementSymbol -> "Element")
                                      String superType = "Unknown";
                                      Class<?> superClass = obj.getClass().getSuperclass();
                                      if (superClass != null) {
                                          String superName = superClass.getSimpleName();
                                          if (superName.endsWith("Symbol") && !superName.equals("Symbol")) {
                                               superType = superName.substring(0, superName.length() - 6);
                                          }
                                      }
                                      
                                      System.out.println("  - Adding " + symType + ": " + symName + " (extends " + superType + ")");
                                      
                                      PropertyInstance inst = new PropertyInstance(symName, symType, superType);
                                      extractSymbolProperties(obj, inst);
                                      loadedInstances.add(inst);
                                      count++;
                                  } catch (Exception symbolEx) {
                                      // Skip symbols that don't have getName or can't be processed
                                      // This can happen for incomplete symbol definitions
                                  }
                              }
                          }
                      }
                  } catch (Exception innerEx) {
                      // Silently skip inaccessible symbol collections
                  }
              }
          }
      } catch (Exception e) {
          System.err.println("[X] CRITICAL: Failed to reflectively load symbols: " + e.getMessage());
          e.printStackTrace();
      }
      
      System.out.println("[OK] Loaded " + count + " element instances into global scope!");
      
    } catch (Exception e) {
      System.err.println("[X] ERROR loading instances: " + e.getMessage());
      e.printStackTrace();
    }
  }

  /**
   * Dynamically extract meaningful properties from a symbol using reflection.
   * Filters out internal MontiCore framework properties and only serializes simple types.
   * Also extracts semantic properties from the underlying AST node.
   * 
   * @param symbol The symbol object to extract properties from
   * @param instance The PropertyInstance to store the extracted properties
   */
  private static void extractSymbolProperties(Object symbol, PropertyInstance instance) {
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
   * @param instance The PropertyInstance to store the extracted properties
   */
  private static void extractPropertiesFromObject(Object obj, PropertyInstance instance) {
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

  /**
   * Register all action instances in the behavior tree with the CausalLinkValidator.
   * This traverses the AST to extract action instance names and their types.
   * 
   * @param ast The parsed behavior tree AST
   * @param validator The CausalLinkValidator to register instances with
   */
  private void registerActionInstances(ASTAPTree ast, CausalLinkValidator validator) {
    var traverser = DynamicBTFlowNodeMill.traverser();
    
    traverser.add4DynamicBTFlowNode(new dynamicbtflownode._visitor.DynamicBTFlowNodeVisitor2() {
      @Override
      public void visit(ASTGraphNode node) {
        if (node.getNode() instanceof ASTActionNode) {
          ASTActionNode actionNode = (ASTActionNode) node.getNode();
          String instanceName = actionNode.getName();
          // Extract action type from class name (ASTPickUpHL -> PickUpHL)
          String actionType = extractActionType(actionNode);
          validator.registerActionInstance(instanceName, actionType);
          System.out.println("[DEBUG] Registered action: " + instanceName + " (type: " + actionType + ")");
        }
      }
    });
    
    ast.accept(traverser);
  }

  /**
   * Extract action type name from an ASTActionNode.
   * Maps ASTPic kUpHL class name to PickUpHL type name.
   * 
   * @param actionNode The action node
   * @return The action type name (e.g., "PickUpHL")
   */
  private String extractActionType(ASTActionNode actionNode) {
    String className = actionNode.getClass().getSimpleName();
    // ASTPickUpHL -> PickUpHL, ASTPlaceHL -> PlaceHL
    if (className.startsWith("AST")) {
      return className.substring(3); // Remove "AST" prefix
    }
    return className;
  }

  /**
   * Pre-validate all element references in the behavior tree before symbol table creation.
   * This catches undefined references early with clear error messages.
   * 
   * @param ast The parsed behavior tree AST
   * @return List of error messages (empty if all references are valid)
   */
  private List<String> validateElementReferences(ASTAPTree ast) {
    List<String> errors = new ArrayList<>();
    IDynamicBTFlowNodeGlobalScope globalScope = DynamicBTFlowNodeMill.globalScope();
    
    // Create a traverser for the full language hierarchy
    var traverser = DynamicBTFlowNodeMill.traverser();
    
    traverser.add4CRFTypesCon(new CRFTypesConVisitor2() {
      @Override
      public void visit(ASTPickUpHL node) {
        String elementName = node.getObj();
        int line = node.get_SourcePositionStart().getLine();
        
        // Try to resolve the element in the global scope
        Optional<ElementSymbol> symbol = globalScope.resolveElement(elementName);
        
        if (!symbol.isPresent()) {
          errors.add("Line " + line + ": PickUpHL references undefined element '" + elementName + "'");
        }
      }
      
      @Override
      public void visit(ASTPlaceHL node) {
        String elementName = node.getObj();
        int line = node.get_SourcePositionStart().getLine();
        
        // Try to resolve the element in the global scope
        Optional<ElementSymbol> symbol = globalScope.resolveElement(elementName);
        
        if (!symbol.isPresent()) {
          errors.add("Line " + line + ": PlaceHL references undefined element '" + elementName + "'");
        }
      }
    });
    
    // Traverse the AST to collect all validation errors
    ast.accept(traverser);
    
    return errors;
  }
}

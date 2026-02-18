import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;

import org.json.simple.JSONArray;
import org.json.simple.JSONObject;

import behaviortree._ast.ASTActionNode;
import behaviortree._ast.ASTBTNode;
import behaviortree._ast.ASTDecorator;
import behaviortree._ast.ASTFlowNode;
import behaviortree._ast.ASTService;
import crftypescon._ast.ASTWorld;
import crftypescon._parser.CRFTypesConParser;
import de.se_rwth.commons.logging.Log;
import dynamicbtflownode.DynamicBTFlowNodeMill;
import dynamicbtflownode._ast.ASTAPTree;
import dynamicbtflownode._ast.ASTDynamicFlowNode;
import dynamicbtflownode._ast.ASTFinalWorld;
import dynamicbtflownode._ast.ASTGraphNode;
import dynamicbtflownode._ast.ASTNodeGraph;
import dynamicbtflownode._ast.ASTRelation;
import planningservice._ast.ASTServicePDDLPlanner;

/**
 * BTInstanceJsonGenerator - Exports the full BT structure as JSON.
 *
 * Parses the APTree .bt model (DynamicBTFlowNode grammar) and serializes
 * the complete behavior tree topology (flow nodes, action nodes, services,
 * decorators, node graphs, and temporal relations) into a single JSON file
 * that the C# APTreeExecutionEngine can load at runtime.
 *
 * Usage:
 *   java BTInstanceJsonGenerator [btModelPath] [instancesPath] [outputPath]
 *
 * Defaults:
 *   btModelPath  = src/test/resources/valid/behavior_trees/APTreeLivematFinal.bt
 *   instancesPath = src/test/resources/valid/CRFConcrete/LiveMatSetupObjects.bt
 *   outputPath   = ../APTreeExecutionEngine/src/ModelLoader/BehaviorTreeModel.json
 */
public class BTInstanceJsonGenerator {

  private static final String DEFAULT_BT_MODEL = "src/test/resources/valid/behavior_trees/APTreeLiveMat.bt";
  private static final String DEFAULT_INSTANCES = "src/test/resources/valid/CRFConcrete/LiveMatSetupObjects.bt";
  private static final String DEFAULT_OUTPUT = "../APTreeExecutionEngine/src/ModelLoader/BehaviorTreeModel.json";

  // ──────────────────────────────────────────────────────────────────────────
  // Entry point
  // ──────────────────────────────────────────────────────────────────────────

  public static void main(String[] args) {
    Log.init();
    Log.enableFailQuick(false);

    String btModelPath = args.length > 0 ? args[0] : DEFAULT_BT_MODEL;
    String instancesPath = args.length > 1 ? args[1] : DEFAULT_INSTANCES;
    String outputPath = args.length > 2 ? args[2] : DEFAULT_OUTPUT;

    BTInstanceJsonGenerator generator = new BTInstanceJsonGenerator();
    generator.generateBTJson(btModelPath, instancesPath, outputPath);
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Public API
  // ──────────────────────────────────────────────────────────────────────────

  /**
   * Parse the BT model and export its structure to JSON.
   *
   * @param btModelPath   Path to the APTree .bt file
   * @param instancesPath Path to the concrete instances file (LiveMatSetupObjects.bt)
   * @param outputPath    Where the JSON should be written
   */
  public void generateBTJson(String btModelPath, String instancesPath, String outputPath) {
    try {
      // Validate input files
      if (!new File(btModelPath).exists()) {
        System.err.println("[X] BT model file not found: " + btModelPath);
        return;
      }
      if (!new File(instancesPath).exists()) {
        System.err.println("[X] Instances file not found: " + instancesPath);
        return;
      }

      // 1. Initialize the Mill
      DynamicBTFlowNodeMill.init();

      // 2. Load concrete instances into global scope (needed for symbol resolution)
      loadConcreteInstances(instancesPath);

      // 3. Configure symbol path
      DynamicBTFlowNodeMill.globalScope().setSymbolPath(
          new de.monticore.io.paths.MCPath(Paths.get("target", "symbols"))
      );

      // 4. Parse the BT model
      System.out.println("[DEBUG] Parsing BT model: " + btModelPath);
      ASTFinalWorld finalWorld = DynamicBTFlowNodeMill.parser().parseFinalWorld(btModelPath)
          .orElseThrow(() -> new RuntimeException("Parsing failed for: " + btModelPath));

      if (finalWorld.getAPTreeList().isEmpty()) {
        System.err.println("[X] No BehaviorTree found in: " + btModelPath);
        return;
      }

      System.out.println("[OK] Parsed " + finalWorld.getAPTreeList().size() + " behavior tree(s)");

      // 5. Create symbol table (needed for name resolution in relations)
      DynamicBTFlowNodeMill.scopesGenitorDelegator().createFromAST(finalWorld);

      // 6. Export all trees
      JSONObject root = new JSONObject();
      JSONArray treesArray = new JSONArray();

      for (ASTAPTree tree : finalWorld.getAPTreeList()) {
        JSONObject treeJson = exportTree(tree);
        treesArray.add(treeJson);
      }

      root.put("behaviorTrees", treesArray);
      root.put("treeCount", treesArray.size());
      root.put("generatedAt", java.time.LocalDateTime.now().toString());
      root.put("sourceFile", btModelPath);

      // 7. Write JSON
      writeJsonFile(root, outputPath);
      System.out.println("[OK] Exported BT model to: " + outputPath);

    } catch (Exception e) {
      System.err.println("[X] ERROR: " + e.getMessage());
      e.printStackTrace();
    }
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Tree export
  // ──────────────────────────────────────────────────────────────────────────

  /**
   * Export a single BehaviorTree (ASTAPTree) to JSON.
   */
  private JSONObject exportTree(ASTAPTree tree) {
    JSONObject treeJson = new JSONObject();
    treeJson.put("name", tree.getName());

    // Export the root flow node
    ASTFlowNode rootFlow = tree.getRoot();
    if (rootFlow != null) {
      treeJson.put("root", exportFlowNode(rootFlow));
    }

    return treeJson;
  }

  /**
   * Export a FlowNode (Sequence, Parallel, or DynamicFlowNode) to JSON.
   */
  private JSONObject exportFlowNode(ASTFlowNode flow) {
    JSONObject flowJson = new JSONObject();

    // Basic info
    flowJson.put("name", flow.getName());
    flowJson.put("type", getFlowNodeType(flow));

    // Services
    JSONArray servicesArray = exportServices(flow.getServiceList());
    if (!servicesArray.isEmpty()) {
      flowJson.put("services", servicesArray);
    }

    // Decorators
    JSONArray decoratorsArray = exportDecorators(flow.getDecoratorList());
    if (!decoratorsArray.isEmpty()) {
      flowJson.put("decorators", decoratorsArray);
    }

    // DynamicFlowNode-specific fields
    if (flow instanceof ASTDynamicFlowNode) {
      ASTDynamicFlowNode dynFlow = (ASTDynamicFlowNode) flow;

      // Success criteria (All, Any, Count, Percentage, Signal)
      if (dynFlow.getSuccri() != null) {
        flowJson.put("successCriteria", dynFlow.getSuccri().toString());
      }

      // Child type (AllAction, AllFlow)
      if (dynFlow.getChildType() != null) {
        flowJson.put("childType", dynFlow.getChildType().toString());
      }

      // NodeGraph
      ASTNodeGraph nodeGraph = dynFlow.getNodeGraph();
      if (nodeGraph != null) {
        flowJson.put("nodeGraph", exportNodeGraph(nodeGraph));
      }
    }

    // Child BTNodes (for Sequence/Parallel composite nodes)
    List<ASTBTNode> children = flow.getChildrenList();
    if (children != null && !children.isEmpty()) {
      JSONArray childrenArray = new JSONArray();
      for (ASTBTNode child : children) {
        if (child instanceof ASTFlowNode) {
          childrenArray.add(exportFlowNode((ASTFlowNode) child));
        } else if (child instanceof ASTActionNode) {
          childrenArray.add(exportActionNode((ASTActionNode) child));
        }
      }
      if (!childrenArray.isEmpty()) {
        flowJson.put("children", childrenArray);
      }
    }

    return flowJson;
  }

  /**
   * Export a NodeGraph to JSON.
   * The node graph contains action/flow nodes and their temporal relations.
   */
  private JSONObject exportNodeGraph(ASTNodeGraph graph) {
    JSONObject graphJson = new JSONObject();

    // First pass: collect all nodes and build name→index map
    JSONArray nodesArray = new JSONArray();
    Map<String, Integer> nameToIndex = new LinkedHashMap<>();

    List<ASTGraphNode> graphNodes = graph.getNodesList();
    for (int i = 0; i < graphNodes.size(); i++) {
      ASTGraphNode gn = graphNodes.get(i);
      ASTBTNode btNode = gn.getNode();

      JSONObject nodeJson;
      if (btNode instanceof ASTFlowNode) {
        nodeJson = exportFlowNode((ASTFlowNode) btNode);
      } else if (btNode instanceof ASTActionNode) {
        nodeJson = exportActionNode((ASTActionNode) btNode);
      } else {
        nodeJson = new JSONObject();
        nodeJson.put("name", btNode.getName());
        nodeJson.put("type", btNode.getClass().getSimpleName().replaceFirst("^AST", ""));
      }

      nodesArray.add(nodeJson);
      nameToIndex.put(btNode.getName(), i);
    }

    graphJson.put("nodes", nodesArray);

    // Second pass: export temporal relations
    JSONArray relationsArray = new JSONArray();
    for (ASTGraphNode gn : graphNodes) {
      ASTBTNode source = gn.getNode();
      String sourceName = source.getName();

      for (ASTRelation rel : gn.getSuccessorsList()) {
        JSONObject relJson = new JSONObject();
        relJson.put("from", sourceName);
        relJson.put("to", rel.getTarget());

        // Temporal type (Meets, Precedes, Overlaps, etc.)
        if (rel.getTemptype() != null) {
          relJson.put("temporalType", rel.getTemptype().toString());
        }

        relationsArray.add(relJson);
      }
    }

    if (!relationsArray.isEmpty()) {
      graphJson.put("relations", relationsArray);
    }

    graphJson.put("nodeCount", nodesArray.size());
    graphJson.put("relationCount", relationsArray.size());

    return graphJson;
  }

  /**
   * Export an ActionNode to JSON, including its parameters.
   */
  private JSONObject exportActionNode(ASTActionNode action) {
    JSONObject actionJson = new JSONObject();

    actionJson.put("name", action.getName());

    // Action type (e.g., PickUpHL, PlaceHL, StackHL, etc.)
    String actionType = action.getClass().getSimpleName();
    if (actionType.startsWith("AST")) {
      actionType = actionType.substring(3);
    }
    actionJson.put("actionType", actionType);

    // Extract parameters via reflection (same approach as APTreeJsonCli)
    JSONObject paramsJson = extractActionParameters(action);
    if (!paramsJson.isEmpty()) {
      actionJson.put("parameters", paramsJson);
    }

    // Subtree annotation (@SubtreeName)
    String subtreeAnnotation = tryGetSubtreeAnnotation(action);
    if (subtreeAnnotation != null) {
      actionJson.put("subtreeAnnotation", subtreeAnnotation);
    }

    // Action level (HighLevel, MidLevel, LowLevel)
    String actLevel = tryGetActionLevel(action);
    if (actLevel != null) {
      actionJson.put("actionLevel", actLevel);
    }

    // Services on action node
    JSONArray servicesArray = exportServices(action.getServiceList());
    if (!servicesArray.isEmpty()) {
      actionJson.put("services", servicesArray);
    }

    // Decorators on action node
    JSONArray decoratorsArray = exportDecorators(action.getDecoratorList());
    if (!decoratorsArray.isEmpty()) {
      actionJson.put("decorators", decoratorsArray);
    }

    return actionJson;
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Services & Decorators
  // ──────────────────────────────────────────────────────────────────────────

  private JSONArray exportServices(List<? extends ASTService> services) {
    JSONArray arr = new JSONArray();
    if (services == null) return arr;

    for (ASTService service : services) {
      JSONObject sJson = new JSONObject();
      sJson.put("name", service.getName());

      // Determine service type
      String serviceType = service.getClass().getSimpleName();
      if (serviceType.startsWith("AST")) {
        serviceType = serviceType.substring(3);
      }
      sJson.put("type", serviceType);

      // For ServicePDDLPlanner, extract planner reference
      if (service instanceof ASTServicePDDLPlanner) {
        ASTServicePDDLPlanner pddlService = (ASTServicePDDLPlanner) service;
        sJson.put("plannerRef", pddlService.getPlanner());
      }

      arr.add(sJson);
    }
    return arr;
  }

  private JSONArray exportDecorators(List<? extends ASTDecorator> decorators) {
    JSONArray arr = new JSONArray();
    if (decorators == null) return arr;

    for (ASTDecorator decorator : decorators) {
      JSONObject dJson = new JSONObject();
      dJson.put("name", decorator.getName());

      String decoratorType = decorator.getClass().getSimpleName();
      if (decoratorType.startsWith("AST")) {
        decoratorType = decoratorType.substring(3);
      }
      dJson.put("type", decoratorType);

      arr.add(dJson);
    }
    return arr;
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Parameter extraction (reflection-based, compatible with all action types)
  // ──────────────────────────────────────────────────────────────────────────

  /**
   * Extract action parameters via reflection.
   * Uses the same getter-based approach as APTreeJsonCli, but returns a JSONObject
   * mapping parameter names to values (typically instance names like "b1", "r1", "fp1").
   */
  private JSONObject extractActionParameters(ASTActionNode action) {
    JSONObject params = new JSONObject();

    try {
      // Known parameter getters per action type (from CRFTypesCon.mc4 grammar)
      Map<String, String[]> typeGetters = new LinkedHashMap<>();
      typeGetters.put("ASTPickUpHL", new String[]{"getObj", "getGrabPos", "getClient"});
      typeGetters.put("ASTPlaceHL", new String[]{"getObj", "getPlacePos", "getClient"});
      typeGetters.put("ASTStackHL", new String[]{"getObj1", "getObj2", "getClient", "getPr", "getLay", "getMod"});
      typeGetters.put("ASTStackOnMultipleHL", new String[]{"getPlate", "getClient", "getPos", "getMod", "getLay"});
      typeGetters.put("ASTGluingPlateHL", new String[]{"getObj", "getPos", "getClient"});
      typeGetters.put("ASTGluingBeamHL", new String[]{"getObj", "getPos", "getClient", "getMod", "getLay"});
      typeGetters.put("ASTNailingHL", new String[]{"getObj", "getPos", "getClient"});
      typeGetters.put("ASTTravelML", new String[]{"getClient", "getFrom", "getTo"});
      typeGetters.put("ASTEquipeML", new String[]{"getClient", "getToo", "getEp"});
      typeGetters.put("ASTDeequipML", new String[]{"getClient", "getToo", "getEp"});
      typeGetters.put("ASTInitializeML", new String[]{"getClient", "getToo"});
      typeGetters.put("ASTCloseToolML", new String[]{"getClient", "getToo"});
      typeGetters.put("ASTPickUpML", new String[]{"getObj", "getPos", "getClient", "getVg"});
      typeGetters.put("ASTPlaceML", new String[]{"getObj", "getPlacepos", "getClient", "getVg"});
      typeGetters.put("ASTGluingML", new String[]{"getObj", "getPos", "getClient", "getGg"});
      typeGetters.put("ASTNailingML", new String[]{"getObj", "getPos", "getClient", "getNg"});
      typeGetters.put("ASTStackML", new String[]{"getObj1", "getObj2", "getClient", "getVg", "getPr"});
      typeGetters.put("ASTStackOnMultipleML", new String[]{"getPlate", "getClient", "getPos", "getVg", "getMod", "getLay"});

      String className = action.getClass().getSimpleName();
      String[] getters = typeGetters.get(className);

      if (getters != null) {
        // Use known parameter set
        for (String getter : getters) {
          try {
            java.lang.reflect.Method method = action.getClass().getMethod(getter);
            Object value = method.invoke(action);
            if (value != null) {
              // Parameter name = getter minus "get", lower-cased first letter
              String paramName = getter.substring(3);
              paramName = paramName.substring(0, 1).toLowerCase() + paramName.substring(1);
              params.put(paramName, value.toString());
            }
          } catch (NoSuchMethodException e) {
            // Getter not available for this type — skip
          }
        }
      } else {
        // Fallback: generic reflection for unknown action types
        extractParametersGeneric(action, params);
      }

    } catch (Exception e) {
      System.err.println("[!] Warning: Could not extract parameters for " + action.getName() + ": " + e.getMessage());
    }

    return params;
  }

  /**
   * Generic parameter extraction fallback for action types not explicitly listed.
   * Scans for getter methods returning simple String values (typical for Name@ references).
   */
  private void extractParametersGeneric(ASTActionNode action, JSONObject params) {
    // Skip these framework getters
    java.util.Set<String> skipGetters = new java.util.HashSet<>();
    skipGetters.add("getName");
    skipGetters.add("getClass");
    skipGetters.add("getActLevel");
    skipGetters.add("getSubtreeAnnotation");

    for (java.lang.reflect.Method method : action.getClass().getMethods()) {
      String mName = method.getName();
      if (mName.startsWith("get") && mName.length() > 3
          && method.getParameterCount() == 0
          && method.getReturnType() == String.class
          && !skipGetters.contains(mName)) {
        try {
          Object value = method.invoke(action);
          if (value != null) {
            String paramName = mName.substring(3);
            paramName = paramName.substring(0, 1).toLowerCase() + paramName.substring(1);
            params.put(paramName, value.toString());
          }
        } catch (Exception e) {
          // skip
        }
      }
    }
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Helpers
  // ──────────────────────────────────────────────────────────────────────────

  /**
   * Determine the flow node type name.
   */
  private String getFlowNodeType(ASTFlowNode flow) {
    if (flow instanceof ASTDynamicFlowNode) return "DynamicFlowNode";

    String className = flow.getClass().getSimpleName();
    if (className.startsWith("AST")) {
      return className.substring(3);  // ASTSequence → Sequence, ASTParallel → Parallel
    }
    return className;
  }

  /**
   * Try to get the subtree annotation (@SubtreeName) from a PActionNode.
   * MontiCore optional attributes require checking isPresentXxx() before calling getXxx().
   */
  private String tryGetSubtreeAnnotation(ASTActionNode action) {
    try {
      // Check if the optional attribute is present first
      java.lang.reflect.Method isPresent = action.getClass().getMethod("isPresentSubtreeAnnotation");
      Boolean present = (Boolean) isPresent.invoke(action);
      if (present == null || !present) {
        return null;
      }
      java.lang.reflect.Method m = action.getClass().getMethod("getSubtreeAnnotation");
      Object value = m.invoke(action);
      return value != null ? value.toString() : null;
    } catch (Exception e) {
      return null;
    }
  }

  /**
   * Try to get the action level (HighLevel, MidLevel, LowLevel) from a PActionNode.
   */
  private String tryGetActionLevel(ASTActionNode action) {
    try {
      java.lang.reflect.Method m = action.getClass().getMethod("getActLevel");
      Object value = m.invoke(action);
      if (value == null) return null;
      if (value instanceof Enum) return ((Enum<?>) value).name();
      return value.toString();
    } catch (Exception e) {
      return null;
    }
  }

  // ──────────────────────────────────────────────────────────────────────────
  // Concrete instance loading (for symbol resolution)
  // ──────────────────────────────────────────────────────────────────────────

  private void loadConcreteInstances(String instancesFile) {
    System.out.println("[DEBUG] Loading concrete instances from: " + instancesFile);
    try {
      crftypescon.CRFTypesConMill.init();
      CRFTypesConParser parser = crftypescon.CRFTypesConMill.parser();
      Optional<ASTWorld> result = parser.parse(instancesFile);

      if (result.isEmpty()) {
        System.err.println("[!] Failed to parse instances file: " + instancesFile);
        return;
      }

      ASTWorld world = result.get();
      var instanceScope = crftypescon.CRFTypesConMill.scopesGenitorDelegator().createFromAST(world);
      DynamicBTFlowNodeMill.globalScope().addSubScope(instanceScope);

      System.out.println("[OK] Loaded concrete instances into global scope");
    } catch (Exception e) {
      System.err.println("[X] ERROR loading instances: " + e.getMessage());
      e.printStackTrace();
    }
  }

  // ──────────────────────────────────────────────────────────────────────────
  // JSON writing utilities
  // ──────────────────────────────────────────────────────────────────────────

  private void writeJsonFile(JSONObject json, String outputPath) {
    try {
      // Ensure parent directory exists
      File outputFile = new File(outputPath);
      File parentDir = outputFile.getParentFile();
      if (parentDir != null && !parentDir.exists()) {
        parentDir.mkdirs();
      }

      try (FileWriter file = new FileWriter(outputPath)) {
        String prettyJson = prettyPrintJson(json.toJSONString());
        file.write(prettyJson);
        file.flush();
      }
    } catch (IOException e) {
      System.err.println("[X] ERROR writing JSON: " + e.getMessage());
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

import java.io.File;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

import CoCos.CRFTypesCon.ElementExistsCoCo;
import crftypescon.CRFTypesConMill;
import crftypescon._ast.ASTPickUpHL;
import crftypescon._ast.ASTPlaceHL;
import crftypescon._ast.ASTWorld;
import crftypescon._parser.CRFTypesConParser;
import crftypescon._visitor.CRFTypesConVisitor2;
import crftypesdef._symboltable.ElementSymbol;
import de.se_rwth.commons.logging.Log;
import dynamicbtflownode.DynamicBTFlowNodeMill;
import dynamicbtflownode._ast.ASTAPTree;
import dynamicbtflownode._ast.ASTDynamicBTFlowNodeNode;
import dynamicbtflownode._cocos.DynamicBTFlowNodeCoCoChecker;
import dynamicbtflownode._symboltable.IDynamicBTFlowNodeArtifactScope;
import dynamicbtflownode._symboltable.IDynamicBTFlowNodeGlobalScope;

/**
 * JSON-only CLI wrapper around the APTree parsing + validation pipeline.
 *
 * Contract:
 * - Writes exactly one JSON object to stdout.
 * - Writes diagnostics/debug info to stderr (optional) but avoids stdout noise.
 */
public class APTreeJsonCli {

  private static final String DEFAULT_INSTANCES_FILE =
      "src/test/resources/valid/CRFConcrete/CRFConcreteInstances.bt";

  public static void main(String[] args) {
    Log.init();
    Log.enableFailQuick(false);

    String modelFile = null;
    String instancesFile = DEFAULT_INSTANCES_FILE;

    for (int i = 0; i < args.length; i++) {
      String arg = args[i];
      if ("--model".equals(arg) && i + 1 < args.length) {
        modelFile = args[++i];
      } else if ("--instances".equals(arg) && i + 1 < args.length) {
        instancesFile = args[++i];
      } else if (!arg.startsWith("--") && modelFile == null) {
        // Backwards-compatible: first positional argument is the model file
        modelFile = arg;
      }
    }

    if (modelFile == null || modelFile.isBlank()) {
      System.out.print(errorJson("Missing required argument: --model <file> (or positional <file>)"));
      return;
    }

    // Keep stdout JSON-only: MontiCore's Log may write to System.out during parsing/validation.
    // Route any such console output to stderr and restore stdout only for the final JSON print.
    java.io.PrintStream jsonOut = System.out;
    try {
      System.setOut(System.err);
      APTreeJsonCli cli = new APTreeJsonCli();
      String json = cli.run(modelFile, instancesFile);
      System.setOut(jsonOut);
      jsonOut.print(json);
    } finally {
      System.setOut(jsonOut);
    }
  }

  public String run(String modelFile, String instancesFile) {
    try {
      File model = new File(modelFile);
      if (!model.exists()) {
        return errorJson("Model file not found: " + modelFile + " (cwd=" + System.getProperty("user.dir") + ")");
      }

      DynamicBTFlowNodeMill.init();

      // Load concrete instances into the global scope first
      loadConcreteInstancesIntoGlobalScope(instancesFile);

      // Instance parsing may add global Log errors/findings. Some MontiCore parsers return Optional.empty
      // when the global Log already contains errors (even if the model itself is valid).
      // Reset the Log state so parsing depends on the model file, not on instance-file noise.
      resetLogStateAfterInstanceLoad();

      // Configure symbol path (for persisted symbols if used)
      DynamicBTFlowNodeMill.globalScope().setSymbolPath(
          new de.monticore.io.paths.MCPath(Paths.get("target", "symbols"))
      );

      Optional<ASTAPTree> parsed = DynamicBTFlowNodeMill.parser().parseAPTree(modelFile);
      if (parsed.isEmpty()) {
        return findingsJson(false, null, "Parsing failed", collectFindings());
      }

      ASTAPTree ast = parsed.get();

      GraphExport graph = GraphExport.fromTree(ast);

      // Pre-validation: element references must resolve
      List<String> preValidationErrors = validateElementReferences(ast);
      if (!preValidationErrors.isEmpty()) {
        return json(false, ast.getName(), preValidationErrors, collectFindings(), graph);
      }

      // Symbol table creation
      IDynamicBTFlowNodeGlobalScope gs = DynamicBTFlowNodeMill.globalScope();
      IDynamicBTFlowNodeArtifactScope as = DynamicBTFlowNodeMill.scopesGenitorDelegator().createFromAST(ast);
      as.setEnclosingScope(gs);

      // CoCos
      DynamicBTFlowNodeCoCoChecker checker = new DynamicBTFlowNodeCoCoChecker();
      ElementExistsCoCo elementCheck = new ElementExistsCoCo();
      checker.addCoCo((crftypescon._cocos.CRFTypesConASTPickUpHLCoCo) elementCheck);
      checker.addCoCo((crftypescon._cocos.CRFTypesConASTPlaceHLCoCo) elementCheck);
      checker.checkAll((ASTDynamicBTFlowNodeNode) ast);

      List<String> findings = collectFindings();
      boolean ok = findings.isEmpty();
      return json(ok, ast.getName(), new ArrayList<>(), findings, graph);

    } catch (Exception e) {
      return errorJson("Exception: " + safe(e.getMessage()));
    }
  }

  private void loadConcreteInstancesIntoGlobalScope(String instancesFile) {
    try {
      CRFTypesConMill.init();

      CRFTypesConParser parser = new CRFTypesConParser();
      Optional<ASTWorld> result = parser.parse(instancesFile);

      if (result.isEmpty()) {
        // Keep going; findings will show parse issues
        return;
      }

      var instanceScope = CRFTypesConMill.scopesGenitorDelegator().createFromAST(result.get());

      for (var beamSymbol : instanceScope.getLocalBeamSymbols()) {
        DynamicBTFlowNodeMill.globalScope().add(beamSymbol);
      }
      for (var plateSymbol : instanceScope.getLocalPlateSymbols()) {
        DynamicBTFlowNodeMill.globalScope().add(plateSymbol);
      }
      for (var robotSymbol : instanceScope.getLocalRobotSymbols()) {
        DynamicBTFlowNodeMill.globalScope().add(robotSymbol);
      }
      for (var fpSymbol : instanceScope.getLocalFirstPosSymbols()) {
        DynamicBTFlowNodeMill.globalScope().add(fpSymbol);
      }

    } catch (Exception ignored) {
      // Surface via generic error finding if present; otherwise keep CLI stable
    }
  }

  private static void resetLogStateAfterInstanceLoad() {
    // Best-effort: different Log versions expose different reset/clear APIs.
    try { Log.getFindings().clear(); } catch (Exception ignored) { }

    try {
      java.lang.reflect.Method m = Log.class.getMethod("clearFindings");
      m.invoke(null);
    } catch (Exception ignored) { }

    try {
      java.lang.reflect.Method m = Log.class.getMethod("clear");
      m.invoke(null);
    } catch (Exception ignored) { }

    // Fallback: re-init logging to reset global counters.
    try {
      Log.init();
      Log.enableFailQuick(false);
    } catch (Exception ignored) {
      // keep CLI stable
    }
  }

  private List<String> validateElementReferences(ASTAPTree ast) {
    List<String> errors = new ArrayList<>();
    IDynamicBTFlowNodeGlobalScope globalScope = DynamicBTFlowNodeMill.globalScope();

    var traverser = DynamicBTFlowNodeMill.traverser();

    traverser.add4CRFTypesCon(new CRFTypesConVisitor2() {
      @Override
      public void visit(ASTPickUpHL node) {
        String elementName = node.getObj();
        int line = node.get_SourcePositionStart().getLine();
        Optional<ElementSymbol> symbol = globalScope.resolveElement(elementName);
        if (symbol.isEmpty()) {
          errors.add("Line " + line + ": PickUpHL references undefined element '" + elementName + "'");
        }
      }

      @Override
      public void visit(ASTPlaceHL node) {
        String elementName = node.getObj();
        int line = node.get_SourcePositionStart().getLine();
        Optional<ElementSymbol> symbol = globalScope.resolveElement(elementName);
        if (symbol.isEmpty()) {
          errors.add("Line " + line + ": PlaceHL references undefined element '" + elementName + "'");
        }
      }
    });

    ast.accept(traverser);
    return errors;
  }

  private static List<String> collectFindings() {
    List<String> out = new ArrayList<>();
    try {
      Log.getFindings().forEach(f -> out.add(f.buildMsg()));
    } catch (Exception ignored) {
      // If logging impl changes, keep response stable
    }
    return out;
  }

  private static String json(boolean ok, String treeName, List<String> errors, List<String> findings, GraphExport graph) {
    StringBuilder sb = new StringBuilder();
    sb.append("{");
    sb.append("\"ok\":").append(ok);
    if (treeName != null) {
      sb.append(",\"treeName\":\"").append(escape(treeName)).append("\"");
    }
    sb.append(",\"errors\":").append(stringArray(errors));
    sb.append(",\"findings\":").append(stringArray(findings));
    if (graph != null) {
      sb.append(",\"graph\":").append(graph.toJson());
    }
    sb.append("}");
    return sb.toString();
  }

  private static String findingsJson(boolean ok, String treeName, String error, List<String> findings) {
    List<String> errors = new ArrayList<>();
    if (error != null && !error.isBlank()) {
      errors.add(error);
    }
    return json(ok, treeName, errors, findings, null);
  }

  private static String errorJson(String message) {
    List<String> errors = new ArrayList<>();
    errors.add(message);
    return json(false, null, errors, new ArrayList<>(), null);
  }

  /**
   * Lightweight graph export that the frontend can turn into a canvas graph.
   * Purposefully avoids JSON libs to keep the tool jar small and stable.
   */
  static final class GraphExport {
    static final class Node {
      final String id;
      final String kind;
      final String label;
      final String name;
      final String astType;
      final Integer line;
      final String successType;

      Node(String id, String kind, String label, String name, String astType, Integer line, String successType) {
        this.id = id;
        this.kind = kind;
        this.label = label;
        this.name = name;
        this.astType = astType;
        this.line = line;
        this.successType = successType;
      }
    }

    static final class Edge {
      final String id;
      final String sourceId;
      final String targetId;
      final String kind;
      final String label;

      Edge(String id, String sourceId, String targetId, String kind, String label) {
        this.id = id;
        this.sourceId = sourceId;
        this.targetId = targetId;
        this.kind = kind;
        this.label = label;
      }
    }

    final String rootId;
    final List<Node> nodes;
    final List<Edge> edges;

    GraphExport(String rootId, List<Node> nodes, List<Edge> edges) {
      this.rootId = rootId;
      this.nodes = nodes;
      this.edges = edges;
    }

    static GraphExport fromTree(ASTAPTree tree) {
      if (tree == null || tree.getRoot() == null) {
        return new GraphExport(null, new ArrayList<>(), new ArrayList<>());
      }

      Builder b = new Builder();
      String root = b.ensureNode(tree.getRoot());
      b.visitFlowNode(tree.getRoot());
      return new GraphExport(root, b.nodes, b.edges);
    }

    String toJson() {
      StringBuilder sb = new StringBuilder();
      sb.append("{");
      if (rootId != null) {
        sb.append("\"rootId\":\"").append(escape(rootId)).append("\",");
      } else {
        sb.append("\"rootId\":null,");
      }
      sb.append("\"nodes\":[");
      for (int i = 0; i < nodes.size(); i++) {
        if (i > 0) sb.append(",");
        Node n = nodes.get(i);
        sb.append("{");
        sb.append("\"id\":\"").append(escape(n.id)).append("\"");
        sb.append(",\"kind\":\"").append(escape(n.kind)).append("\"");
        sb.append(",\"label\":\"").append(escape(n.label)).append("\"");
        if (n.name != null) sb.append(",\"name\":\"").append(escape(n.name)).append("\"");
        if (n.astType != null) sb.append(",\"astType\":\"").append(escape(n.astType)).append("\"");
        if (n.line != null) sb.append(",\"line\":").append(n.line);
        if (n.successType != null) sb.append(",\"successType\":\"").append(escape(n.successType)).append("\"");
        sb.append("}");
      }
      sb.append("],\"edges\":[");
      for (int i = 0; i < edges.size(); i++) {
        if (i > 0) sb.append(",");
        Edge e = edges.get(i);
        sb.append("{");
        sb.append("\"id\":\"").append(escape(e.id)).append("\"");
        sb.append(",\"sourceId\":\"").append(escape(e.sourceId)).append("\"");
        sb.append(",\"targetId\":\"").append(escape(e.targetId)).append("\"");
        sb.append(",\"kind\":\"").append(escape(e.kind)).append("\"");
        if (e.label != null) sb.append(",\"label\":\"").append(escape(e.label)).append("\"");
        sb.append("}");
      }
      sb.append("]}");
      return sb.toString();
    }

    static final class Builder {
      final List<Node> nodes = new ArrayList<>();
      final List<Edge> edges = new ArrayList<>();
      final java.util.IdentityHashMap<Object, String> ids = new java.util.IdentityHashMap<>();
      int nextNodeId = 1;
      int nextEdgeId = 1;

      String ensureNode(Object astNode) {
        if (astNode == null) {
          return null;
        }
        String existing = ids.get(astNode);
        if (existing != null) {
          return existing;
        }

        String id = "n" + (nextNodeId++);
        ids.put(astNode, id);

        String astType = astNode.getClass().getSimpleName();
        String kind = classifyKind(astNode);
        String name = tryGetName(astNode);
        String label = buildLabel(astNode, name);
        Integer line = tryGetLine(astNode);
        String successType = tryGetSuccessType(astNode);
        nodes.add(new Node(id, kind, label, name, astType, line, successType));
        return id;
      }

      void addEdge(String sourceId, String targetId, String kind, String label) {
        if (sourceId == null || targetId == null) return;
        String id = "e" + (nextEdgeId++);
        edges.add(new Edge(id, sourceId, targetId, kind, label));
      }

      void visitFlowNode(behaviortree._ast.ASTFlowNode flow) {
        if (flow == null) return;
        String flowId = ensureNode(flow);

        // services/decorators as separate nodes so the UI can show them
        for (behaviortree._ast.ASTService service : flow.getServiceList()) {
          String sid = ensureNode(service);
          addEdge(flowId, sid, "service", null);
        }
        for (behaviortree._ast.ASTDecorator decorator : flow.getDecoratorList()) {
          String did = ensureNode(decorator);
          addEdge(flowId, did, "decorator", null);
        }

        if (flow instanceof dynamicbtflownode._ast.ASTDynamicFlowNode) {
          dynamicbtflownode._ast.ASTDynamicFlowNode dyn = (dynamicbtflownode._ast.ASTDynamicFlowNode) flow;
          if (dyn.getNodeGraph() != null) {
            visitNodeGraph(flowId, dyn.getNodeGraph());
          }
        }

        // children (nested flow nodes or other BT nodes)
        for (behaviortree._ast.ASTBTNode child : flow.getChildrenList()) {
          String cid = ensureNode(child);
          addEdge(flowId, cid, "child", null);
          if (child instanceof behaviortree._ast.ASTFlowNode) {
            visitFlowNode((behaviortree._ast.ASTFlowNode) child);
          }
        }
      }

      void visitNodeGraph(String ownerId, dynamicbtflownode._ast.ASTNodeGraph graph) {
        if (graph == null) return;
        String graphId = ensureNode(graph);
        addEdge(ownerId, graphId, "contains", null);

        // First pass: ensure all graph member nodes exist
        java.util.Map<String, String> byName = new java.util.HashMap<>();
        for (dynamicbtflownode._ast.ASTGraphNode gn : graph.getNodesList()) {
          behaviortree._ast.ASTBTNode node = gn.getNode();
          String nid = ensureNode(node);
          addEdge(graphId, nid, "member", null);
          String name = tryGetName(node);
          if (name != null && !name.isBlank()) {
            byName.put(name, nid);
          }

          if (node instanceof behaviortree._ast.ASTFlowNode) {
            visitFlowNode((behaviortree._ast.ASTFlowNode) node);
          }
        }

        // Second pass: add successor edges
        for (dynamicbtflownode._ast.ASTGraphNode gn : graph.getNodesList()) {
          behaviortree._ast.ASTBTNode source = gn.getNode();
          String sourceId = ensureNode(source);
          for (dynamicbtflownode._ast.ASTRelation rel : gn.getSuccessorsList()) {
            String targetName = rel.getTarget();
            String targetId = byName.get(targetName);
            if (targetId == null) {
              continue;
            }
            String label = null;
            try {
              if (rel.getTemptype() != null) {
                label = rel.getTemptype().getClass().getSimpleName().replace("AST", "");
              }
            } catch (Exception ignored) {
              // optional
            }
            addEdge(sourceId, targetId, "relation", label);
          }
        }
      }

      static String classifyKind(Object node) {
        if (node instanceof dynamicbtflownode._ast.ASTNodeGraph) return "nodeGraph";
        if (node instanceof dynamicbtflownode._ast.ASTGraphNode) return "graphNode";
        if (node instanceof behaviortree._ast.ASTFlowNode) return "flow";
        if (node instanceof behaviortree._ast.ASTActionNode) return "action";
        if (node instanceof behaviortree._ast.ASTService) return "service";
        if (node instanceof behaviortree._ast.ASTDecorator) return "decorator";
        // Many concrete action nodes come from extended grammars and still end up as BTNodes.
        if (node instanceof behaviortree._ast.ASTBTNode) return "btNode";
        return "ast";
      }

      static String tryGetName(Object node) {
        try {
          if (node instanceof behaviortree._ast.ASTBTNode) {
            return ((behaviortree._ast.ASTBTNode) node).getName();
          }
          if (node instanceof behaviortree._ast.ASTService) {
            return ((behaviortree._ast.ASTService) node).getName();
          }
          if (node instanceof behaviortree._ast.ASTDecorator) {
            return ((behaviortree._ast.ASTDecorator) node).getName();
          }
          if (node instanceof dynamicbtflownode._ast.ASTNodeGraph) {
            return ((dynamicbtflownode._ast.ASTNodeGraph) node).getName();
          }
        } catch (Exception ignored) {
          // best-effort
        }
        return null;
      }

      static String tryGetSuccessType(Object node) {
        if (!(node instanceof behaviortree._ast.ASTFlowNode)) {
          return null;
        }

        // Most flow node variants expose a succri (success criteria) enum.
        // We use reflection to avoid depending on a specific subtype.
        try {
          java.lang.reflect.Method getter = node.getClass().getMethod("getSuccri");
          Object value = getter.invoke(node);
          if (value == null) return null;
          if (value instanceof Enum) {
            return ((Enum<?>) value).name();
          }
          return String.valueOf(value);
        } catch (Exception ignored) {
          return null;
        }
      }

      static Integer tryGetLine(Object node) {
        try {
          if (node instanceof de.monticore.ast.ASTNode) {
            de.monticore.ast.ASTNode ast = (de.monticore.ast.ASTNode) node;
            de.se_rwth.commons.SourcePosition pos = ast.get_SourcePositionStart();
            if (pos != null) {
              int line = pos.getLine();
              return line > 0 ? line : null;
            }
          }
        } catch (Exception ignored) {
          // best-effort
        }
        return null;
      }

      static String buildLabel(Object astNode, String name) {
        String type = astNode.getClass().getSimpleName();
        String typeLabel = type.startsWith("AST") ? type.substring(3) : type;

        if (astNode instanceof dynamicbtflownode._ast.ASTNodeGraph) {
          return name != null && !name.isBlank() ? "NodeGraph " + name : "NodeGraph";
        }
        if (astNode instanceof behaviortree._ast.ASTFlowNode) {
          return name != null && !name.isBlank() ? "FlowNode " + name : "FlowNode";
        }
        if (astNode instanceof behaviortree._ast.ASTService) {
          return name != null && !name.isBlank() ? "Service " + name : "Service";
        }
        if (astNode instanceof behaviortree._ast.ASTDecorator) {
          return name != null && !name.isBlank() ? "Decorator " + name : "Decorator";
        }
        if (name != null && !name.isBlank()) {
          return typeLabel + " " + name;
        }
        return typeLabel;
      }
    }
  }

  private static String stringArray(List<String> items) {
    StringBuilder sb = new StringBuilder();
    sb.append("[");
    boolean first = true;
    for (String item : items) {
      if (!first) sb.append(",");
      first = false;
      sb.append("\"").append(escape(item)).append("\"");
    }
    sb.append("]");
    return sb.toString();
  }

  private static String escape(String s) {
    if (s == null) return "";
    StringBuilder sb = new StringBuilder();
    for (int i = 0; i < s.length(); i++) {
      char c = s.charAt(i);
      switch (c) {
        case '"': sb.append("\\\""); break;
        case '\\': sb.append("\\\\"); break;
        case '\b': sb.append("\\b"); break;
        case '\f': sb.append("\\f"); break;
        case '\n': sb.append("\\n"); break;
        case '\r': sb.append("\\r"); break;
        case '\t': sb.append("\\t"); break;
        default:
          if (c < 0x20) {
            sb.append(String.format("\\u%04x", (int) c));
          } else {
            sb.append(c);
          }
      }
    }
    return sb.toString();
  }

  private static String safe(String s) {
    return s == null ? "" : s;
  }
}

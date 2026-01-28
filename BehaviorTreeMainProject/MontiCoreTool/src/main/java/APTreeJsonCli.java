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

    APTreeJsonCli cli = new APTreeJsonCli();
    System.out.print(cli.run(modelFile, instancesFile));
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

      // Configure symbol path (for persisted symbols if used)
      DynamicBTFlowNodeMill.globalScope().setSymbolPath(
          new de.monticore.io.paths.MCPath(Paths.get("target", "symbols"))
      );

      Optional<ASTAPTree> parsed = DynamicBTFlowNodeMill.parser().parseAPTree(modelFile);
      if (parsed.isEmpty()) {
        return findingsJson(false, null, "Parsing failed", collectFindings());
      }

      ASTAPTree ast = parsed.get();

      // Pre-validation: element references must resolve
      List<String> preValidationErrors = validateElementReferences(ast);
      if (!preValidationErrors.isEmpty()) {
        return json(false, ast.getName(), preValidationErrors, collectFindings());
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
      return json(ok, ast.getName(), new ArrayList<>(), findings);

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
      for (var fpSymbol : instanceScope.getLocalFirstPositionSymbols()) {
        DynamicBTFlowNodeMill.globalScope().add(fpSymbol);
      }

    } catch (Exception ignored) {
      // Surface via generic error finding if present; otherwise keep CLI stable
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

  private static String json(boolean ok, String treeName, List<String> errors, List<String> findings) {
    StringBuilder sb = new StringBuilder();
    sb.append("{");
    sb.append("\"ok\":").append(ok);
    if (treeName != null) {
      sb.append(",\"treeName\":\"").append(escape(treeName)).append("\"");
    }
    sb.append(",\"errors\":").append(stringArray(errors));
    sb.append(",\"findings\":").append(stringArray(findings));
    sb.append("}");
    return sb.toString();
  }

  private static String findingsJson(boolean ok, String treeName, String error, List<String> findings) {
    List<String> errors = new ArrayList<>();
    if (error != null && !error.isBlank()) {
      errors.add(error);
    }
    return json(ok, treeName, errors, findings);
  }

  private static String errorJson(String message) {
    List<String> errors = new ArrayList<>();
    errors.add(message);
    return json(false, null, errors, new ArrayList<>());
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

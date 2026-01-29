import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

import CoCos.CRFTypesCon.ElementExistsCoCo;
import crftypescon._ast.ASTPickUpHL;
import crftypescon._ast.ASTPlaceHL;
import crftypescon._ast.ASTWorld;
import crftypescon._cocos.CRFTypesConASTPickUpHLCoCo;
import crftypescon._cocos.CRFTypesConASTPlaceHLCoCo;
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

public class APTreeTool {

  public static void main(String[] args) {
    // Standard MontiCore logging setup
    Log.init();
    // Configure MontiCore logging to not fail-fast on errors
    // This allows our CoCos to run and provide better error messages
    Log.enableFailQuick(false);
    
    APTreeTool tool = new APTreeTool();
    String filePath = args.length > 0 ? args[0] : "src/test/resources/valid/behavior_trees/APTree.bt";
    tool.run(filePath);
  }

  public void run(String modelFile) {
    System.out.println("Running APTreeTool on: " + modelFile);

    // 1. Initialize the Mill
    DynamicBTFlowNodeMill.init();
    
    // 2. Load concrete instances into symbol table FIRST
    String instancesFile = "src/test/resources/valid/CRFConcrete/CRFConcreteInstances.bt";
    loadConcreteInstancesIntoGlobalScope(instancesFile);
    
    // 3. Configure Global Scope with symbol path
    DynamicBTFlowNodeMill.globalScope().setSymbolPath(
        new de.monticore.io.paths.MCPath(Paths.get("target", "symbols"))
    );

    try {
        // 4. Parse tree
        ASTAPTree ast = DynamicBTFlowNodeMill.parser().parseAPTree(modelFile)
             .orElseThrow(() -> new RuntimeException("Parsing failed for file: " + modelFile));
             
        System.out.println("[OK] SUCCESS: Syntactically parsed '" + ast.getName() + "'");
    
        // 4.5. PRE-VALIDATION: Check all element references BEFORE symbol table creation
        List<String> validationErrors = validateElementReferences(ast);
        if (!validationErrors.isEmpty()) {
            System.err.println("\n[X] VALIDATION FAILED: Found undefined element references:");
            System.err.println("Available elements: beam1, beam2, lp1, plate1, r1, FP1\n");
            for (String error : validationErrors) {
                System.err.println("  " + error);
            }
            System.err.println("\nPlease fix these references in your behavior tree file.");
            return;
        }
        System.out.println("[OK] Pre-validation passed: All element references are defined");
    
        // 5. Create Symbol Table
        IDynamicBTFlowNodeGlobalScope gs = DynamicBTFlowNodeMill.globalScope();
        IDynamicBTFlowNodeArtifactScope as;
        
        as = DynamicBTFlowNodeMill.scopesGenitorDelegator().createFromAST(ast);
        as.setEnclosingScope(gs);
        
        // 6. Run CoCo Checks
        DynamicBTFlowNodeCoCoChecker checker = new DynamicBTFlowNodeCoCoChecker();
        // Add custom checks (must register for each node type explicitly to avoid ambiguity)
        ElementExistsCoCo elementCheck = new ElementExistsCoCo();
        checker.addCoCo((CRFTypesConASTPickUpHLCoCo) elementCheck);
        checker.addCoCo((CRFTypesConASTPlaceHLCoCo) elementCheck);
        
        // Add default CoCos here if any exist in the language definition
        checker.checkAll((ASTDynamicBTFlowNodeNode) ast);
    
        System.out.println("[OK] SUCCESS: Model parsed and symbols checked successfully!");

    } catch (Exception e) {
        System.err.println("tool run failed: " + e.getMessage());
        e.printStackTrace();
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
      
      // Beam, Plate, Robot, FirstPosition all extend Element
      // Get symbols for each concrete type
      int count = 0;
      
      // Get Beam symbols
      for (var beamSymbol : instanceScope.getLocalBeamSymbols()) {
        System.out.println("  - Adding Beam: " + beamSymbol.getName());
        DynamicBTFlowNodeMill.globalScope().add(beamSymbol);
        count++;
      }
      
      // Get Plate symbols
      for (var plateSymbol : instanceScope.getLocalPlateSymbols()) {
        System.out.println("  - Adding Plate: " + plateSymbol.getName());
        DynamicBTFlowNodeMill.globalScope().add(plateSymbol);
        count++;
      }
      
      // Get Robot symbols
      for (var robotSymbol : instanceScope.getLocalRobotSymbols()) {
        System.out.println("  - Adding Robot: " + robotSymbol.getName());
        DynamicBTFlowNodeMill.globalScope().add(robotSymbol);
        count++;
      }
      
      // Get FirstPosition symbols
      for (var fpSymbol : instanceScope.getLocalFirstPosSymbols()) {
        System.out.println("  - Adding FirstPosition: " + fpSymbol.getName());
        DynamicBTFlowNodeMill.globalScope().add(fpSymbol);
        count++;
      }
      
      System.out.println("[OK] Loaded " + count + " element instances into global scope!");
      
    } catch (Exception e) {
      System.err.println("[X] ERROR loading instances: " + e.getMessage());
      e.printStackTrace();
    }
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

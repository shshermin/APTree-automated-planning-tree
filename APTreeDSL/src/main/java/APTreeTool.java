import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

import CoCos.DomainTypesCon.ElementExistsCoCo;
import CoCos.DomainTypesCon.ParameterInstanceExistsCoCo;
import CoCos.DynamicBTFlowNode.ActionNodesCannotHavePlanningService;
import CoCos.DynamicBTFlowNode.CausalLinkValidator;
import CoCos.DynamicBTFlowNode.MustHavePlanningService;
import CoCos.DynamicBTFlowNode.PlanningServiceActionsCoverageCoCo;
import CoCos.DynamicBTFlowNode.SharedResourceConflictCoCo;
import CoCos.DynamicBTFlowNode.UniquenessOfNames;
import CoCos.PlanningService.PlannerConfigurationCoCo;
import behaviortree._ast.ASTActionNode;
import behaviortree._cocos.BehaviorTreeASTDecoratorCoCo;
import behaviortree._cocos.BehaviorTreeASTServiceCoCo;
import domaintypescon._ast.ASTPickUpHL;
import domaintypescon._ast.ASTPlaceHL;
import domaintypescon._ast.ASTWorld;
import domaintypescon._cocos.DomainTypesConASTPickUpHLCoCo;
import domaintypescon._cocos.DomainTypesConASTPlaceHLCoCo;
import domaintypescon._parser.DomainTypesConParser;
import domaintypescon._visitor.DomainTypesConVisitor2;
import domaintypesdef._cocos.DomainTypesDefASTPActionNodeCoCo;
import domaintypesdef._symboltable.ElementSymbol;
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
import planningservice._cocos.PlanningServiceASTServicePDDLPlanningCoCo;

public class APTreeTool {

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
        
        long errorsBefore = Log.getErrorCount();
        as = DynamicBTFlowNodeMill.scopesGenitorDelegator().createFromAST(world);
        // Suppress MontiCore's internal 0xA7003/0xA7303 errors from scope creation
        // (unresolved symbol references will be caught by CoCos with better messages)
        Log.getFindings().removeIf(f -> f.getMsg().contains("0xA7003") || f.getMsg().contains("0xA7303"));
        as.setEnclosingScope(gs);
        // Set a name on the artifact scope so scope-chain resolution doesn't throw
        // 0xA7003 when calling getName() on a nameless scope
        as.setName(modelFile);
        
        // Also set names on any sub-scopes that may be unnamed
        setNamesOnUnnamedScopes(as, modelFile);
        
        // 6. Run CoCo Checks
        DynamicBTFlowNodeCoCoChecker checker = new DynamicBTFlowNodeCoCoChecker();
        // Add custom checks (must register for each node type explicitly to avoid ambiguity)
        ElementExistsCoCo elementCheck = new ElementExistsCoCo();
        checker.addCoCo((DomainTypesConASTPickUpHLCoCo) elementCheck);
        checker.addCoCo((DomainTypesConASTPlaceHLCoCo) elementCheck);
        // New: Every FlowNode must have at least one PlanningService
        MustHavePlanningService planningServiceCheck = new MustHavePlanningService();
       // checker.addCoCo(planningServiceCheck);
        // New: Action nodes cannot have PlanningService (generic, works with all ASTPActionNode subclasses)
        ActionNodesCannotHavePlanningService actionNodeCheck = new ActionNodesCannotHavePlanningService();
        checker.addCoCo((DomainTypesDefASTPActionNodeCoCo) actionNodeCheck);
        // New: Decorator and service names must be unique
        UniquenessOfNames uniquenessCheck = new UniquenessOfNames();
        checker.addCoCo((BehaviorTreeASTDecoratorCoCo) uniquenessCheck);
        checker.addCoCo((BehaviorTreeASTServiceCoCo) uniquenessCheck);
        PlannerConfigurationCoCo plannerConfigurationCheck = new PlannerConfigurationCoCo();
        checker.addCoCo((PlanningServiceASTServicePDDLPlanningCoCo) plannerConfigurationCheck);
        // New: Check that all action parameters resolve to known instances of the correct type
        ParameterInstanceExistsCoCo paramCheck = new ParameterInstanceExistsCoCo();
        checker.addCoCo((DomainTypesDefASTPActionNodeCoCo) paramCheck);
        // New: Validate causal links between connected actions
        CausalLinkValidator causalValidator = new CausalLinkValidator();
        // checker.addCoCo((DynamicBTFlowNodeASTGraphNodeCoCo) causalValidator);
        
        // New: Check that all actions in behavior tree are defined in planning service domain
        PlanningServiceActionsCoverageCoCo actionsCoverageCheck = new PlanningServiceActionsCoverageCoCo();
        // checker.addCoCo(actionsCoverageCheck);
        // New: Detect shared resources between parallel action sequences
        SharedResourceConflictCoCo sharedResourceCheck = new SharedResourceConflictCoCo();
        // checker.addCoCo(sharedResourceCheck);
        
        // Register all action instances to build type mapping
        registerActionInstances(ast, causalValidator);
        
        // Add default CoCos here if any exist in the language definition
        checker.checkAll((ASTDynamicBTFlowNodeNode) ast);
        
        // Remove MontiCore internal scope-resolution errors (0xA7003/0xA7303)
        // Our CoCos produce better messages for the same issues (0xDF020)
        Log.getFindings().removeIf(f -> f.getMsg().contains("0xA7003") || f.getMsg().contains("0xA7303"));
        
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
   * Load concrete instances (beam1, beam2, robot1, etc.) into the global scope
   * so they are available when the tree is parsed.
   * 
   * @param instancesFile Path to the concrete instances model file (e.g., CRFConcreteInstances.bt)
   */
  private void loadConcreteInstancesIntoGlobalScope(String instancesFile) {
    System.out.println("Loading concrete instances from: " + instancesFile);
    
    try {
      // Initialize DomainTypesCon mill (separate from DynamicBTFlowNode)
      domaintypescon.DomainTypesConMill.init();
      
      // Parse the concrete instances model
      DomainTypesConParser parser = domaintypescon.DomainTypesConMill.parser();
      Optional<ASTWorld> result = parser.parse(instancesFile);
      
      if (result.isEmpty()) {
        System.err.println("[!] Failed to parse instances file: " + instancesFile);
        return;
      }
      
      ASTWorld world = result.get();
      System.out.println("[OK] Parsed instances: " + instancesFile);
      
      // Create symbol table from instances AST
      var instanceScope = domaintypescon.DomainTypesConMill.scopesGenitorDelegator().createFromAST(world);
      
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

                                      // Copy the symbol into the composed language's global scope.
                                      // The DomainTypesCon artifact scope itself cannot be attached as
                                      // an IDynamicBTFlowNodeScope, but its inherited symbols are compatible.
                                      addSymbolToDynamicGlobalScope(obj);
                                      
                                        // Symbol loaded silently
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

  private void addSymbolToDynamicGlobalScope(Object symbol) throws ReflectiveOperationException {
    Object globalScope = DynamicBTFlowNodeMill.globalScope();

    // Find the most specific add() method for this symbol type.
    // getMethods() order is unspecified, so we must pick the most specific match
    // (e.g., add(RobotSymbol) over add(AgentSymbol)) to ensure correct symbol table registration.
    java.lang.reflect.Method bestMatch = null;
    for (java.lang.reflect.Method method : globalScope.getClass().getMethods()) {
      if (method.getName().equals("add")
          && method.getParameterCount() == 1
          && method.getParameterTypes()[0].isAssignableFrom(symbol.getClass())) {
        if (bestMatch == null
            || bestMatch.getParameterTypes()[0].isAssignableFrom(method.getParameterTypes()[0])) {
          bestMatch = method;
        }
      }
    }

    if (bestMatch != null) {
      bestMatch.invoke(globalScope, symbol);
      return;
    }

    throw new NoSuchMethodException(
        "No compatible global-scope add method for " + symbol.getClass().getName());
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
          // Action registered silently
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
    
    traverser.add4DomainTypesCon(new DomainTypesConVisitor2() {
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

  /**
   * Walk all sub-scopes and assign a name to any scope that doesn't have one.
   * MontiCore's scope-chain resolution calls getName() on every scope in the chain;
   * if a scope has Optional.empty() as its name, it throws 0xA7003.
   */
  private void setNamesOnUnnamedScopes(dynamicbtflownode._symboltable.IDynamicBTFlowNodeScope scope, String baseName) {
    if (scope instanceof dynamicbtflownode._symboltable.DynamicBTFlowNodeScope) {
      dynamicbtflownode._symboltable.DynamicBTFlowNodeScope concreteScope =
          (dynamicbtflownode._symboltable.DynamicBTFlowNodeScope) scope;
      if (!concreteScope.isPresentName()) {
        concreteScope.setName(baseName);
      }
      for (dynamicbtflownode._symboltable.IDynamicBTFlowNodeScope sub : concreteScope.getSubScopes()) {
        setNamesOnUnnamedScopes(sub, baseName);
      }
    }
  }
}

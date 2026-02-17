package CoCos.DynamicBTFlowNode;

import java.util.List;

import crftypesdef._ast.ASTPActionNode;
import crftypesdef._cocos.CRFTypesDefASTPActionNodeCoCo;
import behaviortree._ast.ASTService;
import de.se_rwth.commons.logging.Log;
import planningservice._ast.ASTServicePlanning;

/**
 * CoCo: Action nodes cannot declare PlanningService.
 * 
 * This rule works with all subclasses of ASTPActionNode (e.g., ASTPickUpHL, ASTPlaceHL).
 * Action nodes may have other services (e.g., decorators), but not planning services.
 * 
 * Error code: 0xDF002
 */
public class ActionNodesCannotHavePlanningService implements CRFTypesDefASTPActionNodeCoCo {

  @Override
  public void check(ASTPActionNode node) {
    // Get services list from the action node
    List<ASTService> services = node.getServiceList();
    
    // Check if any service is a PlanningService
    for (ASTService service : services) {
      if (service instanceof ASTServicePlanning) {
        String nodeName = (node.getName() != null) ? node.getName() : "<unnamed>";
        Log.error(
            String.format("0xDF002 Action node '%s' cannot declare a PlanningService.", nodeName),
            node.get_SourcePositionStart()
        );
        break; // Report only once per node
      }
    }
  }
}

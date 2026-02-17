package CoCos.DynamicBTFlowNode;

import java.util.List;

import behaviortree._ast.ASTService;
import de.se_rwth.commons.logging.Log;
import dynamicbtflownode._ast.ASTDynamicFlowNode;
import dynamicbtflownode._cocos.DynamicBTFlowNodeASTDynamicFlowNodeCoCo;
import planningservice._ast.ASTServicePlanning;

/**
 * CoCo: Every Dynamic FlowNode must declare at least one PlanningService.
 * A PlanningService is any service derived from PlanningService grammar (e.g., PDDLPlannerService).
 */
public class MustHavePlanningService implements DynamicBTFlowNodeASTDynamicFlowNodeCoCo {

  @Override
  public void check(ASTDynamicFlowNode node) {
    // Services are inherited from BehaviorTree's ASTFlowNode
    List<ASTService> services = node.getServiceList();
    String nodeName = (node.getName() != null) ? node.getName() : "<unnamed>";

    System.out.println("[DEBUG] Checking DynamicFlowNode: " + nodeName + ", services count: " + services.size());
    for (ASTService svc : services) {
      System.out.println("  - Service type: " + svc.getClass().getName());
    }

    boolean hasPlanningService = services.stream()
        .anyMatch(s -> s instanceof ASTServicePlanning);

    if (!hasPlanningService) {
      Log.error(
          String.format("0xDF001 FlowNode '%s' must declare at least one PlanningService.", nodeName),
          node.get_SourcePositionStart()
      );
    }
  }
}

package CoCos.PlanningService;

import planningservice._ast.ASTPlannerENHSP;
import planningservice._ast.ASTProblem;
import planningservice._cocos.PlanningServiceASTPlannerENHSPCoCo;
import de.se_rwth.commons.logging.Log;

/**
 * CoCo: The Problem referenced in a Planner must belong to the same Domain
 * that the Planner references.
 *
 * Checks that planner.problem.domain == planner.domain.
 *
 * Error code: 0xPS001
 */
public class ProblemDomainMatchCoCo implements PlanningServiceASTPlannerENHSPCoCo {

  @Override
  public void check(ASTPlannerENHSP node) {
    // Only check if a problem is actually specified (it's optional)
    if (!node.isPresentProblem()) {
      return;
    }

    String plannerDomain = node.getDomain();

    // Resolve the Problem to get its AST node
    if (!node.isPresentProblemDefinition()) {
      // Problem symbol could not be resolved — other CoCos/symbol resolution handles this
      return;
    }

    ASTProblem problem = node.getProblemDefinition();
    String problemDomain = problem.getDomain();

    if (!plannerDomain.equals(problemDomain)) {
      Log.error(
          String.format("0xPS001 Problem '%s' declares Domain '%s', " +
              "but the Planner references Domain '%s'. They must match.",
              node.getProblem(), problemDomain, plannerDomain),
          node.get_SourcePositionStart()
      );
    }
  }
}

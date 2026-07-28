package CoCos.PlanningService;

import java.util.Optional;

import CoCos.PlanningService.PlannerCapabilityRegistry.PlannerCapabilities;
import CoCos.PlanningService.PlanningDomainCatalog.DomainMetadata;
import de.se_rwth.commons.logging.Log;
import planningservice._ast.ASTPlanner;
import planningservice._ast.ASTPlannerENHSP;
import planningservice._ast.ASTPlannerFF;
import planningservice._ast.ASTPlannerPDDL;
import planningservice._ast.ASTServicePDDLPlanning;
import planningservice._cocos.PlanningServiceASTServicePDDLPlanningCoCo;

public class PlannerConfigurationCoCo implements PlanningServiceASTServicePDDLPlanningCoCo {

  @Override
  public void check(ASTServicePDDLPlanning service) {
    ASTPlanner planner = service.getPlanner();
    if (!(planner instanceof ASTPlannerPDDL)) {
      return;
    }

    ASTPlannerPDDL pddlPlanner = (ASTPlannerPDDL) planner;
    String plannerName = getPlannerName(planner);
    Optional<PlannerCapabilities> capabilities = PlannerCapabilityRegistry.find(plannerName);

    if (pddlPlanner.isPresentConfig()
        && (!capabilities.isPresent() || !capabilities.get().supportsConfig(pddlPlanner.getConfig()))) {
      String supported = capabilities.map(value -> String.join(", ", value.getConfigs())).orElse("none");
      Log.error(String.format(
          "0xDF006 Planner '%s' does not support config '%s'. Supported configs: %s.",
          plannerName, pddlPlanner.getConfig(), supported), planner.get_SourcePositionStart());
    }

    Optional<DomainMetadata> domain = PlanningDomainCatalog.resolve(pddlPlanner.getDomain());
    if (!domain.isPresent()) {
      Log.error(String.format(
          "0xDF007 Planning domain file '%s.pddl' is not specified or cannot be resolved.",
          pddlPlanner.getDomain()), planner.get_SourcePositionStart());
      return;
    }

    if (capabilities.isPresent()
        && domain.get().getLanguageVersion() > capabilities.get().getMaximumPddlVersion()) {
      Log.error(String.format(
          "0xDF008 Planner '%s' supports PDDL up to %.1f, but domain '%s' uses PDDL %.1f.",
          plannerName, capabilities.get().getMaximumPddlVersion(), domain.get().getName(),
          domain.get().getLanguageVersion()), planner.get_SourcePositionStart());
    }
  }

  private String getPlannerName(ASTPlanner planner) {
    if (planner instanceof ASTPlannerENHSP) {
      return "ENHSP";
    }
    if (planner instanceof ASTPlannerFF) {
      return "FF";
    }
    return planner.getClass().getSimpleName().replaceFirst("^ASTPlanner", "");
  }
}
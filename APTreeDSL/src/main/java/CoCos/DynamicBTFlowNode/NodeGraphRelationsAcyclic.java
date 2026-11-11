package CoCos.DynamicBTFlowNode;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import de.se_rwth.commons.logging.Log;
import dynamicbtflownode._ast.ASTGraphNode;
import dynamicbtflownode._ast.ASTNodeGraph;
import dynamicbtflownode._ast.ASTRelation;
import dynamicbtflownode._cocos.DynamicBTFlowNodeASTNodeGraphCoCo;

/**
 * CoCo: the temporal relations inside one NodeGraph must not form a cycle.
 *
 * The execution engine turns every relation into an ordering edge, so a cycle
 * (including a node related to itself, e.g. "a --[Equals]--> a") leaves nodes
 * waiting for each other and the graph never finishes.
 *
 * Error codes: 0xDF010 (relation to itself), 0xDF011 (cycle).
 */
public class NodeGraphRelationsAcyclic implements DynamicBTFlowNodeASTNodeGraphCoCo {

  @Override
  public void check(ASTNodeGraph graph) {
    Map<String, List<ASTRelation>> edges = new LinkedHashMap<>();
    for (ASTGraphNode graphNode : graph.getNodesList()) {
      String from = graphNode.getNode().getName();
      List<ASTRelation> out = edges.computeIfAbsent(from, k -> new ArrayList<>());
      for (ASTRelation relation : graphNode.getSuccessorsList()) {
        if (from.equals(relation.getTarget())) {
          Log.error(String.format("0xDF010 Node '%s' is related to itself (%s).",
              from, relation.getTemptype()), relation.get_SourcePositionStart());
        } else {
          out.add(relation);
        }
      }
    }

    Map<String, Integer> state = new HashMap<>(); // 1 = on the current path, 2 = done
    for (String start : edges.keySet()) {
      if (!state.containsKey(start)) {
        findCycle(start, edges, state, new ArrayList<>());
      }
    }
  }

  private void findCycle(String node, Map<String, List<ASTRelation>> edges,
                         Map<String, Integer> state, List<String> path) {
    state.put(node, 1);
    path.add(node);
    for (ASTRelation relation : edges.getOrDefault(node, List.of())) {
      String next = relation.getTarget();
      Integer seen = state.get(next);
      if (seen == null) {
        findCycle(next, edges, state, path);
      } else if (seen == 1) {
        List<String> cycle = new ArrayList<>(path.subList(path.indexOf(next), path.size()));
        cycle.add(next);
        Log.error(String.format("0xDF011 Temporal relations form a cycle: %s.",
            String.join(" -> ", cycle)), relation.get_SourcePositionStart());
      }
    }
    path.remove(path.size() - 1);
    state.put(node, 2);
  }
}

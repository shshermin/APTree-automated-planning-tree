import de.se_rwth.commons.logging.Log;
import dynamicbtflownode.DynamicBTFlowNodeMill;
import dynamicbtflownode._ast.ASTDynamicFlowNode;
import dynamicbtflownode._ast.ASTFinalWorld;
import dynamicbtflownode._ast.ASTGraphNode;
import dynamicbtflownode._ast.ASTRelation;
import dynamicbtflownode._ast.ASTTemporalType;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.io.IOException;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

/**
 * Each of the seven TemporalType values (Meets,
 * Precedes, Overlaps, Starts, Finishes, Contains, Equals) is parsed into the
 * correct edge (temptype + target) in the resolved GraphNode successor list.
 *
 * No existing fixture in the repo actually uses anything but Meets, so this
 * exercises a small dedicated fixture (TemporalRelations.bt) built purely to
 * cover the other six relation types.
 */
class TemporalRelationTest {

    @BeforeAll
    static void initMill() {
        DynamicBTFlowNodeMill.init();
        Log.init();
        Log.enableFailQuick(false);
    }

    @Test
    void eachTemporalRelationTypeProducesTheCorrectEdge() throws IOException {
        ASTFinalWorld world = APTreeModelParser.parseModel(
                "src/test/resources/valid/behavior_trees/TemporalRelations.bt");

        ASTDynamicFlowNode main = (ASTDynamicFlowNode) world.getAPTreeList().get(0).getRoot();

        // Relations attach to FlowNode-typed graph nodes as a separate `{ ... }`
        // block after the FlowNode's own (mandatory) body — not to Action nodes,
        // whose own optional trailing `{ (Decorator|Service)* }` block in the
        // CRFTypesCon grammar shadows the GraphNode-level successors block.
        Map<String, ASTGraphNode> nodes = main.getNodeGraph().getNodesList().stream()
                .collect(Collectors.toMap(n -> n.getNode().getName(), n -> n));

        assertEdge(nodes, "a1", ASTTemporalType.MEETS, "a2");
        assertEdge(nodes, "a2", ASTTemporalType.PRECEDES, "a3");
        assertEdge(nodes, "a3", ASTTemporalType.OVERLAPS, "a4");
        assertEdge(nodes, "a4", ASTTemporalType.STARTS, "a5");
        assertEdge(nodes, "a5", ASTTemporalType.FINISHES, "a6");
        assertEdge(nodes, "a6", ASTTemporalType.CONTAINS, "a7");
        assertEdge(nodes, "a7", ASTTemporalType.EQUALS, "a1");
    }

    private static void assertEdge(Map<String, ASTGraphNode> nodes, String sourceName,
                                    ASTTemporalType expectedType, String expectedTarget) {
        ASTGraphNode source = nodes.get(sourceName);
        assertTrue(source != null, "graph node not found: " + sourceName);

        List<ASTRelation> successors = source.getSuccessorsList();
        assertEquals(1, successors.size(), sourceName + " should have exactly one successor edge");

        ASTRelation edge = successors.get(0);
        assertEquals(expectedType, edge.getTemptype(), sourceName + " relation type");
        assertEquals(expectedTarget, edge.getTarget(), sourceName + " relation target");
    }
}

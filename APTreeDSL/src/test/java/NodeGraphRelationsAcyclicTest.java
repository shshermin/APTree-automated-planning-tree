import CoCos.DynamicBTFlowNode.NodeGraphRelationsAcyclic;
import de.se_rwth.commons.logging.Finding;
import de.se_rwth.commons.logging.Log;
import dynamicbtflownode.DynamicBTFlowNodeMill;
import dynamicbtflownode._ast.ASTFinalWorld;
import dynamicbtflownode._cocos.DynamicBTFlowNodeCoCoChecker;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.io.File;
import java.io.IOException;
import java.util.List;
import java.util.stream.Collectors;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

/** Temporal relations inside a NodeGraph must not form a cycle (CoCo 0xDF010 / 0xDF011). */
class NodeGraphRelationsAcyclicTest {

    @BeforeEach
    void initMill() {
        DynamicBTFlowNodeMill.init();
        Log.init();
        Log.enableFailQuick(false);
    }

    private static List<String> errorsFor(String modelPath) throws IOException {
        ASTFinalWorld world = APTreeModelParser.parseModel(modelPath);
        assertNotNull(world, "fixture must parse: " + modelPath);
        Log.getFindings().clear();

        DynamicBTFlowNodeCoCoChecker checker = new DynamicBTFlowNodeCoCoChecker();
        checker.addCoCo(new NodeGraphRelationsAcyclic());
        checker.checkAll(world);

        return Log.getFindings().stream()
                .filter(Finding::isError)
                .map(Finding::getMsg)
                .collect(Collectors.toList());
    }

    /** The CoCo also runs on the editor's validate call, so no shipped model may trip it. */
    @Test
    void noShippedModel_HasCyclicRelations() throws IOException {
        File[] models = new File("src/test/resources/valid/behavior_trees").listFiles((d, n) -> n.endsWith(".bt"));
        assertNotNull(models);
        List<File> all = new java.util.ArrayList<>(List.of(models));
        all.add(new File("../DemonstratorFinal.bt"));

        int checked = 0;
        for (File model : all) {
            DynamicBTFlowNodeMill.init();
            Log.init();
            Log.enableFailQuick(false);
            ASTFinalWorld world = APTreeModelParser.parseModel(model.getPath());
            if (world == null) continue; // known-broken fixtures, see APTreeModelParserTest
            Log.getFindings().clear();

            DynamicBTFlowNodeCoCoChecker checker = new DynamicBTFlowNodeCoCoChecker();
            checker.addCoCo(new NodeGraphRelationsAcyclic());
            checker.checkAll(world);

            List<String> relationErrors = Log.getFindings().stream()
                    .map(Finding::getMsg)
                    .filter(m -> m.startsWith("0xDF010") || m.startsWith("0xDF011"))
                    .collect(Collectors.toList());
            assertEquals(List.of(), relationErrors, model.getName());
            checked++;
        }
        assertTrue(checked > 0, "no model parsed");
    }

    @Test
    void aChainOfRelations_IsAccepted() throws IOException {
        assertEquals(List.of(), errorsFor("src/test/resources/valid/behavior_trees/TemporalRelations.bt"));
    }

    @Test
    void aCycleOverSeveralNodes_IsRejectedWithThePath() throws IOException {
        List<String> errors = errorsFor("src/test/resources/invalid/temporal_relations/Cycle.bt");

        assertEquals(1, errors.size(), errors.toString());
        assertTrue(errors.get(0).startsWith("0xDF011"), errors.get(0));
        assertTrue(errors.get(0).contains("a -> b -> c -> a"), errors.get(0));
    }

    @Test
    void aRelationToItself_IsRejected() throws IOException {
        List<String> errors = errorsFor("src/test/resources/invalid/temporal_relations/SelfRelation.bt");

        assertEquals(1, errors.size(), errors.toString());
        assertTrue(errors.get(0).startsWith("0xDF010"), errors.get(0));
    }
}

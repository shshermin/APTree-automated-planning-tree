import de.se_rwth.commons.logging.Log;
import dynamicbtflownode.DynamicBTFlowNodeMill;
import dynamicbtflownode._ast.ASTFinalWorld;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.io.IOException;

import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertFalse;

/**
 * Phase 0 smoke test: proves the JUnit 5 harness is wired up correctly
 * (resolves generated MontiCore parser classes, runs under `gradle test`).
 * Real DSL coverage per test-plan section 1 lands in later phases.
 */
public class InfrastructureSmokeTest {

    @BeforeAll
    static void initMontiCore() {
        // Mirrors APTreeModelParser.main(): the mill must be initialized and
        // fail-quick disabled before parsing, otherwise Log.error() on a parse
        // issue terminates the whole JVM instead of just failing the test.
        DynamicBTFlowNodeMill.init();
        Log.init();
        Log.enableFailQuick(false);
    }

    @Test
    void parsesAKnownGoodModel() throws IOException {
        ASTFinalWorld world = APTreeModelParser.parseModel(
                "src/test/resources/valid/behavior_trees/APTreeLiveMat.bt");

        assertNotNull(world, "Parser should successfully parse a known-good model");
        assertFalse(world.getAPTreeList().isEmpty(), "Parsed model should contain at least one behavior tree");
    }
}

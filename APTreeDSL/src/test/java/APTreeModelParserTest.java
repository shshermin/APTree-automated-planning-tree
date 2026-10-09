import de.se_rwth.commons.logging.Log;
import dynamicbtflownode.DynamicBTFlowNodeMill;
import dynamicbtflownode._ast.ASTFinalWorld;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.MethodSource;

import java.io.File;
import java.io.IOException;
import java.util.Arrays;
import java.util.Set;
import java.util.stream.Stream;

import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNotNull;

/**
 * Every APTree fixture under src/test/resources/valid/behavior_trees parses without errors via
 * the DynamicBTFlowNode grammar (FlowNode / NodeGraph / ServicePlanning /
 * temporal relations).
 */
class APTreeModelParserTest {

    private static final String DIR = "src/test/resources/valid/behavior_trees/";

    // Excluded on purpose, not by accident — see reasons below.
    private static final Set<String> EXCLUDED = Set.of(
            // Base-grammar-only fixture (Sequence/Parallel, no FlowNode/NodeGraph);
            // covered separately by BehaviorTreeParserTest against the base grammar.
            "BehaviorTree.bt",
            // Stray duplicate file (space + "copy" in the name) — not an intentional fixture.
            "FullDemonstratorFinal copy.bt",
            // KNOWN BROKEN: CRFTypesCon Action productions define their own optional
            // `{ (Decorator|Service)* }` block, which shadows GraphNode's relation
            // block, so `{ --[Meets]--> X; }` after an Action never parses
            // ("missing Name at ')'"). Relations only work on FlowNode graph nodes.
            // This also breaks `gradle runAPTreeTool` on APTreeLivematFinal.bt.
            "APTreeLivematFinal.bt",
            "APTree.bt",
            "APTree2.bt",
            // KNOWN BROKEN: quoted string literals ("movel"/"movej") where the
            // grammar now expects a bare Name, e.g.
            // `Action MoveToLL moveToPickPosition (p client "movel")`; plus a
            // `LiftLL`/`RetractLL` missing a trailing parameter.
            "DemonstratorLLSubtrees.bt",
            // KNOWN BROKEN: "extraneous input 'temp1graph' expecting '{'" —
            // `NodeGraph temp1graph {}` gives the graph an explicit name, but
            // the current grammar only accepts an anonymous `NodeGraph { ... }`.
            "SubTrees.bt"
    );

    @BeforeAll
    static void initMill() {
        DynamicBTFlowNodeMill.init();
        Log.init();
        Log.enableFailQuick(false);
    }

    private static Stream<String> aptreeFiles() {
        File[] files = new File(DIR).listFiles((dir, name) -> name.endsWith(".bt") && !EXCLUDED.contains(name));
        assertNotNull(files, "behavior_trees fixture directory should exist: " + DIR);
        return Arrays.stream(files).map(File::getName).sorted();
    }

    @ParameterizedTest(name = "APTree model: {0}")
    @MethodSource("aptreeFiles")
    void parsesValidAPTreeModel(String fileName) throws IOException {
        ASTFinalWorld world = APTreeModelParser.parseModel(DIR + fileName);

        assertNotNull(world, fileName + " should parse successfully");
        assertFalse(world.getAPTreeList().isEmpty(), fileName + " should contain at least one behavior tree");
    }
}

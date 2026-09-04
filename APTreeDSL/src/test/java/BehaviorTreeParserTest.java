import behaviortree.BehaviorTreeMill;
import behaviortree._ast.ASTBehaviorTree;
import de.se_rwth.commons.logging.Log;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;

import java.io.IOException;

import static org.junit.jupiter.api.Assertions.assertNotNull;

/**
 * A plain BehaviorTree model (Sequence/Parallel/Action/Decorator/Service, 
 * no APTree-specific FlowNode extensions) parses without errors via the 
 * base BehaviorTree grammar.
 */
class BehaviorTreeParserTest {

    @BeforeAll
    static void initMill() {
        BehaviorTreeMill.init();
        Log.init();
        Log.enableFailQuick(false);
    }

    @Test
    void parsesValidBaseBehaviorTree() throws IOException {
        ASTBehaviorTree bt = BehaviorTreeModelParser.parseModel(
                "src/test/resources/valid/behavior_trees/BehaviorTree.bt");

        assertNotNull(bt, "BehaviorTree.bt should parse successfully");
    }
}

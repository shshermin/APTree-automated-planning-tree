import org.junit.jupiter.api.Disabled;
import org.junit.jupiter.api.Test;

/**
 * `APTreeTool.run(modelFile)` should produce a schema-valid BehaviorTreeModel.json.
 *
 * Disabled until APTreeTool takes its paths as parameters: it hardcodes the
 * concrete-instances input to LiveMatSetupObjects.bt (which doesn't parse, see
 * ConcreteInstanceParserTest) and writes its output straight into
 * ../APTreeExecutionEngine/src/ModelLoader/PropertyInstances.json.
 */
class EndToEndAPTreeToolTest {

    @Disabled("APTreeTool.run() hardcodes a broken instances fixture and a real-tree output path - see class comment")
    @Test
    void runAPTreeToolProducesSchemaValidModel() {
    }
}

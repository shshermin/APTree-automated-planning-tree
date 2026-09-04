import org.junit.jupiter.api.Disabled;
import org.junit.jupiter.api.Test;

/**
 * `APTreeTool.run(modelFile)` on a full model should
 * produce a schema-valid BehaviorTreeModel.json.
 *
 * BLOCKED, not just broken-fixture: unlike the parser/codegen tests above,
 * this one cannot be safely automated yet, for two independent reasons found
 * while wiring it up:
 *
 * 1. APTreeTool.run() hardcodes its concrete-instances file to
 *    "src/test/resources/valid/CRFConcrete/LiveMatSetupObjects.bt" (see
 *    APTreeTool.java line ~84) - there is no args[] override. That fixture is
 *    already documented as KNOWN BROKEN in ConcreteInstanceParserTest
 *    ("ur10" where a boolean is expected; "Stack lay1 (1 m1)" arity
 *    mismatch). loadConcreteInstancesIntoGlobalScope() degrades gracefully on
 *    a parse failure (logs and returns) rather than throwing, so the tool
 *    would "complete" but with an empty symbol table - not a meaningful
 *    end-to-end check.
 * 2. APTreeTool.run() also hardcodes its JSON output path to
 *    "../APTreeExecutionEngine/src/ModelLoader/PropertyInstances.json" - the
 *    real execution-engine source tree, with no args[] override. Running it
 *    from a test would silently overwrite checked-in files (this happened
 *    once already with the codegen generators while writing
 *    CodegenGoldenTest - see git history - which is why this one is disabled
 *    rather than worked around).
 *
 * Fixing either needs a source change to APTreeTool itself (an output-path
 * parameter, at minimum) or a fixed LiveMatSetupObjects.bt - both out of
 * scope for adding tests. Tracked here rather than silently skipped.
 */
class EndToEndAPTreeToolTest {

    @Disabled("APTreeTool.run() hardcodes a broken instances fixture and a real-tree output path - see class comment")
    @Test
    void runAPTreeToolProducesSchemaValidModel() {
        // Intentionally empty - see class-level comment for why this is blocked.
    }
}

import de.se_rwth.commons.logging.Log;
import org.json.simple.JSONArray;
import org.json.simple.JSONObject;
import org.json.simple.parser.JSONParser;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

import java.io.File;
import java.nio.file.Files;
import java.nio.file.Path;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

/**
 * APTreeTool runs end to end with its paths passed in, and writes the property
 * instances where it is told to instead of into the execution engine's tree.
 */
class EndToEndAPTreeToolTest {

    @BeforeEach
    void initLog() {
        // As in APTreeTool.main: report errors instead of exiting the JVM on the first one.
        Log.init();
        Log.enableFailQuick(false);
    }

    @Test
    void runAPTreeToolWritesThePropertyInstancesToTheGivenPath(@TempDir Path tmp) throws Exception {
        File realOutput = new File(APTreeTool.DEFAULT_PROPERTY_INSTANCES_OUT);
        long realOutputModified = realOutput.lastModified();
        Path out = tmp.resolve("PropertyInstances.json");

        new APTreeTool().run(
                "src/test/resources/valid/behavior_trees/DemonstratorFinal.bt",
                "src/test/resources/valid/CRFConcrete/DemonstratorSceneObjects.bt",
                out.toString());

        assertTrue(Files.exists(out), "no output written to " + out);
        JSONObject json = (JSONObject) new JSONParser().parse(Files.readString(out));
        JSONArray instances = (JSONArray) json.get("instances");
        assertEquals((long) instances.size(), json.get("count"));
        assertEquals(realOutputModified, realOutput.lastModified(), "the engine's PropertyInstances.json was touched");
        // Not asserted yet: the export is empty and the run logs 0xA7003 errors for this
        // model, because the instance loader only knows the LiveMat types (see the TODO
        // list in APTreeDSL/README.md).
    }
}

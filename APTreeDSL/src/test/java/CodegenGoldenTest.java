import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

import java.io.File;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Arrays;
import java.util.Comparator;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;

/**
 * The three C# generators produce output that
 * matches a checked-in golden snapshot for a fixed, known-good input.
 *
 * Each generator's default input/output in its own DEFAULT_INPUT_PATH /
 * DEFAULT_OUTPUT_DIR fields points at the real APTreeExecutionEngine source
 * tree (../APTreeExecutionEngine/src/ModelLoader/...) - always pass an
 * explicit @TempDir as the output arg here, never rely on the default, or
 * this will silently overwrite real checked-in generated C# files (it did,
 * once, while this test was being written - see git history if curious).
 *
 * Also note: the DEFAULT_INPUT_PATH for CSharpPredicateGenerator is
 * LiveMatPredicaetTypes.bt, which is one of the fixtures documented as
 * KNOWN BROKEN in CRFTypesParserTest - so this test deliberately uses
 * CRFPredicateTypes.bt (a known-good fixture) instead.
 */
class CodegenGoldenTest {

    @Test
    void parameterTypeGeneratorMatchesGolden(@TempDir Path outputDir) throws IOException {
        CSharopCodeGenerator.main(new String[]{
                "src/test/resources/valid/CRFTypes/LiveMatPropertyTypes.bt",
                outputDir.toString()
        });

        assertGeneratedOutputMatchesGolden(outputDir, "src/test/resources/golden/parameterTypes");
    }

    @Test
    void predicateTypeGeneratorMatchesGolden(@TempDir Path outputDir) throws IOException {
        CSharpPredicateGenerator.main(new String[]{
                "src/test/resources/valid/CRFTypes/CRFPredicateTypes.bt",
                outputDir.toString()
        });

        assertGeneratedOutputMatchesGolden(outputDir, "src/test/resources/golden/predicateTypes");
    }

    @Test
    void actionTypeGeneratorMatchesGolden(@TempDir Path outputDir) throws IOException {
        CSharpActionGenerator.main(new String[]{
                "src/test/resources/valid/CRFTypes/LiveMatActionTypes.bt",
                outputDir.toString()
        });

        assertGeneratedOutputMatchesGolden(outputDir, "src/test/resources/golden/actionTypes");
    }

    private static void assertGeneratedOutputMatchesGolden(Path actualDir, String goldenDirPath) throws IOException {
        File goldenDir = new File(goldenDirPath);
        File[] goldenFiles = goldenDir.listFiles((dir, name) -> name.endsWith(".cs"));
        assertNotNull(goldenFiles, "golden directory should exist: " + goldenDirPath);
        assertEqualFileSets(actualDir, goldenFiles);

        for (File golden : goldenFiles) {
            Path actualFile = actualDir.resolve(golden.getName());
            String expected = Files.readString(golden.toPath(), StandardCharsets.UTF_8);
            String actual = Files.readString(actualFile, StandardCharsets.UTF_8);
            assertEquals(expected, actual, golden.getName() + " should match the golden output exactly");
        }
    }

    private static void assertEqualFileSets(Path actualDir, File[] goldenFiles) throws IOException {
        String[] actualNames;
        try (var stream = Files.list(actualDir)) {
            actualNames = stream.map(p -> p.getFileName().toString()).sorted().toArray(String[]::new);
        }
        String[] goldenNames = Arrays.stream(goldenFiles).map(File::getName).sorted().toArray(String[]::new);
        Arrays.sort(goldenNames, Comparator.naturalOrder());

        assertEquals(Arrays.toString(goldenNames), Arrays.toString(actualNames),
                "generated file set should match the golden file set exactly");
    }
}

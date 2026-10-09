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
 * The three C# generators match checked-in golden output for a known-good input.
 *
 * The generators' default output paths point into the real
 * APTreeExecutionEngine source tree, so always pass a @TempDir as output.
 * CRFPredicateTypes.bt is used instead of the generator's default input
 * LiveMatPredicaetTypes.bt, which doesn't parse (see CRFTypesParserTest).
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

import domaintypescon._parser.DomainTypesConParser;
import domaintypescon._ast.ASTWorld;
import domaintypescon._symboltable.DomainTypesConArtifactScope;
import domaintypescon._symboltable.IDomainTypesConArtifactScope;
import domaintypescon._symboltable.DomainTypesConSymbols2Json;
import domaintypesdef._symboltable.ElementSymbol;
import domaintypescon.DomainTypesConMill;
import de.se_rwth.commons.logging.Log;

import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Collection;
import java.util.Optional;

/**
 * InstanceSymbolsGenerator
 *
 * Generates one .sym file per concrete Element (e.g., Beam/Plate/Robot/FirstPosition)
 * from CRFConcreteInstances.bt so cross-file name resolution can autoload them.
 *
 * Usage:
 *   - args[0] = input .bt path (default: src/test/resources/valid/CRFConcrete/CRFConcreteInstances.bt)
 *   - args[1] = output dir for .sym files (default: target/symbols)
 */
public class InstanceSymbolsGenerator {

    private static final String DEFAULT_INPUT = "src/test/resources/valid/CRFConcrete/CRFConcreteInstances.bt";
    private static final String DEFAULT_OUTDIR = "target/symbols";

    public static void main(String[] args) {
        String input = args.length > 0 ? args[0] : DEFAULT_INPUT;
        String outDir = args.length > 1 ? args[1] : DEFAULT_OUTDIR;

        try {
            System.out.println("=== INSTANCE SYMBOLS GENERATOR ===");
            System.out.println("Input model: " + input);
            System.out.println("Output dir:  " + outDir);

            DomainTypesConMill.init();

            // Ensure output directory exists
            Path outPath = Paths.get(outDir);
            Files.createDirectories(outPath);

            // Parse instances model
            ASTWorld world = parseWorld(input);
            if (world == null) {
                System.err.println("✗ Failed to parse instances model. Abort.");
                return;
            }

            // Build initial artifact scope from AST
            IDomainTypesConArtifactScope initial = DomainTypesConMill.scopesGenitorDelegator().createFromAST(world);

            // Collect all Element symbols (covers Beam/Plate/Robot/FirstPosition)
            Collection<ElementSymbol> elements = initial.getElementSymbols().values();
            if (elements.isEmpty()) {
                System.out.println("⚠ No Element symbols found to serialize.");
            }

            // Serialize each Element into its own artifact scope named <symbol>.sym
            int count = 0;
            for (ElementSymbol el : elements) {
                String name = el.getName();
                DomainTypesConArtifactScope single = new DomainTypesConArtifactScope();
                single.setName(name); // ensures loader searches <name>.sym
                single.add(el);       // add as ElementSymbol

                String json = new DomainTypesConSymbols2Json().serialize(single);
                Path symPath = outPath.resolve(name + ".sym");
                try (FileWriter fw = new FileWriter(symPath.toFile())) {
                    fw.write(json);
                }
                count++;
            }

            System.out.println("✓ Wrote " + count + " symbol file(s) to: " + outPath.toAbsolutePath());

        } catch (Exception e) {
            System.err.println("✗ ERROR: " + e.getMessage());
            e.printStackTrace();
        }
    }

    private static ASTWorld parseWorld(String modelPath) throws IOException {
        File modelFile = new File(modelPath);
        if (!modelFile.exists()) {
            System.err.println("✗ Model not found: " + modelPath);
            System.err.println("  CWD: " + System.getProperty("user.dir"));
            return null;
        }
        DomainTypesConParser parser = new DomainTypesConParser();
        Optional<ASTWorld> res = parser.parse(modelPath);
        if (res.isEmpty()) {
            Log.getFindings().forEach(f -> System.err.println("  " + f.buildMsg()));
            return null;
        }
        return res.get();
    }
}

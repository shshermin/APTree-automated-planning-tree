package CoCos.PlanningService;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Optional;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public final class PlanningDomainCatalog {

  private static final Path PDDL_DIRECTORY = Paths.get("..", "APTreeExecutionEngine", "python_service",
      "Plannerinputs", "static");
  private static final Path DECLARATIONS_FILE = Paths.get("src", "test", "resources", "valid", "Planners",
      "PDDLPlanner.bt");
  private static final Pattern PDDL_21_FEATURE = Pattern.compile(
      ":(?:fluents|numeric-fluents|durative-actions|duration-inequalities|continuous-effects)\\b",
      Pattern.CASE_INSENSITIVE);

  private PlanningDomainCatalog() {
  }

  public static Optional<DomainMetadata> resolve(String domainName) {
    Path domainFile = PDDL_DIRECTORY.resolve(domainName + ".pddl");
    if (Files.isRegularFile(domainFile)) {
      return readPddlMetadata(domainName, domainFile);
    }
    return readDeclaredMetadata(domainName);
  }

  private static Optional<DomainMetadata> readPddlMetadata(String domainName, Path domainFile) {
    try {
      String content = new String(Files.readAllBytes(domainFile), StandardCharsets.UTF_8);
      double languageVersion = PDDL_21_FEATURE.matcher(content).find() ? 2.1 : 1.2;
      return Optional.of(new DomainMetadata(domainName, languageVersion, domainFile));
    } catch (IOException ignored) {
      return Optional.empty();
    }
  }

  private static Optional<DomainMetadata> readDeclaredMetadata(String domainName) {
    if (!Files.isRegularFile(DECLARATIONS_FILE)) {
      return Optional.empty();
    }
    try {
      String content = new String(Files.readAllBytes(DECLARATIONS_FILE), StandardCharsets.UTF_8);
      Pattern declaration = Pattern.compile("\\bDomain\\s+" + Pattern.quote(domainName)
          + "\\s+LanguageVersion\\s*:\\s*(\\d+(?:\\.\\d+)?)\\b");
      Matcher matcher = declaration.matcher(content);
      if (matcher.find()) {
        return Optional.of(new DomainMetadata(domainName, Double.parseDouble(matcher.group(1)), DECLARATIONS_FILE));
      }
    } catch (IOException ignored) {
      return Optional.empty();
    }
    return Optional.empty();
  }

  public static final class DomainMetadata {
    private final String name;
    private final double languageVersion;
    private final Path source;

    private DomainMetadata(String name, double languageVersion, Path source) {
      this.name = name;
      this.languageVersion = languageVersion;
      this.source = source;
    }

    public String getName() {
      return name;
    }

    public double getLanguageVersion() {
      return languageVersion;
    }

    public Path getSource() {
      return source;
    }
  }
}
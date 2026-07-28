package CoCos.PlanningService;

import java.util.Arrays;
import java.util.Collections;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.Locale;
import java.util.Map;
import java.util.Optional;
import java.util.Set;

public final class PlannerCapabilityRegistry {

  private static final Map<String, PlannerCapabilities> PLANNERS = new LinkedHashMap<>();

  static {
    register("ENHSP", Double.POSITIVE_INFINITY,
        "sat-hadd", "sat-hmax", "sat-blind",
        "opt-hadd", "opt-hmax", "opt-blind");
    register("FF", 1.2);
  }

  private PlannerCapabilityRegistry() {
  }

  public static void register(String plannerName, double maximumPddlVersion, String... configs) {
    PLANNERS.put(normalize(plannerName),
        new PlannerCapabilities(maximumPddlVersion, new LinkedHashSet<>(Arrays.asList(configs))));
  }

  public static Optional<PlannerCapabilities> find(String plannerName) {
    return Optional.ofNullable(PLANNERS.get(normalize(plannerName)));
  }

  public static void registerConfigs(String plannerName, String... configs) {
    String normalizedName = normalize(plannerName);
    PlannerCapabilities current = PLANNERS.get(normalizedName);
    if (current == null) {
      throw new IllegalArgumentException("Unknown planner: " + plannerName);
    }
    Set<String> mergedConfigs = new LinkedHashSet<>(current.getConfigs());
    mergedConfigs.addAll(Arrays.asList(configs));
    PLANNERS.put(normalizedName, new PlannerCapabilities(current.getMaximumPddlVersion(), mergedConfigs));
  }

  private static String normalize(String value) {
    return value.toUpperCase(Locale.ROOT);
  }

  public static final class PlannerCapabilities {
    private final double maximumPddlVersion;
    private final Set<String> configs;

    private PlannerCapabilities(double maximumPddlVersion, Set<String> configs) {
      this.maximumPddlVersion = maximumPddlVersion;
      this.configs = Collections.unmodifiableSet(configs);
    }

    public double getMaximumPddlVersion() {
      return maximumPddlVersion;
    }

    public Set<String> getConfigs() {
      return configs;
    }

    public boolean supportsConfig(String config) {
      return configs.stream().anyMatch(candidate -> candidate.equalsIgnoreCase(config));
    }
  }
}
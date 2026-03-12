#ifndef PREDICATE_GENERATOR_H
#define PREDICATE_GENERATOR_H

#include <string>
#include <vector>
#include <utility>
#include <fstream>
#include <sstream>
#include <iostream>
#include <regex>

// Parsed element from a properties .bt file (Stick or Cube line)
struct ElementEntry {
    std::string type;       // "Stick" or "Cube"
    std::string name;       // e.g. "Stick1"
    std::string initLoc;    // e.g. "InitLocStick1"
    std::string finalLoc;   // e.g. "FinalLocStick1"
};

// Parsed robot from a properties .bt file
struct RobotEntry {
    std::string name;       // e.g. "robot1"
    std::string tool;       // e.g. "gripper1"
    bool hasTool;           // e.g. True/False
    std::string loc;        // e.g. "homePos"
};

// Parsed tool from a properties .bt file (Gripper, StaplerGun, etc.)
struct ToolEntry {
    std::string type;       // e.g. "Gripper" or "StaplerGun"
    std::string name;       // e.g. "gripper1"
    std::string loc;        // e.g. "equipLocGripper"
};

// Generates DSL predicate instances from a DemonstratorProperties.bt file.
// Output format matches the CRFConcrete predicate syntax, e.g.:
//   AtPlace(Stick1 InitLocStick1)
//   GripperEmpty(robot1)
//   ObjectFinalPosition(Stick1 FinalLocStick1)
class PredicateGenerator {
public:
    // Construct by parsing a properties .bt model file.
    // propertiesPath: path to a DemonstratorProperties.bt (or similar)
    explicit PredicateGenerator(const std::string& propertiesPath);

    // Generate AtPlace predicates: AtPlace(elementName initLocName)
    // One per element, using its initial location.
    void addAtPlacePredicates();

    // Generate GripperEmpty predicates: GripperEmpty(robotName)
    // One per robot found in the file.
    void addGripperEmptyPredicates();

    // Generate ObjectFinalPosition predicates: ObjectFinalPosition(elementName finalLocName)
    // One per element, using its final location.
    void addObjectFinalPositionPredicates();

    // Holding predicates are only added when the robot IS holding something.
    // By default (initial state), nothing is emitted.
    void addHoldingPredicates();

    // Explicitly add a single Holding predicate: Holding(robotName elementName)
    void addHoldingPredicate(const std::string& robotName, const std::string& elementName);

    // Add Stacked predicates from spatial analysis results.
    // Each pair is (topName, bottomName) -> PredicateName(topName bottomName)
    // If predicateName is empty or omitted, defaults to "Stacked".
    // Pass e.g. "Nailed" to emit Nailed(obj1 obj2) instead.
    void addStackedPredicates(const std::vector<std::pair<std::string, std::string>>& stackedPairs,
                              const std::string& predicateName = "Stacked");

    // Add AtPlace predicates using FINAL locations (for goal state).
    void addAtPlaceFinalPredicates();

    // Add AtFinalPosition predicates: AtFinalPosition(elementName)
    void addAtFinalPositionPredicates();

    // Add Fixed predicates: Fixed(elementName) for every element.
    void addFixedPredicates();

    // Add AtAgent predicates: AtAgent(robotName robotLoc)
    void addAtAgentPredicates();

    // Add AtTool predicates: AtTool(toolName toolLoc)
    void addAtToolPredicates();

    // Add HasTool predicates: HasTool(robotName toolName)
    // Only generated if the robot has a tool (hasTool == true).
    void addHasToolPredicates();

    // Add RobotEquipped predicates: RobotEquipped(robotName)
    // Only generated if the robot has a tool.
    void addRobotEquippedPredicates();

    // Add ActiveTool predicates: ActiveTool(toolName)
    // Only generated for tools that a robot currently has.
    void addActiveToolPredicates();

    // Run all predicate generators and write to outputPath.
    void generateAll(const std::string& outputPath);

    // Get all collected predicates.
    const std::vector<std::string>& getPredicates() const;

    // Write all predicates to a .bt file (appends to existing content).
    void writeToFile(const std::string& outputPath) const;

    // Overwrite the file with only the generated predicates.
    void writeToFileOverwrite(const std::string& outputPath) const;

private:
    // Parse the properties file and populate elements_ and robots_.
    void parsePropertiesFile(const std::string& path);

    std::vector<ElementEntry> elements_;
    std::vector<RobotEntry> robots_;
    std::vector<ToolEntry> tools_;
    std::vector<std::string> predicates_;
};

#endif // PREDICATE_GENERATOR_H

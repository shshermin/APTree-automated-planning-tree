#include "PredicateGenerator.h"

PredicateGenerator::PredicateGenerator(const std::string& propertiesPath) {
    parsePropertiesFile(propertiesPath);
}

void PredicateGenerator::parsePropertiesFile(const std::string& path) {
    std::ifstream file(path);
    if (!file.is_open()) {
        throw std::runtime_error("Cannot open properties file: " + path);
    }

    // Regex for element lines: "Stick Stick1 (InitLocStick1 FinalLocStick1)"
    // or "Cube cube1 (initloccube1 finloccube1)"
    std::regex elementRegex(
        R"(^\s*(Stick|Cube)\s+(\S+)\s+\(\s*(\S+)\s+(\S+)\s*\))",
        std::regex::icase
    );

    // Regex for robot lines: "Robot robot1 (gripper1 True homePos)"
    std::regex robotRegex(
        R"(^\s*Robot\s+(\S+)\s+\(\s*(\S+)\s+(True|False)\s+(\S+)\s*\))",
        std::regex::icase
    );

    // Regex for tool lines: "Gripper gripper1 (... equipLocGripper)" or "StaplerGun staplergun1 (... equipLocStapler)"
    // Captures type, name, and the last token (loc) before the closing paren
    std::regex toolRegex(
        R"(^\s*(Gripper|StaplerGun|VacGripper|NailGripper|GlueGun)\s+(\S+)\s+\((.*)\))",
        std::regex::icase
    );

    std::string line;
    while (std::getline(file, line)) {
        // Skip comments and blank lines
        if (line.empty() || line.find("//") == 0) continue;

        std::smatch match;

        if (std::regex_search(line, match, elementRegex)) {
            ElementEntry entry;
            entry.type = match[1].str();
            entry.name = match[2].str();
            entry.initLoc = match[3].str();
            entry.finalLoc = match[4].str();
            elements_.push_back(entry);
        }
        else if (std::regex_search(line, match, robotRegex)) {
            RobotEntry entry;
            entry.name = match[1].str();
            entry.tool = match[2].str();
            std::string hasToolStr = match[3].str();
            entry.hasTool = (hasToolStr == "True" || hasToolStr == "true");
            entry.loc = match[4].str();
            robots_.push_back(entry);
        }
        else if (std::regex_search(line, match, toolRegex)) {
            ToolEntry entry;
            entry.type = match[1].str();
            entry.name = match[2].str();
            // Extract the last token from the parenthesized content as loc
            std::string content = match[3].str();
            std::istringstream iss(content);
            std::string token;
            while (iss >> token) {
                entry.loc = token; // last token wins
            }
            tools_.push_back(entry);
        }
    }

    std::cout << "Parsed " << elements_.size() << " elements, "
              << robots_.size() << " robots, and "
              << tools_.size() << " tools from: " << path << std::endl;
}

void PredicateGenerator::addAtPlacePredicates() {
    predicates_.push_back("// AtPlace predicates (elements at initial locations)");
    for (const auto& elem : elements_) {
        predicates_.push_back("AtPlace(" + elem.name + " " + elem.initLoc + ")");
    }
    predicates_.push_back("");
}

void PredicateGenerator::addGripperEmptyPredicates() {
    predicates_.push_back("// GripperEmpty predicates");
    for (const auto& robot : robots_) {
        predicates_.push_back("GripperEmpty(" + robot.name + ")");
    }
    predicates_.push_back("");
}

void PredicateGenerator::addObjectFinalPositionPredicates() {
    predicates_.push_back("// ObjectFinalPosition predicates");
    for (const auto& elem : elements_) {
        predicates_.push_back("ObjectFinalPosition(" + elem.name + " " + elem.finalLoc + ")");
    }
    predicates_.push_back("");
}

void PredicateGenerator::addHoldingPredicates() {
    // Holding predicates are only added when the robot IS holding something.
    // By default (initial state), the robot holds nothing, so nothing is emitted.
    // To explicitly add a Holding predicate, call addHoldingPredicate(robotName, elemName).
}

void PredicateGenerator::addHoldingPredicate(const std::string& robotName, const std::string& elementName) {
    predicates_.push_back("Holding(" + robotName + " " + elementName + ")");
}

void PredicateGenerator::addStackedPredicates(const std::vector<std::pair<std::string, std::string>>& stackedPairs,
                                               const std::string& predicateName) {
    std::string name = predicateName.empty() ? "Stacked" : predicateName;
    predicates_.push_back("// " + name + " predicates (from spatial analysis)");
    for (const auto& pair : stackedPairs) {
        predicates_.push_back(name + "(" + pair.first + " " + pair.second + ")");
    }
    predicates_.push_back("");
}

void PredicateGenerator::addAtPlaceFinalPredicates() {
    predicates_.push_back("// AtPlace predicates (elements at final locations)");
    for (const auto& elem : elements_) {
        predicates_.push_back("AtPlace(" + elem.name + " " + elem.finalLoc + ")");
    }
    predicates_.push_back("");
}

void PredicateGenerator::addAtFinalPositionPredicates() {
    predicates_.push_back("// AtFinalPosition predicates");
    for (const auto& elem : elements_) {
        predicates_.push_back("AtFinalPosition(" + elem.name + ")");
    }
    predicates_.push_back("");
}

void PredicateGenerator::addFixedPredicates() {
    predicates_.push_back("// Fixed predicates");
    for (const auto& elem : elements_) {
        predicates_.push_back("Fixed(" + elem.name + ")");
    }
    predicates_.push_back("");
}

void PredicateGenerator::addAtAgentPredicates() {
    predicates_.push_back("// AtAgent predicates");
    for (const auto& robot : robots_) {
        if (!robot.loc.empty()) {
            predicates_.push_back("AtAgent(" + robot.name + " " + robot.loc + ")");
        }
    }
    predicates_.push_back("");
}

void PredicateGenerator::addAtToolPredicates() {
    predicates_.push_back("// AtTool predicates");
    for (const auto& tool : tools_) {
        if (!tool.loc.empty()) {
            predicates_.push_back("AtTool(" + tool.name + " " + tool.loc + ")");
        }
    }
    predicates_.push_back("");
}

void PredicateGenerator::addHasToolPredicates() {
    predicates_.push_back("// HasTool predicates");
    for (const auto& robot : robots_) {
        if (robot.hasTool && !robot.tool.empty()) {
            predicates_.push_back("HasTool(" + robot.name + " " + robot.tool + ")");
        }
    }
    predicates_.push_back("");
}

void PredicateGenerator::addRobotEquippedPredicates() {
    predicates_.push_back("// RobotEquipped predicates");
    for (const auto& robot : robots_) {
        if (robot.hasTool && !robot.tool.empty()) {
            predicates_.push_back("RobotEquipped(" + robot.name + ")");
        }
    }
    predicates_.push_back("");
}

void PredicateGenerator::addActiveToolPredicates() {
    predicates_.push_back("// ActiveTool predicates");
    for (const auto& robot : robots_) {
        if (robot.hasTool && !robot.tool.empty()) {
            predicates_.push_back("ActiveTool(" + robot.tool + ")");
        }
    }
    predicates_.push_back("");
}

void PredicateGenerator::generateAll(const std::string& outputPath) {
    addAtPlacePredicates();
    addGripperEmptyPredicates();
    addObjectFinalPositionPredicates();
    addHoldingPredicates();
    addAtAgentPredicates();
    addAtToolPredicates();
    addHasToolPredicates();
    addRobotEquippedPredicates();
    addActiveToolPredicates();
    writeToFile(outputPath);
}

const std::vector<std::string>& PredicateGenerator::getPredicates() const {
    return predicates_;
}

void PredicateGenerator::writeToFile(const std::string& outputPath) const {
    static const std::string START_MARKER = "// === GENERATED PREDICATES (DO NOT EDIT BELOW) ===";
    static const std::string END_MARKER   = "// === END GENERATED PREDICATES ===";

    // Read existing file content
    std::vector<std::string> existingLines;
    {
        std::ifstream in(outputPath);
        if (in.is_open()) {
            std::string line;
            while (std::getline(in, line)) {
                existingLines.push_back(line);
            }
        }
    }

    // Find existing marker positions
    int startIdx = -1, endIdx = -1;
    for (int i = 0; i < (int)existingLines.size(); i++) {
        if (existingLines[i].find(START_MARKER) != std::string::npos) startIdx = i;
        if (existingLines[i].find(END_MARKER) != std::string::npos) endIdx = i;
    }

    std::ofstream out(outputPath);
    if (!out.is_open()) {
        throw std::runtime_error("Cannot open output file for writing: " + outputPath);
    }

    if (startIdx >= 0 && endIdx > startIdx) {
        // Replace the section between markers
        for (int i = 0; i < startIdx; i++) {
            out << existingLines[i] << "\n";
        }
    } else {
        // No markers found — write all existing content, then append
        for (const auto& line : existingLines) {
            out << line << "\n";
        }
        out << "\n";
    }

    // Write the generated block with markers
    out << START_MARKER << "\n";
    for (const auto& pred : predicates_) {
        out << pred << "\n";
    }
    out << END_MARKER << "\n";

    // If markers existed, write everything after the old end marker
    if (startIdx >= 0 && endIdx > startIdx) {
        for (int i = endIdx + 1; i < (int)existingLines.size(); i++) {
            out << existingLines[i] << "\n";
        }
    }

    std::cout << "Wrote " << predicates_.size() << " generated predicate lines to: " << outputPath << std::endl;
}

void PredicateGenerator::writeToFileOverwrite(const std::string& outputPath) const {
    std::ofstream out(outputPath);
    if (!out.is_open()) {
        throw std::runtime_error("Cannot open output file for writing: " + outputPath);
    }

    for (const auto& pred : predicates_) {
        out << pred << std::endl;
    }

    std::cout << "Wrote " << predicates_.size() << " lines to: " << outputPath << std::endl;
}

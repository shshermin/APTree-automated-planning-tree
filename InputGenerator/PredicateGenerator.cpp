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

    // Regex for robot lines: "Robot robot1 (gripper1)"
    std::regex robotRegex(
        R"(^\s*Robot\s+(\S+)\s+\(\s*(\S+)\s*\))",
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
            robots_.push_back(entry);
        }
    }

    std::cout << "Parsed " << elements_.size() << " elements and "
              << robots_.size() << " robots from: " << path << std::endl;
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

void PredicateGenerator::generateAll(const std::string& outputPath) {
    addAtPlacePredicates();
    addGripperEmptyPredicates();
    addObjectFinalPositionPredicates();
    addHoldingPredicates();
    writeToFile(outputPath);
}

const std::vector<std::string>& PredicateGenerator::getPredicates() const {
    return predicates_;
}

void PredicateGenerator::writeToFile(const std::string& outputPath) const {
    std::ofstream out(outputPath, std::ios::app);
    if (!out.is_open()) {
        throw std::runtime_error("Cannot open output file for appending: " + outputPath);
    }

    out << std::endl;
    for (const auto& pred : predicates_) {
        out << pred << std::endl;
    }

    std::cout << "Appended " << predicates_.size() << " lines to: " << outputPath << std::endl;
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

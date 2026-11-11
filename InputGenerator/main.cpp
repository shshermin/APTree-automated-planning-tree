#include <iostream>
#include "spatial_predicates.h"
#include "PDDLPredicateGenerator.h"
#include "PredicateGenerator.h"

static const char* USAGE =
    "Usage: InputGenerator [scene.obj] [properties.bt] [init-state.bt] [goal-state.bt]\n"
    "                      [--unit mm|cm|m] [--up x|y|z]\n"
    "  --unit  unit the OBJ coordinates are in (default mm)\n"
    "  --up    axis that points up in the OBJ (default y)\n";

int main(int argc, char* argv[]) {
    // Options can appear anywhere; everything else is positional.
    LengthUnit unit = LengthUnit::Millimeter;
    UpAxis up = UpAxis::Y;
    std::vector<std::string> positional;
    for (int i = 1; i < argc; ++i) {
        std::string arg = argv[i];
        if (arg == "--unit" && i + 1 < argc) {
            if (!parseLengthUnit(argv[++i], unit)) { std::cerr << "Unknown unit: " << argv[i] << "\n" << USAGE; return 2; }
        } else if (arg == "--up" && i + 1 < argc) {
            if (!parseUpAxis(argv[++i], up)) { std::cerr << "Unknown up axis: " << argv[i] << "\n" << USAGE; return 2; }
        } else if (arg.rfind("--", 0) == 0) {
            std::cerr << "Unknown option: " << arg << "\n" << USAGE;
            return 2;
        } else {
            positional.push_back(arg);
        }
    }
    const char* axisName = up == UpAxis::X ? "X-up" : up == UpAxis::Z ? "Z-up" : "Y-up";

    // Default file path
    std::string filepath = positional.size() > 0 ? positional[0] : "test meshes.obj";

    std::cout << "Loading: " << filepath << " (unit: " << (unit == LengthUnit::Meter ? "m" : unit == LengthUnit::Centimeter ? "cm" : "mm")
              << ", " << axisName << ")" << std::endl;

    // Load all named objects from the OBJ file
    std::vector<SpatialObject> scene = loadMultiObjectOBJ(filepath);

    std::cout << "Found " << scene.size() << " objects:" << std::endl;
    std::cout << std::endl;

    // Print info for each object
    std::cout << "=== Scene Objects ===" << std::endl;
    for (const auto& obj : scene) {
        std::cout << "  " << obj.name
                  << " | vertices: " << obj.mesh.number_of_vertices()
                  << " | faces: " << obj.mesh.number_of_faces()
                  << " | Y range: [" << obj.bbox.ymin() << ", " << obj.bbox.ymax() << "]"
                  << std::endl;
    }

    std::cout << std::endl << "=== IsObjectClear (" << axisName << ") ===" << std::endl;
    for (const auto& obj : scene) {
        bool clear = IsObjectClear(obj, scene, up);
        std::cout << "  " << obj.name << ": "
                  << (clear ? "CLEAR" : "NOT CLEAR (something on top)")
                  << std::endl;
    }

    // Test Stacked relationships
    std::cout << std::endl << "=== Stacked Relationships (" << axisName << ") ===" << std::endl;
    auto stacked = FindAllStacked(scene, up, unit);
    for (const auto& pair : stacked) {
        std::cout << "  Stacked(" << pair.first << ", " << pair.second << ")" << std::endl;
    }
    std::cout << "  Total: " << stacked.size() << " stacked pairs" << std::endl;

    // Test Stack Contact Centroids
    std::cout << std::endl << "=== Stack Contact Centroids (" << axisName << ") ===" << std::endl;
    auto contacts = FindAllStackedWithContact(scene, up, unit);
    for (const auto& c : contacts) {
        std::cout << "  " << c.topName << " on " << c.bottomName
                  << " | centroid: (" << c.centroid.x() << ", "
                  << c.centroid.y() << ", " << c.centroid.z() << ")"
                  << " | area: " << c.area << std::endl;
    }
    std::cout << "  Total: " << contacts.size() << " contact surfaces" << std::endl;

    // Write contact centroids to file
    {
        std::ofstream contactFile("StackContactCentroids.csv");
        contactFile << "top,bottom,centroid_x,centroid_y,centroid_z,area" << std::endl;
        for (const auto& c : contacts) {
            contactFile << c.topName << "," << c.bottomName << ","
                        << c.centroid.x() << "," << c.centroid.y() << ","
                        << c.centroid.z() << "," << c.area << std::endl;
        }
        std::cout << std::endl << "Written contact centroids to StackContactCentroids.csv" << std::endl;
    }

    // Generate PDDL predicates
    PDDLPredicateGenerator pddl(scene, up);
    pddl.addClearPredicate();

    std::cout << std::endl << "=== Generated PDDL Predicates ===" << std::endl;
    for (const auto& pred : pddl.getPredicates()) {
        std::cout << "  " << pred << std::endl;
    }

    // Write to file
    pddl.writeToFile("InitialState.pddl");
    std::cout << std::endl << "Written to InitialState.pddl" << std::endl;

    // Generate DSL predicates from DemonstratorProperties.bt
    std::string propertiesPath = "../APTreeDSL/src/test/resources/valid/CRFConcrete/DemonstratorProperties.bt";
    std::string predicatesOutput = "../APTreeDSL/src/test/resources/valid/CRFConcrete/DemonstratorInitState.bt";
    std::string goalStateOutput = "../APTreeDSL/src/test/resources/valid/CRFConcrete/DemonstratorGoalState.bt";

    if (positional.size() > 1) {
        propertiesPath = positional[1];
    }
    if (positional.size() > 2) {
        predicatesOutput = positional[2];
    }
    if (positional.size() > 3) {
        goalStateOutput = positional[3];
    }

    // Generate Init State predicates (append to existing BelongsToLayer content)
    std::cout << std::endl << "=== DSL Init State Generation ===" << std::endl;
    PredicateGenerator initGen(propertiesPath);
    initGen.addAtPlacePredicates();
    initGen.addGripperEmptyPredicates();
    initGen.addObjectFinalPositionPredicates();
    initGen.addAtAgentPredicates();
    initGen.addAtToolPredicates();
    initGen.addHasToolPredicates();
    initGen.addRobotEquippedPredicates();
    initGen.addActiveToolPredicates();
    initGen.writeToFile(predicatesOutput);

    // Generate Goal State: Stacked predicates + AtPlace(final) + AtFinalPosition
    std::cout << std::endl << "=== DSL Goal State Generation ===" << std::endl;
    PredicateGenerator goalGen(propertiesPath);
    goalGen.addStackedPredicates(stacked);
    goalGen.addStackedPredicates(stacked, "Nailed");
    goalGen.addAtPlaceFinalPredicates();
    goalGen.addAtFinalPositionPredicates();
    goalGen.addFixedPredicates();
    goalGen.addGripperEmptyPredicates();
    goalGen.addAtAgentPredicates();
    goalGen.addAtToolPredicates();
    goalGen.addHasToolPredicates();
    goalGen.addRobotEquippedPredicates();
    goalGen.addActiveToolPredicates();
    goalGen.writeToFileOverwrite(goalStateOutput);

    return 0;
}

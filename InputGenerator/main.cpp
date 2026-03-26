#include <iostream>
#include "spatial_predicates.h"
#include "PDDLPredicateGenerator.h"
#include "PredicateGenerator.h"

int main(int argc, char* argv[]) {
    // Default file path
    std::string filepath = "test meshes.obj";
    if (argc > 1) {
        filepath = argv[1];
    }

    std::cout << "Loading: " << filepath << std::endl;

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

    // Test IsObjectClear (Y-up, since Rhino uses Y as vertical axis)
    std::cout << std::endl << "=== IsObjectClear (Y-up) ===" << std::endl;
    for (const auto& obj : scene) {
        bool clear = IsObjectClear(obj, scene, UpAxis::Y);
        std::cout << "  " << obj.name << ": "
                  << (clear ? "CLEAR" : "NOT CLEAR (something on top)")
                  << std::endl;
    }

    // Test Stacked relationships
    std::cout << std::endl << "=== Stacked Relationships (Y-up) ===" << std::endl;
    auto stacked = FindAllStacked(scene, UpAxis::Y);
    for (const auto& pair : stacked) {
        std::cout << "  Stacked(" << pair.first << ", " << pair.second << ")" << std::endl;
    }
    std::cout << "  Total: " << stacked.size() << " stacked pairs" << std::endl;

    // Test Stack Contact Centroids
    std::cout << std::endl << "=== Stack Contact Centroids (Y-up) ===" << std::endl;
    auto contacts = FindAllStackedWithContact(scene, UpAxis::Y);
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
    PDDLPredicateGenerator pddl(scene, UpAxis::Y);
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

    if (argc > 2) {
        propertiesPath = argv[2];
    }
    if (argc > 3) {
        predicatesOutput = argv[3];
    }
    if (argc > 4) {
        goalStateOutput = argv[4];
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

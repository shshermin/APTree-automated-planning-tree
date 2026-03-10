#ifndef PDDL_PREDICATE_GENERATOR_H
#define PDDL_PREDICATE_GENERATOR_H

#include <string>
#include <vector>
#include "spatial_predicates.h"

// Generates PDDL predicate strings from spatial analysis of a 3D scene.
class PDDLPredicateGenerator {
public:
    // Construct with a scene (list of objects) and the up-axis convention.
    PDDLPredicateGenerator(const std::vector<SpatialObject>& scene,
                           UpAxis upAxis = UpAxis::Y);

    // Evaluate IsObjectClear for every object in the scene and store
    // "(clear <name>)" predicates for objects that are clear.
    void addClearPredicate();

    // Return all collected predicate strings (e.g. "(clear box1)")
    const std::vector<std::string>& getPredicates() const;

    // Write all collected predicates to a PDDL :init block in the given file.
    void writeToFile(const std::string& filepath) const;

private:
    std::vector<SpatialObject> scene_;
    UpAxis upAxis_;
    std::vector<std::string> predicates_;
};

#endif // PDDL_PREDICATE_GENERATOR_H

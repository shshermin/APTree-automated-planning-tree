#include "PDDLPredicateGenerator.h"
#include <fstream>
#include <iostream>
#include <stdexcept>

PDDLPredicateGenerator::PDDLPredicateGenerator(const std::vector<SpatialObject>& scene,
                                               UpAxis upAxis)
    : scene_(scene), upAxis_(upAxis) {}

void PDDLPredicateGenerator::addClearPredicate() {
    for (const auto& obj : scene_) {
        if (IsObjectClear(obj, scene_, upAxis_)) {
            predicates_.push_back("(clear " + obj.name + ")");
        }
    }
}

const std::vector<std::string>& PDDLPredicateGenerator::getPredicates() const {
    return predicates_;
}

void PDDLPredicateGenerator::writeToFile(const std::string& filepath) const {
    std::ofstream out(filepath);
    if (!out.is_open()) {
        throw std::runtime_error("Cannot open file for writing: " + filepath);
    }

    out << "(:init" << std::endl;
    for (const auto& pred : predicates_) {
        out << "    " << pred << std::endl;
    }
    out << ")" << std::endl;
}

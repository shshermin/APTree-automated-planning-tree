#include <gtest/gtest.h>
#include "spatial_predicates.h"

// Phase 0 smoke test: proves the GoogleTest harness is wired up correctly
// (links against spatial_predicates.cpp + CGAL, runs under `ctest`).
// Real coverage per test-plan section 7 (#64-65) lands in later phases.

TEST(InfrastructureSmokeTest, StackedBoxIsDetected) {
    SpatialObject bottom = makeBox("bottom", 0, 0, 0, 10, 10, 10);
    SpatialObject top = makeBox("top", 0, 0, 10, 10, 10, 20);
    std::vector<SpatialObject> scene = {bottom, top};

    EXPECT_TRUE(IsStackedOn(top, bottom, scene, UpAxis::Z));
}

TEST(InfrastructureSmokeTest, SeparateBoxesAreNotStacked) {
    SpatialObject a = makeBox("a", 0, 0, 0, 10, 10, 10);
    SpatialObject b = makeBox("b", 100, 100, 100, 110, 110, 110);
    std::vector<SpatialObject> scene = {a, b};

    EXPECT_FALSE(IsStackedOn(b, a, scene, UpAxis::Z));
}

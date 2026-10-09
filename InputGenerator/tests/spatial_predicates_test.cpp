#include <gtest/gtest.h>
#include "spatial_predicates.h"

// Checks the GoogleTest harness links against spatial_predicates.cpp and CGAL.

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

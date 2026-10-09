#include <gtest/gtest.h>
#include <algorithm>
#include <cmath>
#include "spatial_predicates.h"

// Bounding-box overlap and "stacked / on top" contact logic in spatial_predicates.cpp.
// Boxes are built with makeBox(name, xmin,ymin,zmin, xmax,ymax,zmax).

namespace {

using Scene = std::vector<SpatialObject>;

Scene sceneOf(std::initializer_list<SpatialObject> objs) { return Scene(objs); }

bool contains(const std::vector<std::pair<std::string, std::string>>& v, const std::string& top, const std::string& bottom) {
    return std::find(v.begin(), v.end(), std::make_pair(top, bottom)) != v.end();
}

}  // namespace

// ---- Bounding box -----------------------------------------------------------

TEST(Bbox, IsComputedFromTheMeshVertices) {
    SpatialObject b = makeBox("b", 1, 2, 3, 4, 6, 9);

    EXPECT_DOUBLE_EQ(b.bbox.xmin(), 1);  EXPECT_DOUBLE_EQ(b.bbox.xmax(), 4);
    EXPECT_DOUBLE_EQ(b.bbox.ymin(), 2);  EXPECT_DOUBLE_EQ(b.bbox.ymax(), 6);
    EXPECT_DOUBLE_EQ(b.bbox.zmin(), 3);  EXPECT_DOUBLE_EQ(b.bbox.zmax(), 9);
}

// ---- IsObjectClear: footprint overlap ---------------------------------------

TEST(IsObjectClear, AnObjectAloneInTheSceneIsClear) {
    auto a = makeBox("a", 0, 0, 0, 10, 10, 10);
    EXPECT_TRUE(IsObjectClear(a, sceneOf({a}), UpAxis::Z));
}

TEST(IsObjectClear, AnObjectDirectlyOnTopMakesTheTargetNotClear) {
    auto bottom = makeBox("bottom", 0, 0, 0, 10, 10, 10);
    auto top = makeBox("top", 0, 0, 10, 10, 10, 20);
    Scene scene = sceneOf({bottom, top});

    EXPECT_FALSE(IsObjectClear(bottom, scene, UpAxis::Z));
    EXPECT_TRUE(IsObjectClear(top, scene, UpAxis::Z));
}

TEST(IsObjectClear, AnObjectBelowDoesNotCount) {
    auto low = makeBox("low", 0, 0, 0, 10, 10, 10);
    auto high = makeBox("high", 0, 0, 10, 10, 10, 20);

    EXPECT_TRUE(IsObjectClear(high, sceneOf({low, high}), UpAxis::Z));
}

TEST(IsObjectClear, AboveButWithADisjointFootprintIsStillClear) {
    auto a = makeBox("a", 0, 0, 0, 10, 10, 10);
    auto elsewhere = makeBox("elsewhere", 50, 50, 10, 60, 60, 20);

    EXPECT_TRUE(IsObjectClear(a, sceneOf({a, elsewhere}), UpAxis::Z));
}

TEST(IsObjectClear, FootprintsThatOnlyShareAnEdgeDoNotOverlap) {
    auto a = makeBox("a", 0, 0, 0, 10, 10, 10);
    auto edgeNeighbour = makeBox("n", 10, 0, 10, 20, 10, 20);  // touches a's x=10 edge only

    EXPECT_TRUE(IsObjectClear(a, sceneOf({a, edgeNeighbour}), UpAxis::Z));
}

TEST(IsObjectClear, PartialFootprintOverlapCounts) {
    auto a = makeBox("a", 0, 0, 0, 10, 10, 10);
    auto overhang = makeBox("o", 9, 9, 10, 30, 30, 20);

    EXPECT_FALSE(IsObjectClear(a, sceneOf({a, overhang}), UpAxis::Z));
}

TEST(IsObjectClear, AnObjectHigherUpCountsEvenWithAGapBelowIt) {
    // "Above" only means min >= target's max; the object does not need to touch.
    // Something floating 500 units over the target still makes it "not clear".
    auto a = makeBox("a", 0, 0, 0, 10, 10, 10);
    auto floating = makeBox("f", 0, 0, 500, 10, 10, 510);

    EXPECT_FALSE(IsObjectClear(a, sceneOf({a, floating}), UpAxis::Z));
}

class UpAxisScenes : public ::testing::TestWithParam<UpAxis> {};

TEST_P(UpAxisScenes, ClearAndStackedAgreeForEveryUpAxis) {
    // The same physical stack, expressed with each axis as "up".
    UpAxis up = GetParam();
    auto make = [&](const std::string& n, double base, double h) {
        switch (up) {
            case UpAxis::X: return makeBox(n, base, 0, 0, base + h, 10, 10);
            case UpAxis::Y: return makeBox(n, 0, base, 0, 10, base + h, 10);
            default:        return makeBox(n, 0, 0, base, 10, 10, base + h);
        }
    };
    auto bottom = make("bottom", 0, 10);
    auto top = make("top", 10, 10);
    Scene scene = sceneOf({bottom, top});

    EXPECT_FALSE(IsObjectClear(bottom, scene, up));
    EXPECT_TRUE(IsObjectClear(top, scene, up));
    EXPECT_TRUE(IsStackedOn(top, bottom, scene, up));
    EXPECT_FALSE(IsStackedOn(bottom, top, scene, up));
}

INSTANTIATE_TEST_SUITE_P(AllAxes, UpAxisScenes, ::testing::Values(UpAxis::X, UpAxis::Y, UpAxis::Z));

TEST(IsObjectClear, TheWrongUpAxisGivesTheWrongAnswer) {
    // The API defaults to Y-up (main.cpp too: "Rhino uses Y as vertical"),
    // while the Grasshopper script uses world Z. A Z-up scene evaluated with
    // the default axis is silently misjudged: the top box is not "above" in Y.
    auto bottom = makeBox("bottom", 0, 0, 0, 10, 10, 10);
    auto top = makeBox("top", 0, 0, 10, 10, 10, 20);
    Scene scene = sceneOf({bottom, top});

    EXPECT_FALSE(IsObjectClear(bottom, scene, UpAxis::Z));  // correct for a Z-up scene
    EXPECT_TRUE(IsObjectClear(bottom, scene));              // default Y-up: wrongly "clear"
}

TEST(IsObjectClear, ObjectsSharingANameAreInvisibleToEachOther) {
    // The scene is matched by name, not identity: a same-named object above the
    // target is skipped. (OBJ names with spaces are truncated at the first space
    // by the loader, so "Cube 1" and "Cube 2" both become "Cube" - see obj_loader_test.)
    auto bottom = makeBox("cube", 0, 0, 0, 10, 10, 10);
    auto top = makeBox("cube", 0, 0, 10, 10, 10, 20);

    EXPECT_TRUE(IsObjectClear(bottom, sceneOf({bottom, top}), UpAxis::Z));
    EXPECT_FALSE(IsStackedOn(top, bottom, sceneOf({bottom, top}), UpAxis::Z));
}

TEST(IsObjectClear, AnEmptyMeshObjectNeverBlocksAnything) {
    // A default CGAL::Bbox_3 is inverted-infinite, not a point at the origin, so
    // an object whose mesh failed to load overlaps nothing and is silently ignored.
    auto spanningOrigin = makeBox("real", -5, -5, -10, 5, 5, 0);
    SpatialObject ghost;
    ghost.name = "ghost";

    EXPECT_TRUE(std::isinf(ghost.bbox.xmin()));
    EXPECT_TRUE(IsObjectClear(spanningOrigin, sceneOf({spanningOrigin, ghost}), UpAxis::Z));
}

// ---- IsStackedOn: contact tolerance ------------------------------------------

TEST(IsStackedOn, ExactContactIsStacked) {
    auto bottom = makeBox("bottom", 0, 0, 0, 10, 10, 10);
    auto top = makeBox("top", 0, 0, 10, 10, 10, 20);

    EXPECT_TRUE(IsStackedOn(top, bottom, sceneOf({bottom, top}), UpAxis::Z));
}

TEST(IsStackedOn, ItIsNotSymmetric) {
    auto bottom = makeBox("bottom", 0, 0, 0, 10, 10, 10);
    auto top = makeBox("top", 0, 0, 10, 10, 10, 20);

    EXPECT_FALSE(IsStackedOn(bottom, top, sceneOf({bottom, top}), UpAxis::Z));
}

TEST(IsStackedOn, AGapWithinTheToleranceStillCountsAsContact) {
    auto bottom = makeBox("bottom", 0, 0, 0, 10, 10, 10);
    auto nearlyTouching = makeBox("top", 0, 0, 10.9, 10, 10, 20.9);

    EXPECT_TRUE(IsStackedOn(nearlyTouching, bottom, sceneOf({bottom, nearlyTouching}), UpAxis::Z));
}

TEST(IsStackedOn, AGapBeyondTheToleranceIsNotStacked) {
    auto bottom = makeBox("bottom", 0, 0, 0, 10, 10, 10);
    auto floating = makeBox("top", 0, 0, 11.1, 10, 10, 21.1);

    EXPECT_FALSE(IsStackedOn(floating, bottom, sceneOf({bottom, floating}), UpAxis::Z));
}

TEST(IsStackedOn, OverlapBelowTheBottomsTopFaceWithinToleranceAlsoCounts) {
    auto bottom = makeBox("bottom", 0, 0, 0, 10, 10, 10);
    auto sunkIn = makeBox("top", 0, 0, 9.1, 10, 10, 19.1);  // penetrates by 0.9

    EXPECT_TRUE(IsStackedOn(sunkIn, bottom, sceneOf({bottom, sunkIn}), UpAxis::Z));
}

TEST(IsStackedOn, ToleranceIsAnAbsolute1_0InSceneUnitsNotAFractionOfObjectSize) {
    // Known issue: EPSILON is a hard-coded 1.0 with no stated unit (IsObjectClear
    // uses 1e-6, the Grasshopper StackDetector 0.001 m). In a metre-scale scene,
    // boxes with a 90 cm gap count as stacked.
    auto bottom = makeBox("bottom", 0, 0, 0, 0.05, 0.05, 0.05);
    auto ninetyCmAbove = makeBox("top", 0, 0, 0.95, 0.05, 0.05, 1.0);

    EXPECT_TRUE(IsStackedOn(ninetyCmAbove, bottom, sceneOf({bottom, ninetyCmAbove}), UpAxis::Z));
}

TEST(IsStackedOn, ItselfAndEmptyMeshesAreNeverStacked) {
    auto a = makeBox("a", 0, 0, 0, 10, 10, 10);
    SpatialObject ghost;
    ghost.name = "ghost";

    EXPECT_FALSE(IsStackedOn(a, a, sceneOf({a}), UpAxis::Z));
    EXPECT_FALSE(IsStackedOn(ghost, a, sceneOf({a, ghost}), UpAxis::Z));
    EXPECT_FALSE(IsStackedOn(a, ghost, sceneOf({a, ghost}), UpAxis::Z));
}

TEST(IsStackedOn, FootprintsThatOnlyShareAnEdgeAreNotStacked) {
    auto bottom = makeBox("bottom", 0, 0, 0, 10, 10, 10);
    auto corner = makeBox("top", 10, 0, 10, 20, 10, 20);

    EXPECT_FALSE(IsStackedOn(corner, bottom, sceneOf({bottom, corner}), UpAxis::Z));
}

TEST(IsStackedOn, PartialOverlapIsEnough) {
    auto bottom = makeBox("bottom", 0, 0, 0, 10, 10, 10);
    auto offset = makeBox("top", 8, 8, 10, 18, 18, 20);

    EXPECT_TRUE(IsStackedOn(offset, bottom, sceneOf({bottom, offset}), UpAxis::Z));
}

// ---- FindAllStacked -----------------------------------------------------------

TEST(FindAllStacked, ATowerYieldsOnlyAdjacentPairs) {
    auto a = makeBox("a", 0, 0, 0, 10, 10, 10);
    auto b = makeBox("b", 0, 0, 10, 10, 10, 20);
    auto c = makeBox("c", 0, 0, 20, 10, 10, 30);

    auto pairs = FindAllStacked(sceneOf({a, b, c}), UpAxis::Z);

    EXPECT_EQ(pairs.size(), 2u);
    EXPECT_TRUE(contains(pairs, "b", "a"));
    EXPECT_TRUE(contains(pairs, "c", "b"));
    EXPECT_FALSE(contains(pairs, "c", "a"));
}

TEST(FindAllStacked, OneBoxOnTwoSupportsIsStackedOnBoth) {
    auto left = makeBox("left", 0, 0, 0, 10, 10, 10);
    auto right = makeBox("right", 20, 0, 0, 30, 10, 10);
    auto bridge = makeBox("bridge", 5, 0, 10, 25, 10, 20);

    auto pairs = FindAllStacked(sceneOf({left, right, bridge}), UpAxis::Z);

    EXPECT_EQ(pairs.size(), 2u);
    EXPECT_TRUE(contains(pairs, "bridge", "left"));
    EXPECT_TRUE(contains(pairs, "bridge", "right"));
}

TEST(FindAllStacked, AnEmptySceneHasNoPairs) {
    EXPECT_TRUE(FindAllStacked({}, UpAxis::Z).empty());
}

// ---- Contact centroid ----------------------------------------------------------

TEST(ComputeStackContactCentroid, ReportsTheSharedFootprintsCenterAndArea) {
    auto bottom = makeBox("bottom", 0, 0, 0, 10, 10, 10);
    auto top = makeBox("top", 5, 0, 10, 15, 10, 20);  // overlaps x in [5,10]
    StackContactInfo info;

    ASSERT_TRUE(ComputeStackContactCentroid(top, bottom, sceneOf({bottom, top}), info, UpAxis::Z));

    EXPECT_EQ(info.topName, "top");
    EXPECT_EQ(info.bottomName, "bottom");
    EXPECT_DOUBLE_EQ(info.centroid.x(), 7.5);
    EXPECT_DOUBLE_EQ(info.centroid.y(), 5.0);
    EXPECT_DOUBLE_EQ(info.centroid.z(), 10.0);
    EXPECT_DOUBLE_EQ(info.area, 50.0);
}

TEST(ComputeStackContactCentroid, ContactHeightIsTheMidpointOfAToleratedGap) {
    auto bottom = makeBox("bottom", 0, 0, 0, 10, 10, 10);
    auto top = makeBox("top", 0, 0, 10.6, 10, 10, 20.6);
    StackContactInfo info;

    ASSERT_TRUE(ComputeStackContactCentroid(top, bottom, sceneOf({bottom, top}), info, UpAxis::Z));

    EXPECT_DOUBLE_EQ(info.centroid.z(), 10.3);
}

TEST(ComputeStackContactCentroid, YUpUsesTheXZPlaneForTheFootprint) {
    auto bottom = makeBox("bottom", 0, 0, 0, 10, 10, 10);
    auto top = makeBox("top", 0, 10, 4, 10, 20, 14);  // overlaps z in [4,10]
    StackContactInfo info;

    ASSERT_TRUE(ComputeStackContactCentroid(top, bottom, sceneOf({bottom, top}), info, UpAxis::Y));

    EXPECT_DOUBLE_EQ(info.centroid.x(), 5.0);
    EXPECT_DOUBLE_EQ(info.centroid.y(), 10.0);
    EXPECT_DOUBLE_EQ(info.centroid.z(), 7.0);
    EXPECT_DOUBLE_EQ(info.area, 60.0);
}

TEST(ComputeStackContactCentroid, ReturnsFalseWhenNotStacked) {
    auto a = makeBox("a", 0, 0, 0, 10, 10, 10);
    auto far = makeBox("far", 0, 0, 100, 10, 10, 110);
    StackContactInfo info;

    EXPECT_FALSE(ComputeStackContactCentroid(far, a, sceneOf({a, far}), info, UpAxis::Z));
}

TEST(FindAllStackedWithContact, MatchesFindAllStackedOneToOne) {
    auto a = makeBox("a", 0, 0, 0, 10, 10, 10);
    auto b = makeBox("b", 0, 0, 10, 10, 10, 20);
    auto c = makeBox("c", 0, 0, 20, 10, 10, 30);
    Scene scene = sceneOf({a, b, c});

    EXPECT_EQ(FindAllStackedWithContact(scene, UpAxis::Z).size(), FindAllStacked(scene, UpAxis::Z).size());
}

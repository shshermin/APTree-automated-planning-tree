#include <gtest/gtest.h>
#include <algorithm>
#include "spatial_predicates.h"
#include "test_util.h"

// Test the loadMultiObjectOBJ function, plus the shipped
// "test meshes.obj" scene as a regression for the whole pipeline.

namespace {

// Two unit-ish boxes in one file: indices are global across objects (OBJ rule).
const char* kTwoTriangles = R"(# two separate triangles
o first
v 0 0 0
v 1 0 0
v 0 1 0
f 1 2 3
o second
v 5 5 5
v 6 5 5
v 5 6 5
f 4 5 6
)";

}  // namespace

TEST(LoadMultiObjectOBJ, SplitsObjectsAndResolvesGlobalVertexIndices) {
    TempDir dir;
    auto scene = loadMultiObjectOBJ(dir.write("two.obj", kTwoTriangles));

    ASSERT_EQ(scene.size(), 2u);
    EXPECT_EQ(scene[0].name, "first");
    EXPECT_EQ(scene[1].name, "second");
    EXPECT_EQ(scene[0].mesh.number_of_faces(), 1u);
    EXPECT_DOUBLE_EQ(scene[0].bbox.xmax(), 1.0);
    EXPECT_DOUBLE_EQ(scene[1].bbox.xmin(), 5.0);  // second object's faces used indices 4..6
}

TEST(LoadMultiObjectOBJ, MissingFileThrowsARuntimeErrorNamingThePath) {
    try {
        loadMultiObjectOBJ("/definitely/not/here.obj");
        FAIL() << "expected an exception";
    } catch (const std::runtime_error& e) {
        EXPECT_NE(std::string(e.what()).find("/definitely/not/here.obj"), std::string::npos);
    }
}

TEST(LoadMultiObjectOBJ, AcceptsTextureAndNormalIndicesInFaceTokens) {
    TempDir dir;
    auto scene = loadMultiObjectOBJ(dir.write("vtn.obj",
        "o t\nv 0 0 0\nv 1 0 0\nv 0 1 0\nvt 0 0\nvn 0 0 1\nf 1/1/1 2/1/1 3//1\n"));

    ASSERT_EQ(scene.size(), 1u);
    EXPECT_EQ(scene[0].mesh.number_of_faces(), 1u);
}

TEST(LoadMultiObjectOBJ, TriangulatesQuadsAndLargerPolygons) {
    TempDir dir;
    auto scene = loadMultiObjectOBJ(dir.write("poly.obj",
        "o quad\nv 0 0 0\nv 1 0 0\nv 1 1 0\nv 0 1 0\nf 1 2 3 4\n"
        "o pent\nv 0 0 1\nv 1 0 1\nv 2 1 1\nv 1 2 1\nv 0 1 1\nf 5 6 7 8 9\n"));

    ASSERT_EQ(scene.size(), 2u);
    EXPECT_EQ(scene[0].mesh.number_of_faces(), 2u);  // quad -> 2 triangles
    EXPECT_EQ(scene[1].mesh.number_of_faces(), 3u);  // pentagon -> fan of 3
}

TEST(LoadMultiObjectOBJ, FacesBeforeAnyObjectLineGoIntoAnUnnamedObject) {
    TempDir dir;
    auto scene = loadMultiObjectOBJ(dir.write("anon.obj", "v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n"));

    ASSERT_EQ(scene.size(), 1u);
    EXPECT_EQ(scene[0].name, "unnamed");
}

TEST(LoadMultiObjectOBJ, CommentsAndBlankLinesAreIgnored) {
    TempDir dir;
    auto scene = loadMultiObjectOBJ(dir.write("c.obj", "# hi\n\no a\n# mid\nv 0 0 0\nv 1 0 0\nv 0 1 0\n\nf 1 2 3\n"));

    ASSERT_EQ(scene.size(), 1u);
}

TEST(LoadMultiObjectOBJ, AnObjectWithNoFacesHasAnEmptyMeshAndAnInvertedBbox) {
    TempDir dir;
    auto scene = loadMultiObjectOBJ(dir.write("empty.obj", "o nothing\nv 0 0 0\n"));

    ASSERT_EQ(scene.size(), 1u);
    EXPECT_EQ(scene[0].mesh.number_of_vertices(), 0u);
}

TEST(LoadMultiObjectOBJ, ObjectNamesAreTruncatedAtTheFirstSpace) {
    // Known issue: object names are truncated at the first space, so "Cube 1"
    // and "Cube 2" collide - predicates match by name and the PDDL gets duplicates.
    TempDir dir;
    auto scene = loadMultiObjectOBJ(dir.write("spaces.obj",
        "o Cube 1\nv 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n"
        "o Cube 2\nv 0 0 5\nv 1 0 5\nv 0 1 5\nf 4 5 6\n"));

    ASSERT_EQ(scene.size(), 2u);
    EXPECT_EQ(scene[0].name, "Cube");
    EXPECT_EQ(scene[1].name, "Cube");
}

TEST(LoadMultiObjectOBJ, DuplicateObjectNamesAreNotDeduplicated) {
    TempDir dir;
    auto scene = loadMultiObjectOBJ(dir.write("dup.obj",
        "o box\nv 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\no box\nv 0 0 5\nv 1 0 5\nv 0 1 5\nf 4 5 6\n"));

    ASSERT_EQ(scene.size(), 2u);
    EXPECT_EQ(scene[0].name, scene[1].name);
}

// ---- The shipped scene, as a regression --------------------------------------

TEST(ShippedScene, TestMeshesObjLoadsThreeBoxes) {
    auto scene = loadMultiObjectOBJ(std::string(INPUTGENERATOR_DIR) + "/test meshes.obj");

    ASSERT_EQ(scene.size(), 3u);
    for (const auto& o : scene) {
        EXPECT_EQ(o.mesh.number_of_vertices(), 24u) << o.name;
        EXPECT_EQ(o.mesh.number_of_faces(), 12u) << o.name;
    }
}

TEST(ShippedScene, TestMeshesObjHasExactlyOneStackWhenReadAsYUp) {
    // In this file box3 sits on box2 along Y (y 7.19 = 7.19); box1 is a
    // separate box next to them along Z.
    auto scene = loadMultiObjectOBJ(std::string(INPUTGENERATOR_DIR) + "/test meshes.obj");

    auto pairs = FindAllStacked(scene, UpAxis::Y);

    ASSERT_EQ(pairs.size(), 1u);
    EXPECT_EQ(pairs[0].first, "box3");
    EXPECT_EQ(pairs[0].second, "box2");

    auto clear = [&](const char* n) {
        return IsObjectClear(*std::find_if(scene.begin(), scene.end(), [&](const SpatialObject& o) { return o.name == n; }), scene, UpAxis::Y);
    };
    EXPECT_TRUE(clear("box3"));
    EXPECT_FALSE(clear("box2"));
    EXPECT_TRUE(clear("box1"));
}

TEST(ShippedScene, TheSameFileReadAsZUpFindsNoStackAtAll) {
    // Same geometry, wrong axis convention: no stack is found and box2 is
    // reported "not clear" only because box1 lies further along Z.
    auto scene = loadMultiObjectOBJ(std::string(INPUTGENERATOR_DIR) + "/test meshes.obj");

    EXPECT_TRUE(FindAllStacked(scene, UpAxis::Z).empty());
}

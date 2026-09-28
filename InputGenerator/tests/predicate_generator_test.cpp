#include <gtest/gtest.h>
#include <algorithm>
#include "PDDLPredicateGenerator.h"
#include "PredicateGenerator.h"
#include "test_util.h"

// Test the two text generators that turn a
// scene / properties file into predicates.

namespace {

bool has(const std::vector<std::string>& v, const std::string& s) {
    return std::find(v.begin(), v.end(), s) != v.end();
}

// The format the generator's regexes were written for: locations inline.
const char* kProperties = R"(// comment line
Stick stick1 (InitLocStick1 FinalLocStick1)
cube cube1 (initloccube1 finloccube1)

Robot robot1 (gripper1 True homePos)
Robot robot2 (gripper2 False awayPos)
Gripper gripper1 (True True equipLocGripper)
StaplerGun staplergun1 (False equipLocStapler)
)";

const std::string kShippedProperties =
    std::string(REPO_ROOT_DIR) + "/APTreeDSL/src/test/resources/valid/CRFConcrete/DemonstratorProperties.bt";

}  // namespace

// ---- PredicateGenerator: parsing ------------------------------------------------

TEST(PredicateGenerator, ThrowsWhenThePropertiesFileCannotBeOpened) {
    EXPECT_THROW(PredicateGenerator("/no/such/properties.bt"), std::runtime_error);
}

TEST(PredicateGenerator, ParsesElementsInBothCasesAndEmitsOnePredicatePerElement) {
    TempDir dir;
    PredicateGenerator gen(dir.write("p.bt", kProperties));

    gen.addAtPlacePredicates();
    gen.addObjectFinalPositionPredicates();

    EXPECT_TRUE(has(gen.getPredicates(), "AtPlace(stick1 InitLocStick1)"));
    EXPECT_TRUE(has(gen.getPredicates(), "AtPlace(cube1 initloccube1)"));
    EXPECT_TRUE(has(gen.getPredicates(), "ObjectFinalPosition(stick1 FinalLocStick1)"));
    EXPECT_TRUE(has(gen.getPredicates(), "ObjectFinalPosition(cube1 finloccube1)"));
}

TEST(PredicateGenerator, RobotAndToolPredicatesFollowTheHasToolFlag) {
    TempDir dir;
    PredicateGenerator gen(dir.write("p.bt", kProperties));

    gen.addGripperEmptyPredicates();
    gen.addAtAgentPredicates();
    gen.addAtToolPredicates();
    gen.addHasToolPredicates();
    gen.addRobotEquippedPredicates();
    gen.addActiveToolPredicates();
    const auto& p = gen.getPredicates();

    EXPECT_TRUE(has(p, "GripperEmpty(robot1)"));
    EXPECT_TRUE(has(p, "GripperEmpty(robot2)"));            // regardless of hasTool
    EXPECT_TRUE(has(p, "AtAgent(robot1 homePos)"));
    EXPECT_TRUE(has(p, "AtTool(gripper1 equipLocGripper)"));  // last token of the tool's parens
    EXPECT_TRUE(has(p, "AtTool(staplergun1 equipLocStapler)"));
    EXPECT_TRUE(has(p, "HasTool(robot1 gripper1)"));
    EXPECT_TRUE(has(p, "RobotEquipped(robot1)"));
    EXPECT_TRUE(has(p, "ActiveTool(gripper1)"));
    EXPECT_FALSE(has(p, "HasTool(robot2 gripper2)"));       // hasTool == False
    EXPECT_FALSE(has(p, "RobotEquipped(robot2)"));
    EXPECT_FALSE(has(p, "ActiveTool(gripper2)"));
}

TEST(PredicateGenerator, HoldingIsOnlyEmittedWhenAskedForExplicitly) {
    TempDir dir;
    PredicateGenerator gen(dir.write("p.bt", kProperties));

    gen.addHoldingPredicates();
    EXPECT_TRUE(gen.getPredicates().empty());

    gen.addHoldingPredicate("robot1", "stick1");
    EXPECT_TRUE(has(gen.getPredicates(), "Holding(robot1 stick1)"));
}

TEST(PredicateGenerator, StackedPredicatesUseTheGivenNameOrDefaultToStacked) {
    TempDir dir;
    PredicateGenerator gen(dir.write("p.bt", kProperties));

    gen.addStackedPredicates({{"b", "a"}});
    gen.addStackedPredicates({{"b", "a"}}, "Nailed");
    gen.addStackedPredicates({{"c", "b"}}, "");

    EXPECT_TRUE(has(gen.getPredicates(), "Stacked(b a)"));
    EXPECT_TRUE(has(gen.getPredicates(), "Nailed(b a)"));
    EXPECT_TRUE(has(gen.getPredicates(), "Stacked(c b)"));  // empty name -> "Stacked"
}

TEST(PredicateGenerator, GenerateAllWritesEveryInitSectionToTheOutputFile) {
    TempDir dir;
    PredicateGenerator gen(dir.write("p.bt", kProperties));
    std::string out = dir.file("init.bt");

    gen.generateAll(out);

    std::string text = TempDir::read(out);
    for (const char* expected : {"AtPlace(stick1 InitLocStick1)", "GripperEmpty(robot1)", "AtAgent(robot1 homePos)",
                                 "HasTool(robot1 gripper1)", "ActiveTool(gripper1)"}) {
        EXPECT_NE(text.find(expected), std::string::npos) << expected;
    }
}

// ---- The shipped DemonstratorProperties.bt ---------------------------------------

TEST(PredicateGeneratorOnTheShippedProperties, ElementLinesNoLongerMatchSoNoElementPredicatesAreGenerated) {
    // Finding: DemonstratorProperties.bt now declares elements as `Stick stick1 ()`
    // with their locations on separate InitialLocation / FinalLocation lines, but
    // the element regex still expects `Stick name (initLoc finalLoc)`. Of the 104
    // Stick/Cube lines, none match - so pointing the generator at the file it was
    // written for silently yields no AtPlace / ObjectFinalPosition / AtFinalPosition
    // / Fixed predicates and no error.
    PredicateGenerator gen(kShippedProperties);

    gen.addAtPlacePredicates();
    gen.addObjectFinalPositionPredicates();
    gen.addAtFinalPositionPredicates();
    gen.addFixedPredicates();

    for (const auto& line : gen.getPredicates()) {
        EXPECT_TRUE(line.empty() || line.rfind("//", 0) == 0) << "unexpected predicate: " << line;
    }
}

TEST(PredicateGeneratorOnTheShippedProperties, RobotsAndToolsStillParse) {
    PredicateGenerator gen(kShippedProperties);

    gen.addAtAgentPredicates();
    gen.addHasToolPredicates();
    gen.addAtToolPredicates();

    EXPECT_TRUE(has(gen.getPredicates(), "AtAgent(robot1 rpmanipulate)"));
    EXPECT_TRUE(has(gen.getPredicates(), "HasTool(robot1 gripper1)"));
    EXPECT_TRUE(has(gen.getPredicates(), "AtTool(gripper1 equiplocgripper)"));
    EXPECT_TRUE(has(gen.getPredicates(), "AtTool(staplergun1 equiplocstapler)"));
}

// ---- PredicateGenerator: file output ---------------------------------------------

namespace {
const std::string kStart = "// === GENERATED PREDICATES (DO NOT EDIT BELOW) ===";
const std::string kEnd = "// === END GENERATED PREDICATES ===";
}

TEST(PredicateGeneratorWriteToFile, CreatesTheFileWithAMarkedBlock) {
    TempDir dir;
    PredicateGenerator gen(dir.write("p.bt", kProperties));
    gen.addGripperEmptyPredicates();
    std::string out = dir.file("new.bt");

    gen.writeToFile(out);

    std::string text = TempDir::read(out);
    EXPECT_EQ(countOccurrences(text, kStart), 1);
    EXPECT_EQ(countOccurrences(text, kEnd), 1);
    EXPECT_NE(text.find("GripperEmpty(robot1)"), std::string::npos);
}

TEST(PredicateGeneratorWriteToFile, IsIdempotentAndPreservesHandWrittenContentAroundTheBlock) {
    TempDir dir;
    PredicateGenerator gen(dir.write("p.bt", kProperties));
    gen.addGripperEmptyPredicates();
    std::string out = dir.write("existing.bt", "BelongsToLayer(a l1)\n");

    gen.writeToFile(out);
    gen.writeToFile(out);
    gen.writeToFile(out);

    std::string text = TempDir::read(out);
    EXPECT_EQ(countOccurrences(text, kStart), 1);
    EXPECT_EQ(countOccurrences(text, "GripperEmpty(robot1)"), 1);
    EXPECT_NE(text.find("BelongsToLayer(a l1)"), std::string::npos);
}

TEST(PredicateGeneratorWriteToFile, ReplacesOnlyTheBlockAndKeepsTextAfterIt) {
    TempDir dir;
    PredicateGenerator gen(dir.write("p.bt", kProperties));
    gen.addGripperEmptyPredicates();
    std::string out = dir.write("existing.bt",
        "before\n" + kStart + "\nSTALE(x)\n" + kEnd + "\nafter\n");

    gen.writeToFile(out);

    std::string text = TempDir::read(out);
    EXPECT_EQ(text.find("STALE(x)"), std::string::npos);
    EXPECT_NE(text.find("before"), std::string::npos);
    EXPECT_NE(text.find("after"), std::string::npos);
    EXPECT_LT(text.find("before"), text.find(kStart));
    EXPECT_GT(text.find("after"), text.find(kEnd));
}

TEST(PredicateGeneratorWriteToFile, AStartMarkerWithoutAnEndMarkerGrowsAnotherBlockEveryRun) {
    // Finding: if the end marker is missing (e.g. someone deleted it by hand),
    // the file is treated as having no block at all: existing content - stale
    // block included - is kept and a fresh block appended. Every further run
    // appends yet another block and the old ones are never cleaned up.
    TempDir dir;
    PredicateGenerator gen(dir.write("p.bt", kProperties));
    gen.addGripperEmptyPredicates();
    std::string out = dir.write("damaged.bt", kStart + "\nSTALE(x)\n");

    gen.writeToFile(out);
    gen.writeToFile(out);

    std::string text = TempDir::read(out);
    EXPECT_NE(text.find("STALE(x)"), std::string::npos);
    EXPECT_GE(countOccurrences(text, kStart), 2);
}

TEST(PredicateGeneratorWriteToFile, OverwriteReplacesTheWholeFile) {
    TempDir dir;
    PredicateGenerator gen(dir.write("p.bt", kProperties));
    gen.addGripperEmptyPredicates();
    std::string out = dir.write("goal.bt", "old content\n");

    gen.writeToFileOverwrite(out);

    std::string text = TempDir::read(out);
    EXPECT_EQ(text.find("old content"), std::string::npos);
    EXPECT_NE(text.find("GripperEmpty(robot1)"), std::string::npos);
}

TEST(PredicateGeneratorWriteToFile, ThrowsWhenTheOutputCannotBeWritten) {
    TempDir dir;
    PredicateGenerator gen(dir.write("p.bt", kProperties));

    EXPECT_THROW(gen.writeToFile("/no/such/dir/out.bt"), std::runtime_error);
    EXPECT_THROW(gen.writeToFileOverwrite("/no/such/dir/out.bt"), std::runtime_error);
}

// ---- PDDLPredicateGenerator --------------------------------------------------------

TEST(PDDLPredicateGenerator, EmitsAClearPredicateForEveryClearObjectOnly) {
    auto bottom = makeBox("bottom", 0, 0, 0, 10, 10, 10);
    auto top = makeBox("top", 0, 0, 10, 10, 10, 20);
    auto alone = makeBox("alone", 100, 100, 0, 110, 110, 10);
    PDDLPredicateGenerator gen({bottom, top, alone}, UpAxis::Z);

    gen.addClearPredicate();

    EXPECT_TRUE(has(gen.getPredicates(), "(clear top)"));
    EXPECT_TRUE(has(gen.getPredicates(), "(clear alone)"));
    EXPECT_FALSE(has(gen.getPredicates(), "(clear bottom)"));
}

TEST(PDDLPredicateGenerator, WritesAnInitBlock) {
    TempDir dir;
    PDDLPredicateGenerator gen({makeBox("a", 0, 0, 0, 1, 1, 1)}, UpAxis::Z);
    gen.addClearPredicate();
    std::string out = dir.file("init.pddl");

    gen.writeToFile(out);

    EXPECT_EQ(TempDir::read(out), "(:init\n    (clear a)\n)\n");
}

TEST(PDDLPredicateGenerator, ThrowsWhenTheOutputCannotBeWritten) {
    PDDLPredicateGenerator gen({}, UpAxis::Z);

    EXPECT_THROW(gen.writeToFile("/no/such/dir/out.pddl"), std::runtime_error);
}

TEST(PDDLPredicateGenerator, DefaultsToYUpLikeTheRestOfTheApi) {
    auto bottom = makeBox("bottom", 0, 0, 0, 10, 10, 10);
    auto top = makeBox("top", 0, 10, 0, 10, 20, 10);  // above along Y
    PDDLPredicateGenerator gen({bottom, top});

    gen.addClearPredicate();

    EXPECT_FALSE(has(gen.getPredicates(), "(clear bottom)"));
}

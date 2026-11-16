# APTreeDSL - Behavior Tree DSL

A [MontiCore](https://www.se-rwth.de/research/MontiCore/)-based Domain Specific Language (DSL) tool for defining and analyzing behavior trees with PDDL planning integration.

---

## Table of Contents

1. [Requirements & Build](#1-requirements--build)
2. [Step-by-Step How-To](#2-step-by-step-how-to)
   - [Step 1 — Define your property types](#step-1--define-your-property-types)
   - [Step 2 — Define your predicate types](#step-2--define-your-predicate-types)
   - [Step 3 — Define your action types](#step-3--define-your-action-types)
   - [Step 4 — Regenerate grammar rules from type definitions](#step-4--regenerate-grammar-rules-from-type-definitions)
   - [Step 5 — Create concrete world instances](#step-5--create-concrete-world-instances)
   - [Step 6 — Write your behavior tree model](#step-6--write-your-behavior-tree-model)
   - [Step 7 — Validate and analyze with the APTree tool](#step-7--validate-and-analyze-with-the-aptree-tool)
   - [Step 8 — Generate C# code](#step-8--generate-c-code)
3. [Reference](#3-reference)
   - [Project structure](#project-structure)
   - [All Gradle tasks](#all-gradle-tasks)
   - [DSL grammar overview](#dsl-grammar-overview)
4. [Open TODOs](#4-open-todos)

---

## 1. Requirements & Build

**Requirements**
- **JDK 11**: `build.gradle` pins a Java 11 toolchain and configures no toolchain download, so a JDK 11 must be installed locally
- **Gradle 8.x** installed on the PATH: the repository has no Gradle wrapper (the Docker image uses Gradle 8.14.4)

**Build**

```bash
gradle build
```

Other common build commands:

| Command | Purpose |
|---|---|
| `gradle clean build` | Full clean rebuild |
| `gradle test` | Run all tests |
| `gradle tasks` | List every available task |

---

## 2. Step-by-Step How-To

This section walks through the full workflow from defining a domain to generating C# code, using the files under `src/test/resources/valid/` as a reference.

### Step 1 — Define your property types

Property types declare the categories of objects in your domain (elements, locations, agents, tools, etc.).

Create or edit a `*PropertyTypes.bt` file (e.g. `CRFPropertyTypes.bt`):

```
Element Beam  { loc: Location }
Element Plate { loc: Location }
Location FirstPos
Agent Robot   { loc: Location mType: String }
Tool VacGripper { loc: Location isActive: Boolean }
```

See `src/test/resources/valid/CRFTypes/CRFPropertyTypes.bt` for a full example.

---

### Step 2 — Define your predicate types

Predicates describe facts about the world that can be true or false (used for preconditions and effects).

Create or edit a `*PredicateTypes.bt` file:

```
Predicate Holding {
  item  : Element
  agent : Agent
}

Predicate AtPlace {
  item : Element
  loc  : Location
}
```

See `src/test/resources/valid/CRFTypes/CRFPredicateTypes.bt` for a full example.

---

### Step 3 — Define your action types

Action types describe high-level robot actions with typed parameters, PDDL-style preconditions, and effects.

Create or edit a `*ActionTypes.bt` file:

```
Define Action PickUpHL {
    ActLevel: HighLevel
    Properties {
        objToPick : Element
        objLoc    : Location
        client    : Robot
    }
    Preconditions {
        AtPlace(objToPick, objLoc)
        !Holding(client, objToPick)
    }
    Effects {
        Holding(client, objToPick)
        !AtPlace(objToPick, objLoc)
    }
}
```

See `src/test/resources/valid/CRFTypes/CRFActionTypes.bt` for a full example.

---

### Step 4 — Regenerate grammar rules from type definitions

After changing property, predicate, or action type definitions you need to regenerate the MontiCore grammar rules that correspond to them, then rebuild.

```bash
# Regenerate grammar rules (pass your file via -PinputModel=)
gradle generateCRFPropertyTypeRules -PinputModel=src/test/resources/valid/CRFTypes/MyPropertyTypes.bt
gradle generateCRFPredicateTypeRules -PinputModel=src/test/resources/valid/CRFTypes/MyPredicateTypes.bt
gradle generateCRFActionTypeRules -PinputModel=src/test/resources/valid/CRFTypes/MyActionTypes.bt

# Rebuild with new rules
gradle clean build
```

> The generated rules are written into `CRFTypesCon.mc4` between the `// === GENERATED ... ===` markers — do **not** edit those sections by hand.

---

### Step 5 — Create concrete world instances

Concrete instances populate the world with actual named objects used at runtime.

Create or edit a `*ConcreteInstances.bt` file:

```
Plate  plate1 (fp1)
Beam   beam1  (fp2)
Robot  r1     (rp1 ur10)
FirstPos fp1  ()
FirstPos fp2  ()
VacGripper vg1 (fp1 True)
```

See `src/test/resources/valid/CRFConcrete/CRFConcreteInstances.bt` for a full example.

---

### Step 6 — Write your behavior tree model

Use `FlowNode` with a `ServicePlanning` planner, a `NodeGraph`, and temporal relations between actions:

```
BehaviorTree APTree {
  Root FlowNode Main {
    All
    AllFlow
    NodeGraph {
      FlowNode HandlePlate {
        ServicePlanning MyPlanner Enhsp Domain:PickAndPlaceHL Problem:PlacePlate1
        All
        AllAction
        NodeGraph {
          Action PickUpHL PickUpPlate1 (plate1 fp1 r1) {
            --[Meets]--> PlacePlate1;
          }
          Action PlaceHL PlacePlate1 (plate1 pr1 r1)
        }
      }
    }
  }
}
```

Save your model as a `.bt` file (e.g. `src/test/resources/valid/behavior_trees/MyTree.bt`).

---

### Step 7 — Validate and analyze with the APTree tool

Run the APTree tool against your model file to parse it, check all CoCos (context conditions), and produce analysis output:

```bash
gradle runAPTreeTool
```

By default this targets `src/test/resources/valid/behavior_trees/APTreeLivematFinal.bt`, loads the concrete instances from `src/test/resources/valid/CRFConcrete/LiveMatSetupObjects.bt` and writes the property instances to `../APTreeExecutionEngine/src/ModelLoader/PropertyInstances.json`. Each of these can be overridden:

```bash
gradle runAPTreeTool -PinputModel=path/to/model.bt -Pinstances=path/to/instances.bt -PpropertyInstancesOut=path/to/PropertyInstances.json
```

You can also run individual parsers to verify specific sub-models in isolation:

```bash
gradle runBehaviorTreeParser    # parse a BehaviorTree model
gradle runCRFTypesParser        # parse CRF type definitions
gradle runPDDLPlannerParser     # parse a PDDL planner model
gradle runConcreteBTInstanceParser  # parse concrete instance models
```

---

### Step 8 — Generate C# code

Once the model validates, generate the corresponding C# types for use in the execution engine:

```bash
gradle generateCSharpParameterTypes   # object/property types
gradle generateCSharpPredicates       # predicate types
gradle generateCSharpActionTypes      # action types
```

Generated files are written straight into `../APTreeExecutionEngine/src/ModelLoader/` (`ParameterTypes/`, `PredicateTypes/`, `ActionTypes/`).

---

## 3. Reference

### Project structure

```
src/main/grammars/           # MontiCore grammar files (.mc4)
src/main/java/               # Java tool source code
src/test/resources/valid/    # Example / reference model files
  behavior_trees/            #   .bt tree models
  CRFTypes/                  #   property, predicate, and action type definitions
  CRFConcrete/               #   concrete world instances and initial/goal states
  Planners/                  #   PDDL planner definitions
target/                      # Build output
  generated-sources/         #   MontiCore-generated Java sources
  reports/allTests/          #   Test reports
```

---

### All Gradle tasks

| Category | Task | Purpose |
|---|---|---|
| **Build** | `clean build` | Full clean rebuild |
| | `test` | Run all tests |
| **Parsers** | `runBehaviorTreeParser` | Parse a BehaviorTree model |
| | `runCRFTypesParser` | Parse CRF type definitions |
| | `runPDDLPlannerParser` | Parse a PDDL planner model |
| | `runCRFPropertyTypeParser` | Parse property type file |
| | `runCRFPredicateTypeParser` | Parse predicate type file |
| | `runCRFActionTypeParser` | Parse action type file |
| | `runCRFConPropertyParser` | Parse concrete property instances |
| | `runCRFConPredicateParser` | Parse concrete predicate instances |
| | `runConcreteBTInstanceParser` | Parse a concrete BT instance model |
| **Tools** | `runAPTreeTool` | Validate and analyze an APTree model |
| **Rule generators** | `generateCRFPropertyTypeRules` | Regenerate grammar rules for property types |
| | `generateCRFPredicateTypeRules` | Regenerate grammar rules for predicate types |
| | `generateCRFActionTypeRules` | Regenerate grammar rules for action types |
| **C# generators** | `generateCSharpParameterTypes` | Generate C# property/object types |
| | `generateCSharpPredicates` | Generate C# predicate types |
| | `generateCSharpPredicateTypes` | Generate C# predicate type definitions |
| | `generateCSharpActionTypes` | Generate C# action types |
| **Diagrams** | `generateASTClassDiagram` | Generate AST class diagram |

---

### DSL grammar overview

The DSL is composed of layered MontiCore grammars:

```
BehaviorTree          — base tree nodes (Sequence, Parallel, Action, Decorator, Service)
  └── CRFTypesDef     — abstract type system (Element, Location, Agent, Tool, Predicate, Action)
        └── CRFTypesCon  — concrete generated types and predicates
              └── PlanningService  — PDDL planner integration (Domain, Problem, ServicePlanning)
                    └── DynamicBTFlowNode  — APTree with FlowNode, NodeGraph, temporal relations
```

Temporal relation types available in `NodeGraph`: `Meets`, `Precedes`, `Overlaps`, `Starts`, `Finishes`, `Contains`, `Equals`.

The relations inside one `NodeGraph` must not form a cycle, and a node must not be related to itself. The CoCo `NodeGraphRelationsAcyclic` (errors `0xDF010` / `0xDF011`) checks this in `APTreeTool` and in the editor's validate call (`APTreeJsonCli`).

---

## 4. Open TODOs

Known grammar and tool issues, collected while writing the test suite. The tests that document them name the file and the parser error.

**Grammar**
- [ ] Relations after an `Action` graph node don't parse: the CRFTypesCon action productions have their own optional `{ (Decorator|Service)* }` block, which shadows the GraphNode relation block (`{ --[Meets]--> X; }`). Affects `APTreeLivematFinal.bt`, `APTree.bt`, `APTree2.bt` (see `APTreeModelParserTest`).
- [ ] `DemonstratorLLSubtrees.bt`: quoted move types (`"movel"`) where the grammar expects a Name, and `LiftLL`/`RetractLL` missing a parameter.
- [ ] `SubTrees.bt`: named `NodeGraph temp1graph { }`, the grammar only accepts an anonymous `NodeGraph { }`.
- [ ] `PDDLPlanner.bt`: action names in `Domain ... {PickUpHL, PlaceHL}` collide with keywords (`PlannerParserTest` is disabled).
- [ ] `CRFConcreteInstances.bt`, `LiveMatSetupObjects.bt`: `Robot r1 (rp1 ur10)` passes a model name where the Robot type expects a boolean; `LiveMatSetupObjects.bt` also has `Stack lay1 (1 m1)`.
- [ ] `CRFInitialState.bt`: lowercase `atPlace(...)`, the keyword is `AtPlace`.
- [ ] `LiveMatPredicaetTypes.bt`: stray `@` in `client: Name@Agent`; `DemonstratorActionTypes.bt`: `cont moveType: String` doesn't match the action type grammar (see `CRFTypesParserTest`).

**APTreeTool**
- [ ] The default model and instances file (`APTreeLivematFinal.bt`, `LiveMatSetupObjects.bt`) don't parse, so `gradle runAPTreeTool` without arguments fails.
- [ ] The instance loader only knows the LiveMat types (Beam, Plate, Robot, FirstPos): with the Demonstrator files it exports 0 property instances and logs `0xA7003` errors (see `EndToEndAPTreeToolTest`).
- [ ] `MustHavePlanningService`, `PlanningServiceActionsCoverageCoCo` and `SharedResourceConflictCoCo` are created but not registered.
- [ ] The editor's validate call (`APTreeJsonCli`) skips the element pre-validation and the other CoCos; only `NodeGraphRelationsAcyclic` runs there.
- [ ] `src/temp/URMotionPlanner.mc4` and `src/main/java/URMotionPlannerTest.txt` are unfinished.

---

**Version**: 7.8.0 (MontiCore 7.8.0)

# APTreeDSL - Behavior Tree DSL

A [MontiCore](https://www.se-rwth.de/research/MontiCore/)-based Domain Specific Language (DSL) tool for defining and analyzing behavior trees with PDDL planning integration.

## Quick Start

### Requirements

- **Java 11+** (configured via Gradle toolchains)
- **Gradle** 7.x+ (system install)

### Build

```bash
gradle build
```

## Common Commands

| Command | Purpose |
|---------|---------|
| `gradle clean build` | Full rebuild |
| `gradle test` | Run tests |
| `gradle tasks` | List all available tasks |
| `gradle generateCSharpParameterTypes` | Generate C# parameter types |
| `gradle runAPTreeTool` | Analyze APTree models |

## Project Structure

```
src/main/grammars/    # MontiCore grammar files (.mc4)
src/main/java/        # Java source code
target/               # Build output
```

## Key Tasks

**Parsers**: `runBehaviorTreeParser`, `runCRFTypesParser`, `runPDDLPlannerParser`

**Generators**: `generateCSharpParameterTypes`, `generateCSharpPredicates`, `generateCSharpActionTypes`

**Tools**: `runAPTreeTool`, `runConcreteBTInstanceParser`, `generateASTClassDiagram`

## Resources

- See `build.gradle` for complete dependencies and all available tasks
- Generated code: `target/generated-sources/monticore/sourcecode/`
- Test reports: `target/reports/allTests/`
- MontiCore Handbook: [MontiCore documentation](https://www.se-rwth.de/research/MontiCore/)

---

**Version**: 7.8.0 (MontiCore 7.8.0)

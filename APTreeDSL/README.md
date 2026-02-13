# APTreeDSL - Behavior Tree DSL

A MontiCore-based Domain Specific Language (DSL) tool for defining and analyzing behavior trees with support for PDDL planning integration.

## Table of Contents

- [System Requirements](#system-requirements)
- [Dependencies](#dependencies)
- [Installation & Setup](#installation--setup)
- [Building the Project](#building-the-project)
- [Running Gradle Commands](#running-gradle-commands)
- [Available Tasks](#available-tasks)
- [MontiCore Information](#monticore-information)

## System Requirements

- **Java Development Kit (JDK) 11 or higher**
  - The project is configured to use Java 11 via Gradle toolchains
  - Download from: https://www.oracle.com/java/technologies/downloads/
  - Or use OpenJDK: https://openjdk.org/

- **Gradle** (optional - bundled via Gradle wrapper)
  - The project includes a Gradle wrapper, so you don't need to install Gradle separately
  - If you want to install Gradle independently: https://gradle.org/install/

- **Windows, macOS, or Linux** operating system

## Dependencies

### Build Tool Dependencies

The project uses **Gradle** as the build tool. All Java dependencies are automatically managed through Gradle and Maven repositories.

### Java Dependencies

Key dependencies managed by Gradle (see `build.gradle` for complete list):

- **MontiCore 7.8.0** - Language workbench for DSL development
  - `de.monticore:monticore-grammar:7.8.0`
  - `de.monticore:monticore-runtime:7.8.0`

- **CD4Analysis** - Class diagram analysis library
  - `de.monticore.lang:cd4analysis:7.8.0`

- **Apache Commons Lang 3.11** - Utility library
  - `org.apache.commons:commons-lang3:3.11`

- **Google Guava 33.1.0** - Core libraries for Java
  - `com.google.guava:guava:33.1.0-jre`

- **JUnit 5.10.3** - Testing framework
  - Used for unit tests

All dependencies are automatically downloaded from the Maven repository when you run Gradle commands.

## Installation & Setup

### 1. Clone or Download the Repository

```bash
git clone <repository-url>
cd APTreeDSL
```

### 2. Verify Java Installation

Ensure you have Java 11 or higher installed:

```bash
java -version
javac -version
```

### 3. No Additional Installation Needed

The project includes a **Gradle wrapper** (`gradlew.bat` on Windows, `gradlew` on Unix/Linux), so you don't need to install Gradle separately. All dependencies will be downloaded automatically on first build.

## Building the Project

### Using Gradle Wrapper (Recommended)

**On Windows:**
```bash
gradlew.bat build
```

**On macOS/Linux:**
```bash
./gradlew build
```

### Using Gradle (if installed globally)

```bash
gradle build
```

## Running Gradle Commands

### Common Gradle Commands

#### Clean Build
Remove all generated files and rebuild from scratch:
```bash
gradlew.bat clean build
```

#### Compile Only
Compile the source code and generate MontiCore grammars:
```bash
gradlew.bat compileJava
```

#### Generate MontiCore Code
Generate code from the `.mc4` grammar files:
```bash
gradlew.bat generateMCGrammars
```

#### Run Tests
Execute all unit tests:
```bash
gradlew.bat test
```

#### Generate Test Reports
Create aggregated test reports:
```bash
gradlew.bat testReport
```

#### View All Available Tasks
List all available Gradle tasks:
```bash
gradlew.bat tasks
```

#### Create Shadow JAR (All-in-One)
Build a single executable JAR file with all dependencies:
```bash
gradlew.bat shadowJar
```

## Available Tasks

The following custom tasks are defined for the APTreeDSL:

### Parser Tests
- `runBehaviorTreeParser` - Test behavior tree parsing
- `runBehaviorTreeGrammarParser` - Test grammar parsing
- `runCRFTypesParser` - Parse CRF type definitions
- `runPDDLPlannerParser` - Parse PDDL planner definitions
- `runCRFPropertyTypeParser` - Parse property types
- `runCRFPredicateTypeParser` - Parse predicate types
- `runCRFActionTypeParser` - Parse action types
- `runDynamicFlowNodeParser` - Parse dynamic flow nodes

**Example:**
```bash
gradlew.bat runBehaviorTreeParser
```

### Code Generators
- `generateCSharpParameterTypes` - Generate C# parameter type classes
- `generateCSharpPredicates` - Generate C# predicate classes
- `generateCSharpActionTypes` - Generate C# action type classes
- `generateCRFPropertyTypeRules` - Generate grammar rules for property types
- `generateCRFActionTypeRules` - Generate grammar rules for action types
- `generateCRFPredicateTypeRules` - Generate grammar rules for predicate types
- `generateInstanceSymbols` - Generate symbol files for instances

**Example:**
```bash
gradlew.bat generateCSharpParameterTypes
```

### Analysis Tools
- `runAPTreeTool` - Parse and analyze APTree behavior tree models
- `runConcreteBTInstanceParser` - Parse concrete instance definitions (Beam, Robot, etc.)
- `generateASTClassDiagram` - Generate class diagram from grammar (AST structure)

**Example:**
```bash
gradlew.bat runAPTreeTool
```

## MontiCore Information

### What is MontiCore?

MontiCore is a language workbench used to define Domain Specific Languages (DSLs) through formal grammars. Key features:

- **Grammar-based DSL Development** - Define your language syntax in `.mc4` grammar files
- **Automatic Code Generation** - MontiCore generates parsers, AST classes, and symbol tables
- **Type Checking & Cocos** - Built-in support for semantic analysis and constraints

### Grammar Files

The grammar files for this project are located in `src/main/grammars/`:

- `.mc4` files - MontiCore grammar definitions
- These define the syntax and structure of behavior trees and related DSLs

### Generated Code

Generated Java code from MontiCore grammars is placed in:
```
target/generated-sources/monticore/sourcecode/
```

This code is automatically compiled as part of the build process.

### Learn More

- **MontiCore Website**: https://www.monticore.de/
- **MontiCore GitHub**: https://github.com/MontiCore/monticore
- **MontiCore Handbook**: See `MontiCore-Handbook (2).pdf` in this directory

## Project Structure

```
APTreeDSL/
├── src/
│   └── main/
│       ├── grammars/          # MontiCore grammar files (.mc4)
│       └── java/              # Java source code
├── target/                    # Build output directory
├── build.gradle               # Gradle configuration
├── gradle.properties          # Gradle properties (versions, settings)
├── settings.gradle            # Build definition
└── .editorconfig              # Editor configuration
```

## Troubleshooting

### Issue: "Java version mismatch"
**Solution**: The project is configured to use Java 11 via Gradle toolchains. Ensure Java 11+ is installed on your system.

### Issue: "Dependencies not found"
**Solution**: Run `gradlew.bat clean` to clear the cache and then `gradlew.bat build` again. This forces fresh downloads.

### Issue: "Cannot find gradle wrapper"
**Solution**: Ensure you're in the `APTreeDSL` directory where `gradlew.bat` (Windows) or `gradlew` (Unix/Linux) exists.

## Support & Resources

- MontiCore Handbook: See `MontiCore-Handbook (2).pdf`
- Build output: `target/` directory contains build artifacts and reports
- Test reports: `target/reports/allTests/`

---

**Version**: 7.8.0 (MontiCore 7.8.0)

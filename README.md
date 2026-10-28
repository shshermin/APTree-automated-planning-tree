<p align="center">
  <img src="APTreeEditor/BehaviorTreeFrontend/src/assets/APTree-02.png" alt="APTree Logo" width="300"/>
</p>

# APTree

This project contains the core components for behavior tree design and execution using the APTree domain specific language.

## Structure

### APTreeDSL
A Gradle-based Domain-Specific Language (DSL) implemented using the [MontiCore language workbench](https://www.se-rwth.de/research/MontiCore/) for defining behavior trees using a custom syntax.
- **Location**: `APTreeDSL/`
- **Build**: Gradle
- **Output**: behavior tree models and validates syntax based on APTree DSL

### APTreeExecutionEngine
A C# application that executes behavior trees compatible with APTree at runtime.
- **Location**: `APTreeExecutionEngine/`
- **Build**: .NET/C#
- **Features**: 
  - Behavior tree execution
  - Action execution with logging
  - Python service integration for planning

### APTreeEditor
A React/TypeScript web editor (Vite) for building behavior trees on a canvas; it talks to the execution engine's backend.
- **Location**: `APTreeEditor/BehaviorTreeFrontend/`
- **Build**: npm

### InputGenerator
A C++ tool (CMake, CGAL) that derives spatial PDDL predicates from mesh geometry (OBJ).
- **Location**: `InputGenerator/`
- **Build**: CMake

## Getting Started

1. Define and validate behavior trees using the DSL in `APTreeDSL/`
2. Execute trees using the C# engine in `APTreeExecutionEngine/`

To run the planner service and the backend together, see [DOCKER_README.md](DOCKER_README.md).


## Documentation

See individual project READMEs for detailed setup and usage instructions:
- [APTreeDSL README](APTreeDSL/README.md)
- [APTreeExecutionEngine README](APTreeExecutionEngine/README.md)
- [APTreeEditor README](APTreeEditor/BehaviorTreeFrontend/README.md)
- [InputGenerator README](InputGenerator/README.md)
- [Docker README](DOCKER_README.md)

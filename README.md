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

## Getting Started

1. Define and validate behavior trees using the DSL in `APTreeDSL/`
3. Execute trees using the C# engine in `APTreeExecutionEngine/`


## Documentation

See individual project READMEs for detailed setup and usage instructions:
- [APTreeDSL README](APTreeDSL/README.md)
- [APTreeExecutionEngine README](APTreeExecutionEngine/README.txt)

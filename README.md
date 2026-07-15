# APTree – Automated Planning Trees

This repository contains the implementation of APTree, a framework for behavior tree design and execution with integrated PDDL planning. The project consists of three main components: a domain-specific language (DSL), an execution engine, and an optional visual editor.

---

## System Requirements

| Dependency | Version | Purpose |
|---|---|---|
| Java JDK | 11+ | APTreeDSL grammar compilation (Gradle toolchains) |
| Gradle | 7.x+ | Build system for APTreeDSL |
| .NET SDK | 8.0 | Execution engine (ASP.NET Core backend) |
| Python | 3.10+ | PDDL planning service |
| Docker | 24.0+ | Container-based planners (FF, LAMA-FIRST) and full-stack deployment |
| Node.js | 18+ | (Optional) APTreeEditor frontend |

**Supported operating systems:** Linux (native), Windows (via WSL2 for Docker/planning), macOS.

The PDDL planning service requires a Linux environment (or Docker) because the `planutils` planner container relies on Linux-based solver binaries. On Windows, use WSL2 or the Docker-based deployment.

---

## Project Structure

```
APTreeDSL/               MontiCore-based DSL for behavior tree definitions
APTreeExecutionEngine/   C# execution engine + Python planning service
APTreeEditor/            (Optional) Vue.js visual editor frontend
docker/                  Docker entrypoint scripts
Dockerfile              Full-stack container image
docker-compose.yml      Compose configuration
```

---

## Quick Start (Docker – recommended for reproducibility)

The simplest way to run the full system:

```bash
# 1. Build the DSL fat-jar (required by the Docker image)
cd APTreeDSL
gradle shadowJar
cd ..

# 2. Build and run via Docker Compose
docker compose up --build
```

This starts:
- **Python planning service** on `http://localhost:5000`
- **ASP.NET backend** on `http://localhost:5254`

Verify with:
```bash
curl http://localhost:5000/health
curl http://localhost:5254/health
```

---

## Manual Setup (component by component)

### 1. APTreeDSL (Behavior Tree DSL)

```bash
cd APTreeDSL
gradle build      # Windows
```

See [APTreeDSL/README.md](APTreeDSL/README.md) for available Gradle tasks (parsing, code generation, testing).

### 2. APTreeExecutionEngine (C# Backend)

```bash
cd APTreeExecutionEngine
dotnet restore BehaviorTreeMainProject.csproj
dotnet run --project BehaviorTreeMainProject.csproj --urls http://localhost:5254
```

### 3. Python Planning Service

```bash
cd APTreeExecutionEngine/python_service

# Create virtual environment
python3 -m venv pddl_env
source pddl_env/bin/activate        # Linux/macOS
# pddl_env\Scripts\activate         # Windows (limited planner support)

# Install pinned dependencies
pip install -r requirements.txt

# Start the service
python pddl_planning_service.py
```

The service listens on port `5000` and supports the following planners:
- **ENHSP** (numeric planning, via bundled JAR)
- **FF** (classical planning, via `planutils` Docker container)
- **LAMA-FIRST** (satisficing planning, via `planutils` Docker container)

For Docker-based planners (FF, LAMA-FIRST), ensure the `planutils` container is running:
```bash
docker start planutils
```

---

## Reproducing the Experiments

### Planning Experiments

1. Start the planning service (see above).
2. Use the execution engine to run a behavior tree file:
   ```bash
   cd APTreeExecutionEngine
   dotnet run 
   ```
3. Behavior tree input files are located in `APTreeDSL/src/test/resources/valid/behavior_trees/`.
4. Logged results (timing, action execution summaries) are stored in `APTreeExecutionEngine/kept logs/`.

### DSL Validation and Parsing

```bash
cd APTreeDSL
gradle test                        # Run all grammar and parsing tests
gradle runAPTreeTool               # Analyze APTree models
gradle runBehaviorTreeParser       # Parse behavior tree files
```

Test reports are generated at `APTreeDSL/target/reports/allTests/`.

---

## Pinned Versions

| Component | Version |
|---|---|
| MontiCore | 7.8.0 |
| .NET SDK | 8.0 |
| Python dependencies | See [`requirements.txt`](APTreeExecutionEngine/python_service/requirements.txt) |
| Gradle | 7.x+ (system install) |

---

## Documentation

- [APTreeDSL README](APTreeDSL/README.md) – DSL grammar, build tasks, code generation
- [Docker README](DOCKER_README.md) – Full-stack container deployment
- [Planning Service README](APTreeExecutionEngine/python_service/README.md) – PDDL planner configuration

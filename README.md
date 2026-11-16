<p align="center">
  <img src="APTreeEditor/BehaviorTreeFrontend/src/assets/APTree-02.png" alt="APTree Logo" width="300"/>
</p>

# APTree: Automated Planning Trees

APTree combines **behavior trees** with **automated PDDL planning** for robotic assembly and construction. You model a task as a behavior tree in the APTree DSL. While the tree runs, a planner (ENHSP) fills in the missing action sequences from the current world state, and the tree executes them, re-planning when the world changes or an action fails.

- **Hierarchical planning**: high-level (HL) actions such as "pick up stick 3" are planned first, then each is expanded into mid-level (ML) actions, which map to low-level (LL) robot commands.
- **Temporal relations**: plans become node graphs with Allen-style relations (`Meets`, `Starts`, `Finishes`, ...) between actions, and the DSL rejects cyclic ones.
- **Replanning and fault injection**: decorators re-plan from the blackboard after faults, and fault scenarios can be injected for experiments.
- **Editor**: a web editor shows the tree, follows the ticks live and validates models against the DSL.
- **Geometry input**: a C++ tool and a Grasshopper script derive spatial predicates (which part lies on which) from a Rhino scene.

## How the parts fit together

```
Rhino scene ──OBJ──▶ InputGenerator ──▶ init/goal state (PDDL + DSL)
                                              │
                         APTreeDSL (.bt model, validation, C# code generation)
                                              │ BehaviorTreeModel.json + C# types
                                              ▼
APTreeEditor ◀──HTTP/WebSocket──▶ APTreeExecutionEngine ──HTTP──▶ planner service (ENHSP)
  (browser)                         (ticks the tree)    ──HTTP──▶ robot service (UR10, optional)
```

| Component | What it is | Built with |
|---|---|---|
| [APTreeDSL](APTreeDSL/README.md) | DSL for behavior trees, types, predicates and world states; validates models and generates C# types and the JSON model | Java, [MontiCore](https://www.se-rwth.de/research/MontiCore/), Gradle |
| [APTreeExecutionEngine](APTreeExecutionEngine/README.md) | Runs the tree, holds the world state on a blackboard, calls the planner, validates plans, serves the editor's API | .NET 8 |
| [python_service](APTreeExecutionEngine/python_service/README.md) | `pddl_planning_service.py` wraps ENHSP (and optionally FF / LAMA via Planutils); `robot_service.py` sends commands to a UR10 | Python, Flask |
| [APTreeEditor](APTreeEditor/BehaviorTreeFrontend/README.md) | Web editor: canvas, live tick view, `.bt` import and validation | React, TypeScript, Vite |
| [InputGenerator](InputGenerator/README.md) | Derives stacking/contact predicates from an OBJ scene; `StackDetector.cs` does the same inside Grasshopper | C++17, CGAL |

## Quick start (Docker)

The Docker image contains the planner service, the backend and the editor. You only need Docker.

```bash
docker build -t aptree:latest .
docker run -p 127.0.0.1:5000:5000 -p 5254:5254 aptree:latest
```

Then open the editor at http://localhost:5254. Health checks:

```bash
curl http://localhost:5000/health   # planner service
curl http://localhost:5254/health   # backend
```

See [DOCKER_README.md](DOCKER_README.md) for details. The planner service has no authentication, so keep port 5000 on the loopback as shown.

## Running from source

Requirements:

| Needed for | Requirement |
|---|---|
| Execution engine | .NET 8 SDK |
| Planner service | Python 3.10+ with `flask` and `requests`, Java 17 runtime for `enhsp.jar` |
| DSL tool and code generation | JDK 11 and Gradle 8.x (no Gradle wrapper in the repo) |
| Editor | Node.js 18+ and npm |
| InputGenerator (optional) | CMake 3.20+, a C++17 compiler, CGAL |
| Grasshopper script (optional) | Rhino with Grasshopper |

Run the demonstrator in planning-only mode, with no robot:

```bash
# 1. Planner service (terminal 1)
cd APTreeExecutionEngine/python_service
python3 -m venv pddl_env && source pddl_env/bin/activate
pip install -r requirements.txt
python pddl_planning_service.py

# 2. Execution engine (terminal 2): plans the demonstrator tree
cd APTreeExecutionEngine
dotnet run --project BehaviorTreeMainProject.csproj -- --run src/ModelLoader/BehaviorTreeModel.json src/ModelLoader/DemonstratorConfig.json

# 3. Optional: backend for the editor (terminal 3), then the editor (terminal 4)
cd APTreeExecutionEngine && dotnet run --project BehaviorTreeMainProject.csproj
cd APTreeEditor/BehaviorTreeFrontend && npm install && npm run dev
```

The config's `plannerPath` and `subtreePlannerPath` point to `/home/ubuntu/ENHSP-Public/enhsp.jar` (the VM and Docker location). For a local run, set both to the absolute path of the bundled `APTreeExecutionEngine/python_service/enhsp.jar`. The [execution engine README](APTreeExecutionEngine/README.md) covers the full workflow: generating inputs from the DSL, the config reference, run modes and fault injection.

## Working with your own task

1. **Describe the domain** in the DSL: property types, predicate types and action types ([APTreeDSL README](APTreeDSL/README.md), steps 1 to 4).
2. **Describe the scene**: write the initial and goal state by hand, or derive them from a Rhino model with the [InputGenerator](InputGenerator/README.md).
3. **Write the behavior tree** (`.bt`) and validate it with the APTree tool or in the editor.
4. **Generate** the C# types and `BehaviorTreeModel.json` (`gradle generateCSharp*`, `gradle runAPTreeTool`), and provide matching PDDL domain/problem files under `APTreeExecutionEngine/python_service/Plannerinputs/`.
5. **Run** the tree with `--run`, and watch it in the editor.

## Robot execution

The `--run` runner only plans: it expands HL actions into ML plans but does not inject LL robot subtrees, whatever `executionActive` says. Robot commands are sent by the `--test` demonstrator harness (`src/Tests/DemonstratorTreeTest.cs`), which switches LL execution on itself and talks to `robot_service.py` on the PC connected to the UR10. All moves are capped to one shared speed and acceleration limit (`MOVE_VEL_CAP` / `MOVE_ACC_CAP` in `python_service/ur10_control/ur10_commands.py`). The engine does not ask for operator confirmation, so test with the robot in reduced mode and an emergency stop within reach.

## Tests

| Component | Command |
|---|---|
| Execution engine | `dotnet test BehaviorTreeMainProject.sln` (slow tests: `APTREE_RUN_SLOW_TESTS=1`) |
| DSL | `cd APTreeDSL && gradle test` |
| Planner and robot service | `cd APTreeExecutionEngine/python_service && python3 -m pytest` (install `requirements-test.txt` first) |
| Editor | `cd APTreeEditor/BehaviorTreeFrontend && npm test` |
| InputGenerator | `cd InputGenerator/build && ctest --output-on-failure` |
| Grasshopper script | manual checklist in [MANUAL_GRASSHOPPER_TESTS.md](InputGenerator/tests/MANUAL_GRASSHOPPER_TESTS.md) |

## License

APTree is licensed under the [Apache License 2.0](LICENSE). The bundled ENHSP planner (`enhsp.jar`) is GPL-3.0-or-later; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

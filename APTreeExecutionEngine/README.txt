# APTree Execution Engine - Full ModelLoader Test

This guide starts from a new checkout and runs the complete behavior-tree test through the ModelLoader. The test loads a generated behavior-tree model, registers the generated scene objects and initial state, requests plans from the Python planning service, and executes the resulting tree.

Run all commands from the repository layout shown below. The `APTreeDSL` and `APTreeExecutionEngine` directories must remain siblings.

```text
BehaviorTreeMainProject/
|- APTreeDSL/
`- APTreeExecutionEngine/
```

## 1. Required Software and Versions

| Software | Required version | Tested version | Purpose |
|---|---:|---:|---|
| .NET SDK | 8.0 or newer | 9.0.102 | Builds the `net8.0` execution engine |
| Python | 3.10 or newer | 3.13.7 | Runs the Flask planning service |
| Java | 17 or newer | OpenJDK 17.0.17 | Runs the ENHSP planner JAR |
| Gradle | 7.x or newer | 8.14.2 | Regenerates runtime files from the DSL models |

The Python dependencies are pinned in `python_service/requirements.txt`:

- Flask 3.1.0
- Requests 2.32.3

Docker needed only using a planutils planner such as FF, LAMA-FIRST, or TFD.

Verify the installed tools in PowerShell:

```powershell
dotnet --version
python --version
java -version
gradle --version
```

If `python` is not recognized on Windows, try `py --version` and use `py` instead of `python` in the commands below.

## 2. Understand the Generated Runtime Inputs

The execution engine does not define the behavior tree or initial world directly in C#. These three JSON files are generated from the input `.bt` models in `APTreeDSL`:

| Generated file | Meaning | DSL source used by the included LiveMat test |
|---|---|---|
| `src/ModelLoader/BehaviorTreeModel.json` | Behavior-tree nodes, relations, decorators, and planning services | `APTreeDSL/src/test/resources/valid/behavior_trees/APTreeLiveMat.bt` |
| `src/ModelLoader/LiveMatSetupObjects.json` | Scene/setup objects and their properties | `APTreeDSL/src/test/resources/valid/CRFConcrete/LiveMatSetupObjects.bt` |
| `src/ModelLoader/InitialStatePredicates.json` | Predicates that describe the initial world state | `APTreeDSL/src/test/resources/valid/CRFConcrete/LiveMatInitialState.bt` |

Do not treat these JSON files as the primary model. Change the corresponding DSL input model and regenerate the JSON. See the [APTreeDSL README](../APTreeDSL/README.md) for the DSL requirements, build, and generator overview.

The generated model refers to PDDL files under `python_service/Plannerinputs/static`. The included LiveMat test already contains its domain, problem, and batch object files there.

## 3. Generate the JSON Files from the DSL

The repository contains generated JSON files, so a first run can skip this section. Run these commands whenever a DSL input model changes or when you want to reproduce the generated files.

From the repository root:

```powershell
cd .\APTreeDSL
gradle build
gradle generateBTModelJson
gradle runLiveMatSetupObjectsGenerator
gradle runInitialStateJsonGenerator
cd ..
```

These tasks write the three JSON files directly into `APTreeExecutionEngine/src/ModelLoader`.

Verify that generation succeeded:

```powershell
Test-Path .\APTreeExecutionEngine\src\ModelLoader\BehaviorTreeModel.json
Test-Path .\APTreeExecutionEngine\src\ModelLoader\LiveMatSetupObjects.json
Test-Path .\APTreeExecutionEngine\src\ModelLoader\InitialStatePredicates.json
```

All three commands must print `True`.


 A new domain must also provide compatible PDDL files and generated C# parameter, predicate, and action types. The relevant DSL tasks are `generateCSharpPropertyTypes`, `generateCSharpPredicateTypes`, and `generateCSharpActionTypes`.

## 4. Configure the Model and ENHSP Planner

Open `src/ModelLoader/LiveMatExecutionConfig.json`. Its `modelPath` selects the generated behavior-tree JSON, and `treeName` must exactly match a tree name inside that JSON.

The `plannerPath` is interpreted by the machine running the Python planning service, not by the .NET process. For a local Windows run, point it to the included JAR using an absolute path with forward slashes:

```json
{
	"modelPath": "BehaviorTreeModel.json",
	"treeName": "LiveMat",
	"pddlBasePath": "Plannerinputs/static",
	"plannerPath": "C:/Users/YOUR_NAME/Documents/BehaviorTreeMainProject/APTreeExecutionEngine/python_service/enhsp.jar",
	"plannerName": "ENHSP",
	"timeoutSeconds": 120,
}
```

Replace `YOUR_NAME` and the parent directories with the real location of this checkout. Confirm that the JAR exists:

```powershell
Test-Path .\APTreeExecutionEngine\python_service\enhsp.jar
```

If the planning service runs on Linux, WSL, a container, or a remote machine, use an absolute path that exists in that environment instead.

## 5. Create the Python Environment

This setup is required once. From the repository root in PowerShell:

```powershell
cd .\APTreeExecutionEngine
python -m venv .\python_service\.venv
.\python_service\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
python -m pip install -r .\python_service\requirements.txt
```

If PowerShell blocks virtual-environment activation, allow scripts only for the current terminal and activate again:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\python_service\.venv\Scripts\Activate.ps1
```

Linux/macOS activation equivalent:

```bash
source python_service/.venv/bin/activate
```

## 6. Start the Planning Service - Terminal 1

Keep this terminal open while the behavior-tree test runs.

```powershell
cd C:\path\to\BehaviorTreeMainProject\APTreeExecutionEngine
.\python_service\.venv\Scripts\Activate.ps1
python .\python_service\pddl_planning_service.py
```

The service must report that it is listening on port `5000`.

In another PowerShell terminal, verify the service:

```powershell
Invoke-RestMethod http://localhost:5000/health
```

The returned `status` must be `healthy`. The health endpoint checks the service's built-in default ENHSP path, so `enhsp_available` may be `False` even when the `plannerPath` in `LiveMatExecutionConfig.json` is correct. The actual planning request uses the path from that JSON configuration.

## 7. Build the Execution Engine - Terminal 2

From the execution-engine directory:

```powershell
cd C:\path\to\BehaviorTreeMainProject\APTreeExecutionEngine
dotnet restore .\BehaviorTreeMainProject.csproj
dotnet build .\BehaviorTreeMainProject.csproj --no-restore
```

The project may print existing compiler warnings. The important final result is `Build succeeded` with zero errors.

## 8. Run the Full ModelLoader Test - Terminal 2

Make sure Terminal 1 is still running the Python planning service, then run:

```powershell
dotnet run --project .\BehaviorTreeMainProject.csproj
```

No `--model-test` argument is needed. ModelLoader execution is the default and only startup path used by this command.

During a successful run:

1. The blackboard registers generated scene objects and initial-state predicates.
2. `BehaviorTreeModel.json` is loaded using `LiveMatExecutionConfig.json`.
3. The engine sends planning requests to `http://localhost:5000`.
4. ENHSP anf FF produce plans for the cassette subtrees.
5. The behavior tree executes the generated actions and replans when required.
6. The console eventually prints `FULL BEHAVIOR TREE TEST COMPLETED`.

Detailed logs and CSV summaries are written under `WrittenLogs` and the execution-engine working directory.
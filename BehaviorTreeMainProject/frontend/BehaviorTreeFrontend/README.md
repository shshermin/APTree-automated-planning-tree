BehaviorTree Frontend
======================

Quick guide for running the Vite + React TypeScript frontend locally.

Prerequisites
-------------
- Node.js 18 (or newer) and npm installed on your machine.

First-Time Setup
----------------
1. Open a terminal at the repository root.
2. Change into the frontend app directory:
	 ```bash
	 cd frontend/BehaviorTreeFrontend
	 ```
3. Install dependencies:
	 ```bash
	 npm install
	 ```

Start the Dev Server
--------------------
Run Vite in development mode to launch the app with hot reloading:

```bash
npm run dev
```

The command prints a local URL (typically `http://localhost:5173`). Open it in your browser to view the UI. The server watches for file changes and reloads automatically.

Additional Commands
-------------------
- **Preview production build**
	```bash
	npm run build && npm run preview
	```
- **Run linter (if configured)**
	```bash
	npm run lint
	```

Troubleshooting
---------------
- If you update dependencies or switch Node versions, delete `node_modules` and rerun `npm install`.
- Should the dev server fail to start, ensure no other process is using port 5173 or set a custom port via `npm run dev -- --port 3000`.
- If **APTree import/validate** suddenly fails (e.g. `Parsing failed`) after changing APTree/grammar files, rebuild the MontiCore tool jar (the backend executes the jar). This repo uses system Gradle (no `./gradlew`):
	```bash
	cd MontiCoreTool
	gradle shadowJar
	```

Import APTree (.bt)
-------------------
The UI supports importing MontiCore APTree models (`.bt`) and visualizing them as a graph.

Prerequisites:
- Java runtime available on your machine (`java` on PATH)
- MontiCore tool jar built (`MontiCoreTool/target/libs/*-tool.jar`)
- Backend running (Vite proxies `/api/*` to `http://localhost:5254`)

Build the tool jar (once after changes):
```bash
cd MontiCoreTool
gradle shadowJar
```

Run backend + frontend:
```bash
# backend (repo root)
cd BehaviorTreeMainProject
dotnet run --project BehaviorTreeMainProject.csproj --urls http://localhost:5254

# frontend (this folder)
npm run dev
```

Then in the UI use the header menu item: **Import APTree (.bt)**.

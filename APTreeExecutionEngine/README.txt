# APTree Execution Engine

## Requirements

- .NET SDK 8.0
- Python 3.10+ (for the PDDL planning service)
- Docker 24.0+ (for planutils-based planners: FF, LAMA-FIRST)
- Java 17+ (for ENHSP planner JAR)

## 1. Start the Backend (ASP.NET Core)

```bash
cd APTreeExecutionEngine
dotnet run --project BehaviorTreeMainProject.csproj --urls http://localhost:5254
```

### Endpoints

| URL | Description |
|-----|-------------|
| http://localhost:5254/health | Health check |
| http://localhost:5254/swagger | API documentation |
| http://localhost:5254/api/catalog/decorators | Decorator catalog |
| http://localhost:5254/api/catalog/services | Service catalog |
| http://localhost:5254/api/catalog/flows | Flow catalog |

## 2. MontiCore APTree Tool (for .bt import/validation)

The backend endpoint `/api/aptree/validate` executes the MontiCore tool JAR.

Build/update the JAR:
```bash
cd APTreeDSL
gradle shadowJar
```

## 3. Python Planning Service

### Setup

```bash
cd APTreeExecutionEngine/python_service

# Create virtual environment (once)
python3 -m venv pddl_env

# Activate environment
source pddl_env/bin/activate          # Linux/macOS
# pddl_env\Scripts\activate           # Windows

# Install dependencies
pip install -r requirements.txt
```

### Start the Service

```bash
python pddl_planning_service.py
```

The service runs on port 5000.

### Docker-based Planners (FF, LAMA-FIRST)

These planners require the `planutils` Docker container:

```bash
# Ensure Docker daemon is running
sudo systemctl start docker

# Start the planutils container
docker start planutils
```
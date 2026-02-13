# Multi-Planner PDDL Planning Service

This enhanced Python service supports multiple PDDL planners through a unified REST API interface.

## 🚀 **Supported Planners**

- **ENHSP**: Java-based temporal planner
- **FF**: Fast Forward planner (via Docker container)

## 📋 **Architecture Overview**

```
┌─────────────────┐    HTTP REST API    ┌─────────────────────┐
│   C# Client     │ ──────────────────► │  Python Service     │
│                 │                     │                     │
│ - CallPDDLPlanner│                     │ - Flask REST API    │
│ - RestPlanner   │                     │ - Multi-planner     │
│   Communicator  │                     │   dispatcher        │
└─────────────────┘                     └─────────────────────┘
                                                │
                                                ▼
                                    ┌─────────────────────┐
                                    │   Planner Selection │
                                    │                     │
                                    │ - ENHSP (Java)      │
                                    │ - FF (Docker)       │
                                    └─────────────────────┘
```

## 🔧 **Setup Instructions**

### 1. **ENHSP Setup**
```bash
# ENHSP should be installed at the default path
/home/shermin/ENHSP-Public/enhsp.jar
```

### 2. **FF Planner Setup**
```bash
# Navigate to your planning directory
cd /mnt/c/Users/sherk/Documents/Uni-Stuttgart/ff-planner

# Run container with your files mounted
docker run -it --privileged -v "$(pwd):/workspace" aiplanning/planutils:latest bash

# Activate planutils environment
planutils activate

# Navigate to your mounted files
cd /workspace

# Verify your files are there
ls -la *.pddl

# Test FF planner
ff Domain.pddl Problem.pddl
```

### 3. **Python Service Setup**
```bash
# Navigate to python_service directory
cd python_service

# Install dependencies
pip install flask requests

# Start the service
python pddl_planning_service.py
```

## 📡 **API Usage**

### **Health Check**
```bash
curl http://localhost:5000/health
```

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00.000Z",
  "supported_planners": ["ENHSP", "FF"],
  "default_planner": "ENHSP",
  "enhsp_available": true,
  "domain_file_available": true,
  "problem_file_available": true
}
```

### **Planning Request**

#### **ENHSP Planner**
```bash
curl -X POST http://localhost:5000/plan \
  -H "Content-Type: application/json" \
  -d '{
    "planningType": "PDDL",
    "domainFile": "Plannerinputs/domain.pddl",
    "problemFile": "Plannerinputs/problemC1.pddl",
    "plannerPath": "/home/shermin/ENHSP-Public/enhsp.jar",
    "plannerName": "ENHSP",
    "timeoutSeconds": 60,
    "maxPlanLength": 20
  }'
```

#### **FF Planner**
```bash
curl -X POST http://localhost:5000/plan \
  -H "Content-Type: application/json" \
  -d '{
    "planningType": "PDDL",
    "domainFile": "Plannerinputs/domain.pddl",
    "problemFile": "Plannerinputs/problemC1.pddl",
    "plannerName": "FF",
    "timeoutSeconds": 60,
    "maxPlanLength": 20
  }'
```

## 🔄 **C# Integration**

### **Updated PDDLPlanningRequest**
```csharp
// ENHSP planner
var enhspRequest = new PDDLPlanningRequest(
    "./Plannerinputs/domain.pddl", 
    "./Plannerinputs/problemC1.pddl", 
    "/home/shermin/ENHSP-Public/enhsp.jar", 
    "ENHSP"
);

// FF planner
var ffRequest = new PDDLPlanningRequest(
    "./Plannerinputs/domain.pddl", 
    "./Plannerinputs/problemC1.pddl", 
    "/home/shermin/ENHSP-Public/enhsp.jar",  // Not used for FF
    "FF"
);
```

### **Mixed Planner Usage**
```csharp
// Create different planners for different cassettes
var pddlRequest1 = new PDDLPlanningRequest("./Plannerinputs/domain.pddl", "./Plannerinputs/problemC1.pddl", "/home/shermin/ENHSP-Public/enhsp.jar", "ENHSP");
var pddlRequest2 = new PDDLPlanningRequest("./Plannerinputs/domain.pddl", "./Plannerinputs/problemC2.pddl", "/home/shermin/ENHSP-Public/enhsp.jar", "FF");
var pddlRequest3 = new PDDLPlanningRequest("./Plannerinputs/domain.pddl", "./Plannerinputs/problemC3.pddl", "/home/shermin/ENHSP-Public/enhsp.jar", "ENHSP");
var pddlRequest4 = new PDDLPlanningRequest("./Plannerinputs/domain.pddl", "./Plannerinputs/problemC4.pddl", "/home/shermin/ENHSP-Public/enhsp.jar", "FF");
```

## 🧪 **Testing**

### **Run Test Script**
```bash
cd python_service
python test_ff_planner.py
```

### **Expected Output**
```
🚀 Starting FF Planner Integration Tests
==================================================
🔍 Testing health check...
✅ Health check passed
   Supported planners: ['ENHSP', 'FF']
   Default planner: ENHSP
   ENHSP available: True

🔧 Testing ENHSP planner...
✅ ENHSP planning successful
   Planning time: 2.34 seconds
   Plan length: 8
   Planner used: ENHSP

🔧 Testing FF planner...
✅ FF planning successful
   Planning time: 1.87 seconds
   Plan length: 8
   Planner used: FF

🔧 Testing invalid planner...
✅ Invalid planner correctly rejected

==================================================
📊 TEST SUMMARY
==================================================
Health Check: ✅ PASS
ENHSP Planner: ✅ PASS
FF Planner: ✅ PASS
Invalid Planner: ✅ PASS

🎉 All tests passed! FF planner integration is working.
```

## 🔍 **Troubleshooting**

### **FF Planner Issues**
1. **Docker not running**: Ensure Docker is running
2. **Container not found**: Pull the image: `docker pull aiplanning/planutils:latest`
3. **Permission issues**: Run with `--privileged` flag
4. **File mounting**: Ensure PDDL files are in the correct directory

### **ENHSP Issues**
1. **JAR not found**: Check path `/home/shermin/ENHSP-Public/enhsp.jar`
2. **Java not installed**: Install Java 8 or higher
3. **Memory issues**: Increase JVM heap size

### **Service Issues**
1. **Port conflicts**: Change port in `pddl_planning_service.py`
2. **File permissions**: Ensure PDDL files are readable
3. **Network issues**: Check firewall settings

## 📊 **Performance Comparison**

| Planner | Speed | Memory | Plan Quality | Temporal Support |
|---------|-------|--------|--------------|------------------|
| ENHSP   | Medium| High   | High         | ✅ Yes          |
| FF      | Fast  | Low    | Medium       | ❌ No           |

## 🔮 **Future Enhancements**

- [ ] Add more planners (LAMA, Fast Downward)
- [ ] Planner performance benchmarking
- [ ] Automatic planner selection based on problem characteristics
- [ ] Parallel planning with multiple planners
- [ ] Plan quality metrics and comparison

## 📝 **Configuration**

### **Environment Variables**
```bash
export ENHSP_PATH="/home/shermin/ENHSP-Public/enhsp.jar"
export DEFAULT_PLANNER="ENHSP"
export SERVICE_PORT=5000
```

### **Service Configuration**
```python
# In pddl_planning_service.py
DEFAULT_ENHSP_PATH = "/home/shermin/ENHSP-Public/enhsp.jar"
DEFAULT_PLANNER = "ENHSP"
SUPPORTED_PLANNERS = ["ENHSP", "FF"]
```

## 🤝 **Contributing**

To add a new planner:

1. Add planner name to `SUPPORTED_PLANNERS`
2. Implement `call_<planner_name>()` function
3. Implement `parse_<planner_name>_output()` function
4. Implement `convert_<planner_name>_to_plan_string()` function
5. Add planner selection logic in `create_plan()`
6. Update tests and documentation

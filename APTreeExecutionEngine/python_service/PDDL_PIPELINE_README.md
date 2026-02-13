# PDDL Planning Pipeline

This document describes the complete pipeline for integrating external PDDL planning (ENHSP) with your C# behavior tree system.

## Architecture Overview

```
C# Behavior Tree
    ↓
CallPDDLPlanner (C#)
    ↓
RestPlannerCommunicator (C#)
    ↓
HTTP REST API
    ↓
Python Flask Service (Linux/WSL)
    ↓
ENHSP Planner (/home/Public-ENHSP/enhsp)
```

## Files Created

### C# Files
- `src/Services/AIPlanning/PlanningDataStructures.cs` - Data structures for REST API communication
- `src/Services/AIPlanning/IPlannerCommunicator.cs` - Interface for planner communicators
- `src/Services/AIPlanning/RestPlannerCommunicator.cs` - REST API communicator
- `src/Services/BTServicePlanner.cs` - Updated base class for all planners
- `src/Services/AIPlanning/CallPDDLPlanner.cs` - PDDL-specific planner implementation

### Python Files
- `pddl_planning_service.py` - Flask REST API service that calls ENHSP
- `test_pddl_pipeline.py` - Test script to verify the pipeline

## Setup Instructions

### 1. Linux/WSL Setup

#### Install Python Dependencies
```bash
# Install Flask and requests
pip3 install flask requests

# Or create requirements.txt and install
echo "flask==2.3.3" > requirements.txt
echo "requests==2.31.0" >> requirements.txt
pip3 install -r requirements.txt
```

#### Verify ENHSP Installation
```bash
# Check if ENHSP is available
ls -la /home/Public-ENHSP/enhsp

# Test ENHSP directly
/home/Public-ENHSP/enhsp --help
```

### 2. Start the Python Service

```bash
# Make the script executable
chmod +x pddl_planning_service.py

# Start the service
python3 pddl_planning_service.py
```

You should see:
```
Starting PDDL Planning Service...
ENHSP path: /home/Public-ENHSP/enhsp
ENHSP available: True
 * Running on all addresses (0.0.0.0)
 * Running on http://127.0.0.1:5000
```

### 3. Test the Python Service

```bash
# Test the service
python3 test_pddl_pipeline.py
```

### 4. C# Integration

The C# code is already set up to use the REST API. The `CallPDDLPlanner` will automatically:

1. Create a planning request from your blackboard data
2. Send it to `http://localhost:5000/plan`
3. Receive the planning result
4. Convert it to a `NodeGraph`
5. Store it in the blackboard

## Usage Example

### In Your C# Code

```csharp
// Create a PDDL planner
var pddlPlanner = new CallPDDLPlanner(behaviorTree);

// Use it in a high-level action
var pickUpAction = new PickUp(blackboard, element, location);
pickUpAction.SetAsHighLevelAction(subtree, pddlPlanner);

// The planning will happen automatically when the action is ticked
```

### Custom Configuration

```csharp
// Use custom REST endpoint
var customCommunicator = new RestPlannerCommunicator("http://192.168.1.100:5000");
var pddlPlanner = new CallPDDLPlanner(behaviorTree, customCommunicator);
```

## Data Flow

### 1. C# → Python Request
```json
{
  "planningType": "PDDL",
  "availableActions": [
    {
      "name": "PickUp",
      "parameters": ["element", "location"],
      "preconditions": ["element_at_location", "agent_at_location"],
      "effects": ["holding_element", "not_at_location"]
    }
  ],
  "initialState": {
    "beam1_location": "position1",
    "beam2_location": "position2"
  },
  "goals": [
    "beam1_at_position3",
    "beam2_at_position4"
  ],
  "plannerConfig": {
    "timeoutSeconds": 30,
    "plannerPath": "/home/Public-ENHSP/enhsp"
  }
}
```

### 2. Python → ENHSP
The Python service:
1. Creates PDDL domain and problem files
2. Calls ENHSP with the files
3. Parses ENHSP output

### 3. Python → C# Response
```json
{
  "success": true,
  "plan": {
    "actions": [
      {
        "id": 0,
        "name": "PickUp",
        "parameters": ["robot", "beam1", "position1"],
        "duration": 2.0
      },
      {
        "id": 1,
        "name": "Move",
        "parameters": ["robot", "position1", "position3"],
        "duration": 3.0
      }
    ],
    "orderRelations": [
      {
        "fromActionId": 0,
        "toActionId": 1,
        "relationType": "MEETS"
      }
    ]
  },
  "planningTimeSeconds": 0.5,
  "planLength": 2,
  "plannerUsed": "ENHSP"
}
```

### 4. C# → NodeGraph
The `CallPDDLPlanner` converts the response to a `NodeGraph` with:
- Action nodes from the plan
- Order relations between actions
- Temporal constraints

## Troubleshooting

### Python Service Issues

1. **Service won't start**
   ```bash
   # Check if port 5000 is in use
   netstat -tulpn | grep :5000
   
   # Kill process if needed
   sudo kill -9 <PID>
   ```

2. **ENHSP not found**
   ```bash
   # Check ENHSP path
   ls -la /home/Public-ENHSP/enhsp
   
   # Update path in pddl_planning_service.py if needed
   ENHSP_PATH = "/correct/path/to/enhsp"
   ```

3. **Permission denied**
   ```bash
   # Make ENHSP executable
   chmod +x /home/Public-ENHSP/enhsp
   ```

### C# Integration Issues

1. **Connection refused**
   - Make sure Python service is running
   - Check firewall settings
   - Verify URL in `RestPlannerCommunicator`

2. **Timeout errors**
   - Increase timeout in `RestPlannerCommunicator`
   - Check ENHSP performance
   - Simplify planning problem

3. **Action matching issues**
   - Check action names match between C# and PDDL
   - Verify parameter extraction logic
   - Debug with console output

## Performance Considerations

1. **Network Latency**: REST API adds ~10-50ms overhead
2. **ENHSP Performance**: Complex problems may take seconds
3. **Caching**: Consider caching plans for similar problems
4. **Parallel Planning**: Multiple planners can run simultaneously

## Extending the System

### Add New Planner Types

1. Create new planner class inheriting from `BTServicePlanner`
2. Implement `CreatePlanningRequest()` and `GenerateNodeGraphFromResult()`
3. Add new communicator if needed

### Add New Planning Algorithms

1. Update Python service to support new planners
2. Add planner-specific parsing logic
3. Update request/response data structures

### Add Planning Features

1. **Temporal Planning**: Extend data structures for time constraints
2. **Resource Planning**: Add resource allocation logic
3. **Multi-Agent Planning**: Support multiple agents

## Security Considerations

1. **Network Security**: Use HTTPS in production
2. **Input Validation**: Validate all planning requests
3. **Resource Limits**: Limit planning time and memory usage
4. **Access Control**: Restrict access to planning service

## Monitoring and Logging

The system includes extensive logging:
- C# side: Console.WriteLine with emojis for easy identification
- Python side: Print statements for debugging
- HTTP level: Request/response logging

Monitor these logs to track:
- Planning success/failure rates
- Performance metrics
- Error patterns
- Usage statistics

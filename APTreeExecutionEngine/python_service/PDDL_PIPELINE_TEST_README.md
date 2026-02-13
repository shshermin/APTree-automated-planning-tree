# PDDL Planning Pipeline Test

This document explains how to run the step-by-step PDDL planning pipeline test.

## 🎯 Test Overview

The `TestPDDLPlanningPipeline` function tests the complete PDDL planning pipeline step by step:

1. **Step 1**: Create PDDL Planner
2. **Step 2**: Check REST API Availability  
3. **Step 3**: Test Complete Planning Process via Tick
4. **Step 4**: Verify NodeGraph Storage in Blackboard
5. **Step 5**: Test with BTFlowNode_Dynamic

## 🚀 Prerequisites

### 1. Python Service Setup
Make sure the Python PDDL planning service is running:

```bash
# In your WSL/Linux environment
cd /path/to/your/project
python3 pddl_planning_service.py
```

The service should start on `http://localhost:5000`

### 2. ENHSP Installation
Ensure ENHSP is installed at `/home/Public-ENHSP/enhsp`:

```bash
# Check if ENHSP is available
ls -la /home/Public-ENHSP/enhsp
```

### 3. Neo4j Database
Make sure Neo4j is running and accessible with the configured credentials.

## 🧪 Running the Test

### Option 1: Run the Complete Program
```bash
dotnet run
```

The test will automatically run after the other tests complete.

### Option 2: Test Python Service First
Before running the C# test, verify the Python service works:

```bash
python3 test_pddl_pipeline.py
```

This should show:
```
🚀 Testing PDDL Planning Pipeline
==================================================
Testing health check...
✅ Health check passed: {'status': 'healthy', ...}
Testing planning request...
✅ Planning successful!
```

## 📋 Expected Test Output

### Successful Test Output:
```
🧪 TESTING PDDL PLANNING PIPELINE STEP BY STEP
================================================================================

📋 STEP 1: Creating PDDL Planner
----------------------------------------
✅ PDDL Planner created successfully
   Planner type: CallPDDLPlanner
   Has generated NodeGraph: False

📋 STEP 2: Checking REST API Availability
----------------------------------------
✅ REST API availability check: True

📋 STEP 3: Testing Complete Planning Process via Tick
----------------------------------------
✅ Planning Tick completed: True
   Has generated NodeGraph: True
   Generated NodeGraph actions: 3
   Actions in NodeGraph:
     - PickUp_beam1_position1_robot1
     - Move_robot1_position1_position3
     - Place_beam1_position3_robot1
   Execution order:
     1. PickUp_beam1_position1_robot1
     2. Move_robot1_position1_position3
     3. Place_beam1_position3_robot1

📋 STEP 4: Verifying NodeGraph Storage in Blackboard
----------------------------------------
✅ Total NodeGraphs in blackboard: 1
   NodeGraphs in blackboard:
     - NodeGraph 1: 3 actions

📋 STEP 5: Testing with BTFlowNode_Dynamic
----------------------------------------
✅ BTFlowNode_Dynamic created
✅ Planning service set on flow node
   Testing flow node ticks...
   Tick 1: True (Status: InProgress)
   Tick 2: True (Status: InProgress)
   Tick 3: True (Status: Succeeded)

================================================================================
🎉 PDDL PLANNING PIPELINE TEST COMPLETED SUCCESSFULLY!
================================================================================
```

### Expected Issues and Solutions:

#### 1. REST API Not Available
```
⚠️  WARNING: REST API is not available!
   Make sure to start the Python service: python3 pddl_planning_service.py
```

**Solution**: Start the Python service first.

#### 2. Planning Failed
```
❌ Planning failed
   This might be expected if ENHSP is not installed or the Python service is not running.
```

**Solutions**:
- Check if ENHSP is installed at `/home/Public-ENHSP/enhsp`
- Verify the Python service is running
- Check Python service logs for errors

#### 3. No Actions Available
```
Generated NodeGraph actions: 0
```

**Solution**: Make sure action instances are properly registered in the blackboard.

## 🔧 Troubleshooting

### Python Service Issues
1. **Port already in use**: Change port in `pddl_planning_service.py`
2. **ENHSP not found**: Update `ENHSP_PATH` in the Python service
3. **Permission denied**: Make sure ENHSP is executable

### C# Test Issues
1. **Neo4j connection failed**: Check Neo4j credentials and connection
2. **Missing action instances**: Verify action registration in blackboard
3. **Compilation errors**: Check all required using statements

## 📊 Test Validation

The test validates:
- ✅ PDDL planner creation
- ✅ REST API communication
- ✅ Planning request generation
- ✅ Plan execution via ENHSP
- ✅ NodeGraph generation from plan
- ✅ Blackboard storage
- ✅ Flow node integration
- ✅ Action execution

## 🎯 Next Steps

After successful test completion:
1. Test with different action combinations
2. Test with different planning scenarios
3. Integrate with your specific use cases
4. Test StateChart and GOAP planners similarly

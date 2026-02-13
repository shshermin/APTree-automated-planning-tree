#!/usr/bin/env python3
"""
Test script for PDDL Planning Pipeline
Tests the complete pipeline from C# request to ENHSP response
"""

import requests
import json
import time

def test_health_check():
    """Test health check endpoint"""
    print("Testing health check...")
    try:
        response = requests.get("http://localhost:5000/health")
        if response.status_code == 200:
            data = response.json()
            print(f"✅ Health check passed: {data}")
            return True
        else:
            print(f"❌ Health check failed: {response.status_code}")
            return False
    except Exception as e:
        print(f"❌ Health check error: {e}")
        return False

def test_planning_request():
    """Test planning request"""
    print("\nTesting planning request...")
    
    # Sample planning request (similar to what C# would send)
    request_data = {
        "planningType": "PDDL",
        "availableActions": [
            "PickUp_beam1_position1_robot1",
            "Place_beam1_position3_robot1", 
            "Move_robot1_position1_position3"
        ],
        "initialState": {
            "beam1_location": "position1",
            "beam2_location": "position2",
            "current_time": "2024-01-01T12:00:00",
            "planning_requested": True
        },
        "goals": [
            "beam1_at_position3",
            "beam2_at_position4"
        ],
        "plannerConfig": {
            "timeoutSeconds": 30,
            "maxPlanLength": 20,
            "domainFile": "robot_domain.pddl",
            "problemFile": "robot_problem.pddl",
            "plannerPath": "/home/Public-ENHSP/enhsp"
        }
    }
    
    try:
        print(f"Sending request: {json.dumps(request_data, indent=2)}")
        
        response = requests.post(
            "http://localhost:5000/plan",
            json=request_data,
            headers={"Content-Type": "application/json"},
            timeout=60
        )
        
        print(f"Response status: {response.status_code}")
        
        if response.status_code == 200:
            data = response.json()
            print(f"✅ Planning successful!")
            print(f"Planning time: {data.get('planningTimeSeconds', 'N/A')} seconds")
            print(f"Plan length: {data.get('planLength', 'N/A')} actions")
            print(f"Planner used: {data.get('plannerUsed', 'N/A')}")
            
            if data.get('plan'):
                plan_string = data['plan']
                print(f"Plan string:")
                print(plan_string)
            
            return True
        else:
            print(f"❌ Planning failed: {response.status_code}")
            try:
                error_data = response.json()
                print(f"Error: {json.dumps(error_data, indent=2)}")
            except:
                print(f"Error text: {response.text}")
            return False
            
    except Exception as e:
        print(f"❌ Planning request error: {e}")
        return False

def main():
    """Main test function"""
    print("🚀 Testing PDDL Planning Pipeline")
    print("=" * 50)
    
    # Test health check
    if not test_health_check():
        print("\n❌ Health check failed. Make sure the Python service is running.")
        print("Run: python3 pddl_planning_service.py")
        return
    
    # Test planning request
    if not test_planning_request():
        print("\n❌ Planning request failed.")
        return
    
    print("\n✅ All tests passed! The pipeline is working correctly.")
    print("\nNext steps:")
    print("1. Make sure ENHSP is installed at /home/Public-ENHSP/enhsp")
    print("2. Test the C# integration")
    print("3. Verify the generated NodeGraph in your behavior tree")

if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""
Test script for LAMA-first planner
"""

import requests
import json

def test_lama_first_planner():
    """Test the LAMA-first planner endpoint"""
    
    # Test data
    test_data = {
        "planningType": "PDDL",
        "domainFile": "Plannerinputs/domain.pddl",
        "problemFile": "Plannerinputs/problemC1.pddl",
        "plannerName": "LAMA-FIRST",
        "timeoutSeconds": 120,
        "maxPlanLength": 20
    }
    
    print("🧪 Testing LAMA-first planner...")
    print(f"Request data: {json.dumps(test_data, indent=2)}")
    
    try:
        # Make request to the planning service
        response = requests.post(
            "http://localhost:5000/plan",
            json=test_data,
            timeout=130  # Slightly longer than the service timeout
        )
        
        print(f"Response status: {response.status_code}")
        print(f"Response headers: {dict(response.headers)}")
        
        if response.status_code == 200:
            result = response.json()
            print("✅ LAMA-first planner test successful!")
            print(f"Success: {result.get('success')}")
            print(f"Planning time: {result.get('planningTimeSeconds')} seconds")
            print(f"Plan length: {result.get('planLength')}")
            print(f"Planner used: {result.get('plannerUsed')}")
            print(f"Plan:\n{result.get('plan')}")
        else:
            print("❌ LAMA-first planner test failed!")
            print(f"Error: {response.text}")
            
    except requests.exceptions.RequestException as e:
        print(f"❌ Request failed: {e}")
    except Exception as e:
        print(f"❌ Unexpected error: {e}")

def test_health_check():
    """Test the health check endpoint to see if LAMA-FIRST is supported"""
    
    print("🏥 Testing health check...")
    
    try:
        response = requests.get("http://localhost:5000/health", timeout=10)
        
        if response.status_code == 200:
            health = response.json()
            print("✅ Health check successful!")
            print(f"Supported planners: {health.get('supported_planners')}")
            print(f"Default planner: {health.get('default_planner')}")
            
            if "LAMA-FIRST" in health.get('supported_planners', []):
                print("✅ LAMA-FIRST is supported!")
            else:
                print("❌ LAMA-FIRST is not supported!")
        else:
            print(f"❌ Health check failed: {response.text}")
            
    except requests.exceptions.RequestException as e:
        print(f"❌ Health check request failed: {e}")
    except Exception as e:
        print(f"❌ Unexpected error: {e}")

if __name__ == "__main__":
    print("🚀 Starting LAMA-first planner tests...")
    print("=" * 50)
    
    # Test health check first
    test_health_check()
    print()
    
    # Test LAMA-first planner
    test_lama_first_planner()
    
    print("=" * 50)
    print("🏁 Tests completed!")

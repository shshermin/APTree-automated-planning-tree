#!/usr/bin/env python3
"""
Test script for FF planner integration
Tests the enhanced Python service with both ENHSP and FF planners
"""

import requests
import json
import time

# Service configuration
SERVICE_URL = "http://localhost:5000"

def test_health_check():
    """Test the health check endpoint"""
    print("🔍 Testing health check...")
    try:
        response = requests.get(f"{SERVICE_URL}/health")
        if response.status_code == 200:
            data = response.json()
            print(f"✅ Health check passed")
            print(f"   Supported planners: {data.get('supported_planners', [])}")
            print(f"   Default planner: {data.get('default_planner', 'Unknown')}")
            print(f"   ENHSP available: {data.get('enhsp_available', False)}")
            return True
        else:
            print(f"❌ Health check failed: {response.status_code}")
            return False
    except Exception as e:
        print(f"❌ Health check error: {e}")
        return False

def test_enhsp_planner():
    """Test ENHSP planner"""
    print("\n🔧 Testing ENHSP planner...")
    
    request_data = {
        "planningType": "PDDL",
        "domainFile": "Plannerinputs/domain.pddl",
        "problemFile": "Plannerinputs/problemC1.pddl",
        "plannerPath": "/home/shermin/ENHSP-Public/enhsp.jar",
        "plannerName": "ENHSP",
        "timeoutSeconds": 60,
        "maxPlanLength": 20
    }
    
    try:
        response = requests.post(f"{SERVICE_URL}/plan", json=request_data)
        print(f"Response status: {response.status_code}")
        
        if response.status_code == 200:
            data = response.json()
            if data.get('success'):
                print(f"✅ ENHSP planning successful")
                print(f"   Planning time: {data.get('planningTimeSeconds', 0):.2f} seconds")
                print(f"   Plan length: {data.get('planLength', 0)}")
                print(f"   Planner used: {data.get('plannerUsed', 'Unknown')}")
                print(f"   Plan preview: {data.get('plan', '')[:200]}...")
                return True
            else:
                print(f"❌ ENHSP planning failed: {data.get('error', 'Unknown error')}")
                return False
        else:
            print(f"❌ ENHSP request failed: {response.status_code}")
            print(f"   Response: {response.text}")
            return False
    except Exception as e:
        print(f"❌ ENHSP test error: {e}")
        return False

def test_ff_planner():
    """Test FF planner"""
    print("\n🔧 Testing FF planner...")
    
    request_data = {
        "planningType": "PDDL",
        "domainFile": "Plannerinputs/domain.pddl",
        "problemFile": "Plannerinputs/problemC1.pddl",
        "plannerPath": "/home/shermin/ENHSP-Public/enhsp.jar",  # Not used for FF but kept for compatibility
        "plannerName": "FF",
        "timeoutSeconds": 60,
        "maxPlanLength": 20
    }
    
    try:
        response = requests.post(f"{SERVICE_URL}/plan", json=request_data)
        print(f"Response status: {response.status_code}")
        
        if response.status_code == 200:
            data = response.json()
            if data.get('success'):
                print(f"✅ FF planning successful")
                print(f"   Planning time: {data.get('planningTimeSeconds', 0):.2f} seconds")
                print(f"   Plan length: {data.get('planLength', 0)}")
                print(f"   Planner used: {data.get('plannerUsed', 'Unknown')}")
                print(f"   Plan preview: {data.get('plan', '')[:200]}...")
                return True
            else:
                print(f"❌ FF planning failed: {data.get('error', 'Unknown error')}")
                return False
        else:
            print(f"❌ FF request failed: {response.status_code}")
            print(f"   Response: {response.text}")
            return False
    except Exception as e:
        print(f"❌ FF test error: {e}")
        return False

def test_invalid_planner():
    """Test invalid planner selection"""
    print("\n🔧 Testing invalid planner...")
    
    request_data = {
        "planningType": "PDDL",
        "domainFile": "Plannerinputs/domain.pddl",
        "problemFile": "Plannerinputs/problemC1.pddl",
        "plannerName": "INVALID_PLANNER",
        "timeoutSeconds": 60
    }
    
    try:
        response = requests.post(f"{SERVICE_URL}/plan", json=request_data)
        print(f"Response status: {response.status_code}")
        
        if response.status_code == 400:
            data = response.json()
            print(f"✅ Invalid planner correctly rejected")
            print(f"   Error: {data.get('error', {}).get('message', 'Unknown error')}")
            return True
        else:
            print(f"❌ Invalid planner not properly rejected: {response.status_code}")
            return False
    except Exception as e:
        print(f"❌ Invalid planner test error: {e}")
        return False

def main():
    """Run all tests"""
    print("🚀 Starting FF Planner Integration Tests")
    print("=" * 50)
    
    # Test health check
    if not test_health_check():
        print("❌ Health check failed, stopping tests")
        return
    
    # Test ENHSP planner
    enhsp_success = test_enhsp_planner()
    
    # Test FF planner
    ff_success = test_ff_planner()
    
    # Test invalid planner
    invalid_success = test_invalid_planner()
    
    # Summary
    print("\n" + "=" * 50)
    print("📊 TEST SUMMARY")
    print("=" * 50)
    print(f"Health Check: {'✅ PASS' if True else '❌ FAIL'}")
    print(f"ENHSP Planner: {'✅ PASS' if enhsp_success else '❌ FAIL'}")
    print(f"FF Planner: {'✅ PASS' if ff_success else '❌ FAIL'}")
    print(f"Invalid Planner: {'✅ PASS' if invalid_success else '❌ FAIL'}")
    
    if enhsp_success and ff_success and invalid_success:
        print("\n🎉 All tests passed! FF planner integration is working.")
    else:
        print("\n⚠️ Some tests failed. Check the output above for details.")

if __name__ == "__main__":
    main()

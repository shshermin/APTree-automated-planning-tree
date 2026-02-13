#!/usr/bin/env python3
"""
PDDL Planning Service
REST API service that calls multiple PDDL planners (ENHSP, FF)
"""

from flask import Flask, request, jsonify
import subprocess
import tempfile
import os
import json
import time
import shutil
from datetime import datetime

app = Flask(__name__)

# Configuration - these will be overridden by request parameters
DEFAULT_ENHSP_PATH = "/home/shermin/ENHSP-Public/enhsp.jar"  # Default path to ENHSP JAR file

# Get the directory where this script is located
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
# The PDDL files are in the same directory as this script
DEFAULT_DOMAIN_FILE_PATH = os.path.join(SCRIPT_DIR, "Plannerinputs/domain.pddl")  # Default path to domain file
DEFAULT_PROBLEM_FILE_PATH = os.path.join(SCRIPT_DIR, "Plannerinputs/problemC1.pddl")  # Default path to problem file
DEFAULT_TIMEOUT_SECONDS = 120
DEFAULT_PLANNER = "ENHSP"  # Default planner to use

# Supported planners
SUPPORTED_PLANNERS = ["ENHSP", "FF", "LAMA-FIRST"]

@app.route('/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    return jsonify({
        "status": "healthy",
        "timestamp": datetime.now().isoformat(),
        "supported_planners": SUPPORTED_PLANNERS,
        "default_planner": DEFAULT_PLANNER,
        "enhsp_path": DEFAULT_ENHSP_PATH,
        "enhsp_available": os.path.exists(DEFAULT_ENHSP_PATH),
        "domain_file_available": os.path.exists(DEFAULT_DOMAIN_FILE_PATH),
        "problem_file_available": os.path.exists(DEFAULT_PROBLEM_FILE_PATH)
    })

@app.route('/plan', methods=['POST'])
def create_plan():
    """Main planning endpoint"""
    try:
        # Parse request
        data = request.json
        print(f"Received planning request: {json.dumps(data, indent=2)}")
        
        # Extract data - handle both old and new request formats
        planning_type = data.get('planningType', 'PDDL')
        
        # Extract PDDL-specific properties (new format)
        domain_file_path = data.get('domainFile', DEFAULT_DOMAIN_FILE_PATH)
        problem_file_path = data.get('problemFile', DEFAULT_PROBLEM_FILE_PATH)
        planner_path = data.get('plannerPath', DEFAULT_ENHSP_PATH)
        timeout_seconds = data.get('timeoutSeconds', DEFAULT_TIMEOUT_SECONDS)
        max_plan_length = data.get('maxPlanLength', 20)
        planner_name = data.get('plannerName', DEFAULT_PLANNER).upper()  # New: planner selection
        
        # Extract legacy properties (old format) for backward compatibility
        available_actions = data.get('availableActions', [])
        initial_state = data.get('initialState', {})
        goals = data.get('goals', [])
        planner_config = data.get('plannerConfig', {})
        
        # Use planner_config values if not specified in new format
        if planner_config:
            if not domain_file_path or domain_file_path == DEFAULT_DOMAIN_FILE_PATH:
                domain_file_path = planner_config.get('domainFile', domain_file_path)
            if not problem_file_path or problem_file_path == DEFAULT_PROBLEM_FILE_PATH:
                problem_file_path = planner_config.get('problemFile', problem_file_path)
            if not planner_path or planner_path == DEFAULT_ENHSP_PATH:
                planner_path = planner_config.get('plannerPath', planner_path)
            if timeout_seconds == DEFAULT_TIMEOUT_SECONDS:
                timeout_seconds = planner_config.get('timeoutSeconds', timeout_seconds)
            if planner_name == DEFAULT_PLANNER:
                planner_name = planner_config.get('plannerName', planner_name).upper()
        
        # Convert relative paths to absolute paths if they start with "Plannerinputs/"
        if domain_file_path.startswith("Plannerinputs/"):
            domain_file_path = os.path.join(SCRIPT_DIR, domain_file_path)
        if problem_file_path.startswith("Plannerinputs/"):
            problem_file_path = os.path.join(SCRIPT_DIR, problem_file_path)
        
        # Log extracted properties
        print(f"Extracted PDDL properties:")
        print(f"  - Domain file: {domain_file_path}")
        print(f"  - Problem file: {problem_file_path}")
        print(f"  - Planner path: {planner_path}")
        print(f"  - Planner name: {planner_name}")
        print(f"  - Timeout: {timeout_seconds} seconds")
        print(f"  - Max plan length: {max_plan_length}")
        
        if planning_type != 'PDDL':
            return jsonify({
                'success': False,
                'error': {
                    'code': 'UNSUPPORTED_PLANNING_TYPE',
                    'message': f'Planning type {planning_type} not supported',
                    'details': 'Only PDDL planning is currently supported'
                }
            }), 400
        
        # Validate planner selection
        if planner_name not in SUPPORTED_PLANNERS:
            return jsonify({
                'success': False,
                'error': {
                    'code': 'UNSUPPORTED_PLANNER',
                    'message': f'Planner {planner_name} not supported',
                    'details': f'Supported planners: {", ".join(SUPPORTED_PLANNERS)}'
                }
            }), 400
        
      
        
        # Check if domain and problem files exist
        if not os.path.exists(domain_file_path):
            return jsonify({
                'success': False,
                'error': {
                    'code': 'DOMAIN_FILE_NOT_FOUND',
                    'message': 'Domain file not found',
                    'details': f'Domain file not found at {domain_file_path}'
                }
            }), 500
            
        if not os.path.exists(problem_file_path):
            return jsonify({
                'success': False,
                'error': {
                    'code': 'PROBLEM_FILE_NOT_FOUND',
                    'message': 'Problem file not found',
                    'details': f'Problem file not found at {problem_file_path}'
                }
            }), 500
        
        # Use the original files directly (no temporary copies needed)
        domain_file = domain_file_path
        problem_file = problem_file_path
        
        # Call appropriate planner based on selection
        start_time = time.time()
        
        if planner_name == "ENHSP":
            # Check if ENHSP is available
            if not os.path.exists(planner_path):
                return jsonify({
                    'success': False,
                    'error': {
                        'code': 'ENHSP_NOT_FOUND',
                        'message': 'ENHSP planner not found',
                        'details': f'ENHSP not found at {planner_path}'
                    }
                }), 500
            
            plan_result = call_enhsp(domain_file, problem_file, planner_path, timeout_seconds)
        elif planner_name == "FF":
            plan_result = call_ff(domain_file, problem_file, timeout_seconds)
        elif planner_name == "LAMA-FIRST":
            plan_result = call_lama_first(domain_file, problem_file, timeout_seconds)
        else:
            return jsonify({
                'success': False,
                'error': {
                    'code': 'UNSUPPORTED_PLANNER',
                    'message': f'Planner {planner_name} not implemented',
                    'details': f'Supported planners: {", ".join(SUPPORTED_PLANNERS)}'
                }
            }), 400
        
        planning_time = time.time() - start_time
        
        if plan_result['success']:
            # Convert planner output to plan string format
            if planner_name == "ENHSP":
                plan_string = convert_enhsp_to_plan_string(plan_result['plan'])
            elif planner_name == "FF":
                plan_string = convert_ff_to_plan_string(plan_result['plan'])
            elif planner_name == "LAMA-FIRST":
                plan_string = convert_lama_first_to_plan_string(plan_result['plan'])
            else:
                plan_string = str(plan_result['plan'])
            
            return jsonify({
                'success': True,
                'plan': plan_string,
                'planningTimeSeconds': planning_time,
                'planLength': len(plan_result['plan']),
                'plannerUsed': planner_name
            })
        else:
            return jsonify({
                'success': False,
                'error': f'{planner_name} failed to find a plan: {plan_result["error"]}',
                'planningTimeSeconds': planning_time,
                'plannerUsed': planner_name
            })
            
    except Exception as e:
        print(f"Error in planning request: {str(e)}")
        return jsonify({
            'success': False,
            'error': f'Unexpected error during planning: {str(e)}'
        }), 500

def copy_file_to_temp(source_path, prefix):
    """Copy a file to a temporary location"""
    fd, temp_filename = tempfile.mkstemp(suffix='.pddl', prefix=prefix)
    with os.fdopen(fd, 'w') as temp_file:
        with open(source_path, 'r') as source_file:
            temp_file.write(source_file.read())
    
    print(f"Copied {source_path} to temporary file: {temp_filename}")
    return temp_filename

def call_enhsp(domain_file, problem_file, planner_path, timeout_seconds):
    """Call ENHSP planner"""
    try:
        # Build ENHSP command (Java application)
        cmd = [
            'java', '-jar', planner_path,
            '-o', domain_file,
            '-f', problem_file,
            '-planner', 'pt-blind'  # Use pt-blind configuration
        ]
        
        print(f"Calling ENHSP with command: {' '.join(cmd)}")
        
        # Run ENHSP
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=timeout_seconds
        )
        
        print(f"ENHSP stdout: {result.stdout}")
        print(f"ENHSP stderr: {result.stderr}")
        
        if result.returncode == 0:
            # Parse ENHSP output
            plan = parse_enhsp_output(result.stdout)
            return {'success': True, 'plan': plan}
        else:
            return {
                'success': False, 
                'error': f'ENHSP failed with return code {result.returncode}: {result.stderr}'
            }
            
    except subprocess.TimeoutExpired:
        return {'success': False, 'error': 'ENHSP planning timed out'}
    except Exception as e:
        return {'success': False, 'error': f'Error calling ENHSP: {str(e)}'}

def call_ff(domain_file, problem_file, timeout_seconds):
    """Call FF planner using existing Docker container"""
    try:
        print("🔍 Using existing Docker container: stupefied_hellman")
        
        # Get the domain and problem file names (without path)
        domain_filename = os.path.basename(domain_file)
        problem_filename = os.path.basename(problem_file)
        
        print(f"🔍 Domain file: {domain_filename}")
        print(f"🔍 Problem file: {problem_filename}")
        
        # Get the original file names (not the temp names)
        # We need to extract the original names from the temp file names
        # The temp files are like: /tmp/domain_xxx.pddl and /tmp/problem_xxx.pddl
        # But we want the original names like: DomainML.pddl and problemPickUpHL_xxx.pddl
        # For now, let's use the temp file names as they should work with the FF planner
        original_domain_name = domain_filename
        original_problem_name = problem_filename
         
        # Copy the PDDL files to the Docker container's root directory with original names
        # Construct full absolute paths by combining base directory with filenames
        base_directory = "/mnt/c/Users/sherk/Documents/BehaviorTreeMainProject/BehaviorTreeMainProject/python_service/Plannerinputs"
        
        copy_domain_cmd = [
            'docker', 'cp', f'{base_directory}/{original_domain_name}', f'stupefied_hellman:/root/{original_domain_name}'
        ]
        copy_problem_cmd = [
            'docker', 'cp', f'{base_directory}/{original_problem_name}', f'stupefied_hellman:/root/{original_problem_name}'
        ]
        
        print(f"🔍 DEBUG: Base directory: {base_directory}")
        print(f"🔍 DEBUG: Domain filename: {original_domain_name}")
        print(f"🔍 DEBUG: Problem filename: {original_problem_name}")
        print(f"🔍 DEBUG: Full domain path: {base_directory}/{original_domain_name}")
        print(f"🔍 DEBUG: Full problem path: {base_directory}/{original_problem_name}")
        
        print(f"Copying domain file: {' '.join(copy_domain_cmd)}")
        copy_domain_result = subprocess.run(copy_domain_cmd, capture_output=True, text=True, timeout=30)
        if copy_domain_result.returncode != 0:
            print(f"⚠️ Warning: Failed to copy domain file: {copy_domain_result.stderr}")
        else:
            print(f"✅ Domain file copied successfully to /root/{original_domain_name}")

        print(f"Copying problem file: {' '.join(copy_problem_cmd)}")
        copy_problem_result = subprocess.run(copy_problem_cmd, capture_output=True, text=True, timeout=30)
        if copy_problem_result.returncode != 0:
            print(f"⚠️ Warning: Failed to copy problem file: {copy_problem_result.stderr}")
        else:
            print(f"✅ Problem file copied successfully to /root/{original_problem_name}")

        # VERIFICATION: Check what files exist in /root directory
        print(f"🔍 VERIFICATION: Checking files in /root directory...")
        ls_root_cmd = ['docker', 'exec', 'stupefied_hellman', 'ls', '-la', '/root/']
        ls_root_result = subprocess.run(ls_root_cmd, capture_output=True, text=True, timeout=30)
        print(f"Files in /root directory:")
        print(ls_root_result.stdout)
        
        # Execute the FF planning command in the Docker container
        # Use planutils run ff which is more reliable than just ff
        ff_cmd = [
            'docker', 'exec', 'stupefied_hellman',
            'bash', '-c',
            f'planutils activate && planutils run ff {original_domain_name} {original_problem_name}'
        ]
        
        print(f"Calling FF with command: {' '.join(ff_cmd)}")
        
        # Run FF
        result = subprocess.run(
            ff_cmd,
            capture_output=True,
            text=True,
            timeout=timeout_seconds
        )
        
        print(f"FF stdout: {result.stdout}")
        print(f"FF stderr: {result.stderr}")
        print(f"🔍 DEBUG: FF return code: {result.returncode}")
        print(f"🔍 DEBUG: FF stdout length: {len(result.stdout)}")
        print(f"🔍 DEBUG: FF stdout preview: {repr(result.stdout[:500])}")
        
        if result.returncode == 0:
            # Parse FF output
            plan = parse_ff_output(result.stdout)
            return {'success': True, 'plan': plan}
        else:
            return {
                'success': False, 
                'error': f'FF failed with return code {result.returncode}: {result.stderr}'
            }
            
    except subprocess.TimeoutExpired:
        return {'success': False, 'error': 'FF planning timed out'}
    except Exception as e:
        return {'success': False, 'error': f'Error calling FF: {str(e)}'}

def call_lama_first(domain_file, problem_file, timeout_seconds):
    """Call LAMA-first planner using existing Docker container"""
    try:
        print("🔍 Using existing Docker container: stupefied_hellman")
        
        # Get the domain and problem file names (without path)
        domain_filename = os.path.basename(domain_file)
        problem_filename = os.path.basename(problem_file)
        
        print(f"🔍 Domain file: {domain_filename}")
        print(f"🔍 Problem file: {problem_filename}")
        
        # Get the original file names (not the temp names)
        # We need to extract the original names from the temp file names
        # The temp files are like: /tmp/domain_xxx.pddl and /tmp/problem_xxx.pddl
        # But we want the original names like: DomainML.pddl and problemPickUpHL_xxx.pddl
        # For now, let's use the temp file names as they should work with the LAMA-first planner
        original_domain_name = domain_filename
        original_problem_name = problem_filename
         
        # Copy the PDDL files to the Docker container's root directory with original names
        # Construct full absolute paths by combining base directory with filenames
        base_directory = "/mnt/c/Users/sherk/Documents/BehaviorTreeMainProject/BehaviorTreeMainProject/python_service/Plannerinputs"
        
        copy_domain_cmd = [
            'docker', 'cp', f'{base_directory}/{original_domain_name}', f'stupefied_hellman:/root/{original_domain_name}'
        ]
        copy_problem_cmd = [
            'docker', 'cp', f'{base_directory}/{original_problem_name}', f'stupefied_hellman:/root/{original_problem_name}'
        ]
        
        print(f"🔍 DEBUG: Base directory: {base_directory}")
        print(f"🔍 DEBUG: Domain filename: {original_domain_name}")
        print(f"🔍 DEBUG: Problem filename: {original_problem_name}")
        print(f"🔍 DEBUG: Full domain path: {base_directory}/{original_domain_name}")
        print(f"🔍 DEBUG: Full problem path: {base_directory}/{original_problem_name}")
        
        print(f"Copying domain file: {' '.join(copy_domain_cmd)}")
        copy_domain_result = subprocess.run(copy_domain_cmd, capture_output=True, text=True, timeout=30)
        if copy_domain_result.returncode != 0:
            print(f"⚠️ Warning: Failed to copy domain file: {copy_domain_result.stderr}")
        else:
            print(f"✅ Domain file copied successfully to /root/{original_domain_name}")

        print(f"Copying problem file: {' '.join(copy_problem_cmd)}")
        copy_problem_result = subprocess.run(copy_problem_cmd, capture_output=True, text=True, timeout=30)
        if copy_problem_result.returncode != 0:
            print(f"⚠️ Warning: Failed to copy problem file: {copy_problem_result.stderr}")
        else:
            print(f"✅ Problem file copied successfully to /root/{original_problem_name}")

        # VERIFICATION: Check what files exist in /root directory
        print(f"🔍 VERIFICATION: Checking files in /root directory...")
        ls_root_cmd = ['docker', 'exec', 'stupefied_hellman', 'ls', '-la', '/root/']
        ls_root_result = subprocess.run(ls_root_cmd, capture_output=True, text=True, timeout=30)
        print(f"Files in /root directory:")
        print(ls_root_result.stdout)
        
        # Execute the LAMA-first planning command in the Docker container
        # Use planutils run lama-first which is more reliable than just lama-first
        lama_first_cmd = [
            'docker', 'exec', 'stupefied_hellman',
            'bash', '-c',
            f'planutils activate && planutils run lama-first {original_domain_name} {original_problem_name}'
        ]
        
        print(f"Calling LAMA-first with command: {' '.join(lama_first_cmd)}")
        
        # Run LAMA-first
        result = subprocess.run(
            lama_first_cmd,
            capture_output=True,
            text=True,
            timeout=timeout_seconds
        )
        
        print(f"LAMA-first stdout: {result.stdout}")
        print(f"LAMA-first stderr: {result.stderr}")
        print(f"🔍 DEBUG: LAMA-first return code: {result.returncode}")
        print(f"🔍 DEBUG: LAMA-first stdout length: {len(result.stdout)}")
        print(f"🔍 DEBUG: LAMA-first stdout preview: {repr(result.stdout[:500])}")
        
        if result.returncode == 0:
            # Parse LAMA-first output
            plan = parse_lama_first_output(result.stdout)
            return {'success': True, 'plan': plan}
        else:
            return {
                'success': False, 
                'error': f'LAMA-first failed with return code {result.returncode}: {result.stderr}'
            }
            
    except subprocess.TimeoutExpired:
        return {'success': False, 'error': 'LAMA-first planning timed out'}
    except Exception as e:
        return {'success': False, 'error': f'Error calling LAMA-first: {str(e)}'}

def parse_enhsp_output(output):
    """Parse ENHSP output to extract plan"""
    plan = []
    lines = output.split('\n')
    
    for line in lines:
        line = line.strip()
        # Look for lines with timestamp and action like "0.0: (grab lp1 fp1 r1)"
        if ':' in line and '(' in line and line.endswith(')'):
            # Extract the part after the colon and before the parentheses
            colon_index = line.find(':')
            if colon_index != -1:
                action_part = line[colon_index + 1:].strip()
                if action_part.startswith('(') and action_part.endswith(')'):
                    # Parse action like "(grab lp1 fp1 r1)"
                    action_str = action_part[1:-1]  # Remove parentheses
                    parts = action_str.split()
                    
                    if len(parts) >= 2:
                        action_name = parts[0]
                        parameters = parts[1:]
                        
                        plan.append({
                            'name': action_name,
                            'parameters': parameters
                        })
    
    return plan

def parse_ff_output(output):
    """Parse FF output to extract plan"""
    plan = []
    lines = output.split('\n')
    
    print(f"🔍 DEBUG: Raw FF output length: {len(output)}")
    print(f"🔍 DEBUG: Raw FF output preview: {repr(output[:200])}")
    print(f"🔍 DEBUG: Number of lines: {len(lines)}")
    
    for i, line in enumerate(lines):
        line = line.strip()
        print(f"🔍 DEBUG: Line {i}: '{line}'")
        
        # Look for FF action lines like:
        # "step    0: TRAVELML R1 PR2 EP1"
        # "1: EQUIPEML R1 VG EP1"
        # "2: INITIALIZEML R1 VG"
        if ':' in line and (line.startswith('step ') or (line.strip() and line.strip()[0].isdigit() and ':' in line)):
            print(f"🔍 DEBUG: Found action line: '{line}'")
            # Extract the part after the colon
            colon_index = line.find(':')
            if colon_index != -1:
                action_part = line[colon_index + 1:].strip()
                parts = action_part.split()
                
                print(f"🔍 DEBUG: Action part: '{action_part}', parts: {parts}")
                
                if len(parts) >= 1:  # Changed from 2 to 1 since some actions might have no parameters
                    action_name = parts[0]
                    parameters = parts[1:] if len(parts) > 1 else []
                    
                    print(f"🔍 DEBUG: Parsed action: name='{action_name}', parameters={parameters}")
                    
                    plan.append({
                        'name': action_name,
                        'parameters': parameters
                    })
    
    print(f"🔍 DEBUG: Final parsed plan: {plan}")
    return plan

def parse_lama_first_output(output):
    """Parse LAMA-first output to extract plan"""
    plan = []
    lines = output.split('\n')
    
    print(f"🔍 DEBUG: Raw LAMA-first output length: {len(output)}")
    print(f"🔍 DEBUG: Raw LAMA-first output preview: {repr(output[:200])}")
    print(f"🔍 DEBUG: Number of lines: {len(lines)}")
    
    for i, line in enumerate(lines):
        line = line.strip()
        print(f"🔍 DEBUG: Line {i}: '{line}'")
        
        # Look for LAMA-first action lines like:
        # "travelml r1 pr3 fp9 (1)"
        # "pickupml lp2 fp9 r1 vg1 (1)"
        # These lines contain action names and parameters, followed by cost in parentheses
        # Skip lines that are clearly not action lines
        if (line and 
            not line.startswith('[') and 
            not line.startswith('INFO') and 
            not line.startswith('Solution found') and 
            not line.startswith('Peak memory') and 
            not line.startswith('Remove intermediate') and 
            not line.startswith('search exit code') and 
            not line.startswith('time') and 
            not line.startswith('g=') and 
            not line.startswith('New best') and 
            not line.startswith('Initial') and 
            not line.startswith('Conducting') and 
            not line.startswith('Plan length') and 
            not line.startswith('Plan cost') and 
            not line.startswith('Expanded') and 
            not line.startswith('Reopened') and 
            not line.startswith('Evaluated') and 
            not line.startswith('Evaluations') and 
            not line.startswith('Generated') and 
            not line.startswith('Dead ends') and 
            not line.startswith('Number of registered') and 
            not line.startswith('Int hash') and 
            not line.startswith('Search time') and 
            not line.startswith('Total time') and 
            not line.startswith('reading input') and 
            not line.startswith('done reading') and 
            not line.startswith('Initializing') and 
            not line.startswith('Generating') and 
            not line.startswith('Building') and 
            not line.startswith('Variables') and 
            not line.startswith('FactPairs') and 
            not line.startswith('Bytes per state') and 
            not line.startswith('Simplifying') and 
            not line.startswith('time to simplify') and 
            not line.startswith('peak memory') and 
            not line.startswith('time for successor') and 
            not line.startswith('Landmarks generation') and 
            not line.startswith('Discovered') and 
            not line.startswith('edges') and 
            not line.startswith('approx. reasonable') and 
            not line.startswith('Landmark graph') and 
            not line.startswith('Landmark graph contains') and 
            not line.startswith('Landmark graph generation') and 
            not line.startswith('Actual search')):
            
            # Check if this looks like an action line (contains action name and parameters, ends with cost in parentheses)
            if '(' in line and ')' in line and not line.startswith('['):
                print(f"🔍 DEBUG: Found potential action line: '{line}'")
                
                # Extract the part before the cost (everything before the last occurrence of " (")
                cost_start = line.rfind(' (')
                if cost_start != -1:
                    action_part = line[:cost_start].strip()
                    parts = action_part.split()
                    
                    print(f"🔍 DEBUG: Action part: '{action_part}', parts: {parts}")
                    
                    if len(parts) >= 1:
                        action_name = parts[0]
                        parameters = parts[1:] if len(parts) > 1 else []
                        
                        print(f"🔍 DEBUG: Parsed action: name='{action_name}', parameters={parameters}")
                        
                        plan.append({
                            'name': action_name,
                            'parameters': parameters
                        })
    
    print(f"🔍 DEBUG: Final parsed plan: {plan}")
    return plan

def convert_enhsp_to_plan_string(enhsp_plan):
    """Convert ENHSP plan to plan string format (like NodeGraphGenerated.txt)"""
    plan_lines = []
    action_instances = []  # Store full action instance names for relations
    action_names = []  # Store simplified action names for relations
    
    # Add action instances
    for i, action in enumerate(enhsp_plan):
        # Convert ENHSP action to action string format
        action_name = normalize_action_name(action['name'])  # Normalize case to match C# expectations
        parameters = action['parameters']
        
        # Create action string like "PickUpHL_lp4_fp25_r1"
        action_string = f"{action_name}_{'_'.join(parameters)}"
        full_action_instance = f"ActionInstance: {action_string}"
        plan_lines.append(full_action_instance)
        action_instances.append(full_action_instance)  # Store for relations
        action_names.append(action_string)  # Store simplified name for relations
    
    # Add sequential relations using simplified action names (without ActionInstance: prefix)
    for i in range(len(action_names) - 1):
        action1_name = action_names[i]
        action2_name = action_names[i + 1]
        plan_lines.append(f"{action1_name} --[MEETS]--> {action2_name}")
    
    return '\n'.join(plan_lines)

def convert_ff_to_plan_string(ff_plan):
    """Convert FF plan to plan string format (like NodeGraphGenerated.txt)"""
    plan_lines = []
    action_instances = []  # Store full action instance names for relations
    action_names = []  # Store simplified action names for relations
    
    # Add action instances
    for i, action in enumerate(ff_plan):
        # Convert FF action to action string format
        action_name = normalize_action_name(action['name'])  # Normalize case to match C# expectations
        parameters = [param.lower() for param in action['parameters']]  # Convert parameters to lowercase
        
        # Create action string like "TravelML_r1_pr2_ep1"
        action_string = f"{action_name}_{'_'.join(parameters)}"
        full_action_instance = f"ActionInstance: {action_string}"
        plan_lines.append(full_action_instance)
        action_instances.append(full_action_instance)  # Store for relations
        action_names.append(action_string)  # Store simplified name for relations
    
    # Add sequential relations using simplified action names (without ActionInstance: prefix)
    for i in range(len(action_names) - 1):
        action1_name = action_names[i]
        action2_name = action_names[i + 1]
        plan_lines.append(f"{action1_name} --[MEETS]--> {action2_name}")
    
    return '\n'.join(plan_lines)

def convert_lama_first_to_plan_string(lama_first_plan):
    """Convert LAMA-first plan to plan string format (like NodeGraphGenerated.txt)"""
    plan_lines = []
    action_instances = []  # Store full action instance names for relations
    action_names = []  # Store simplified action names for relations
    
    # Add action instances
    for i, action in enumerate(lama_first_plan):
        # Convert LAMA-first action to action string format
        action_name = normalize_action_name(action['name'])  # Normalize case to match C# expectations
        parameters = [param.lower() for param in action['parameters']]  # Convert parameters to lowercase
        
        # Create action string like "TravelML_r1_pr2_ep1"
        action_string = f"{action_name}_{'_'.join(parameters)}"
        full_action_instance = f"ActionInstance: {action_string}"
        plan_lines.append(full_action_instance)
        action_instances.append(full_action_instance)  # Store for relations
        action_names.append(action_string)  # Store simplified name for relations
    
    # Add sequential relations using simplified action names (without ActionInstance: prefix)
    for i in range(len(action_names) - 1):
        action1_name = action_names[i]
        action2_name = action_names[i + 1]
        plan_lines.append(f"{action1_name} --[MEETS]--> {action2_name}")
    
    return '\n'.join(plan_lines)

def normalize_action_name(action_name):
    """Normalize action name case to match C# expectations"""
    # Convert to lowercase first for consistent processing
    action_lower = action_name.lower()
    
    # Common action name mappings
    action_mappings = {
        'travelml': 'TravelML',
        'equipeml': 'EquipeML', 
        'initializeml': 'InitializeML',
        'pickupml': 'PickUpML',
        'pickuphl': 'PickUpHL',
        'placehl': 'PlaceHL',
        'placeml': 'PlaceML',
        'gluingplatehl': 'GluingPlateHL',
        'gluingbeamhl': 'GluingBeamHL',
        'stackhl': 'StackHL',
        'stackml': 'StackML',
        'stackonmultiplehl': 'StackonmultipleHL',
        'nailinghl': 'NailingHL',
        'nailingml': 'NailingML',
        'deequipml': 'DeequipML',
        'closetoolml': 'CloseToolML'
    }
    
    # Check if we have a direct mapping
    if action_lower in action_mappings:
        return action_mappings[action_lower]
    
    # If no direct mapping, try to apply common patterns
    # For example, "travelml" -> "TravelML"
    if action_lower.endswith('ml'):
        prefix = action_lower[:-2]  # Remove 'ml'
        return prefix.title() + 'ML'
    elif action_lower.endswith('hl'):
        prefix = action_lower[:-2]  # Remove 'hl'
        return prefix.title() + 'HL'
    
    # Fallback to title case
    return action_name.title()

if __name__ == '__main__':
    print("Starting PDDL Planning Service...")
    print(f"Script directory: {SCRIPT_DIR}")
    print(f"Supported planners: {', '.join(SUPPORTED_PLANNERS)}")
    print(f"Default planner: {DEFAULT_PLANNER}")
    print(f"Default ENHSP path: {DEFAULT_ENHSP_PATH}")
    print(f"Default ENHSP available: {os.path.exists(DEFAULT_ENHSP_PATH)}")
    print(f"Default domain file path: {DEFAULT_DOMAIN_FILE_PATH}")
    print(f"Default domain file available: {os.path.exists(DEFAULT_DOMAIN_FILE_PATH)}")
    print(f"Default problem file path: {DEFAULT_PROBLEM_FILE_PATH}")
    print(f"Default problem file available: {os.path.exists(DEFAULT_PROBLEM_FILE_PATH)}")
    
    app.run(host='0.0.0.0', port=5000, debug=True)

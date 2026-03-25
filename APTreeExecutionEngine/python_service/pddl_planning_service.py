#!/usr/bin/env python3
"""
PDDL Planning Service
REST API service that calls multiple PDDL planners (ENHSP, FF)
"""

from flask import Flask, request, jsonify
import subprocess
import os
import json
import time
from datetime import datetime

app = Flask(__name__)

# Configuration - these will be overridden by request parameters
DEFAULT_ENHSP_PATH = "/home/ubuntu/ENHSP-Public/enhsp.jar"  # Default path to ENHSP JAR file

# Get the directory where this script is located
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
# The PDDL files are in the same directory as this script
DEFAULT_DOMAIN_FILE_PATH = os.path.join(SCRIPT_DIR, "Plannerinputs/static/DomainHL.pddl")  # Default path to domain file
DEFAULT_PROBLEM_FILE_PATH = os.path.join(SCRIPT_DIR, "Plannerinputs/static/problemC1.pddl")  # Default path to problem file
DEFAULT_TIMEOUT_SECONDS = 120
DEFAULT_PLANNER = "ENHSP"  # Default planner to use

# Docker container name for planutils-based planners (FF, LAMA-FIRST)
DOCKER_CONTAINER_NAME = "planutils"

# Supported planners
SUPPORTED_PLANNERS = ["ENHSP", "FF", "LAMA-FIRST", "SCORPION", "DOWNWARD", "OPTIC", "POPF", "PYPERPLAN"]

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
        enhsp_config = data.get('enhspConfig', None)  # Optional ENHSP -planner config (e.g. 'opt-hmax')
        
        # Legacy format: use planner_config values if not specified in new format
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
        
        # Normalise relative paths: strip leading "./" so they consistently
        # start with "Plannerinputs/" before we join them with SCRIPT_DIR.
        if domain_file_path.startswith("./"):
            domain_file_path = domain_file_path[2:]
        if problem_file_path.startswith("./"):
            problem_file_path = problem_file_path[2:]

        # Convert relative paths to absolute paths if they start with "Plannerinputs/"
        if domain_file_path.startswith("Plannerinputs/"):
            domain_file_path = os.path.join(SCRIPT_DIR, domain_file_path)
        if problem_file_path.startswith("Plannerinputs/"):
            problem_file_path = os.path.join(SCRIPT_DIR, problem_file_path)

        # If the C# side sent file content inline, save it locally so the
        # planner can read it (needed when the service runs on a remote VM).
        # This overrides whatever is already on disk.
        domain_file_content = data.get('domainFileContent')
        if domain_file_content:
            os.makedirs(os.path.dirname(domain_file_path), exist_ok=True)
            with open(domain_file_path, 'w', encoding='utf-8') as f:
                f.write(domain_file_content)
            print(f"✅ Saved inline domain file content to: {domain_file_path}")

        problem_file_content = data.get('problemFileContent')
        if problem_file_content:
            os.makedirs(os.path.dirname(problem_file_path), exist_ok=True)
            with open(problem_file_path, 'w', encoding='utf-8') as f:
                f.write(problem_file_content)
            print(f"✅ Saved inline problem file content to: {problem_file_path}")
        
        # Log extracted properties
        print(f"Extracted PDDL properties:")
        print(f"  - Domain file: {domain_file_path}")
        print(f"  - Problem file: {problem_file_path}")
        print(f"  - Planner path: {planner_path}")
        print(f"  - Planner name: {planner_name}")
        print(f"  - Timeout: {timeout_seconds} seconds")
        print(f"  - Max plan length: {max_plan_length}")
        print(f"  - ENHSP config: {enhsp_config}")
        
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

            plan_result = call_enhsp(domain_file, problem_file, planner_path, timeout_seconds, enhsp_config)
        elif planner_name == "FF":
            plan_result = call_ff(domain_file, problem_file, timeout_seconds)
        elif planner_name == "LAMA-FIRST":
            plan_result = call_lama_first(domain_file, problem_file, timeout_seconds)
        elif planner_name == "SCORPION":
            plan_result = call_scorpion(domain_file, problem_file, timeout_seconds)
        elif planner_name == "DOWNWARD":
            fd_config = enhsp_config  # reuse enhspConfig field for FD search string
            plan_result = call_downward(domain_file, problem_file, timeout_seconds, fd_config)
        elif planner_name == "OPTIC":
            plan_result = call_optic(domain_file, problem_file, timeout_seconds)
        elif planner_name == "POPF":
            plan_result = call_popf(domain_file, problem_file, timeout_seconds)
        elif planner_name == "PYPERPLAN":
            plan_result = call_pyperplan(domain_file, problem_file, timeout_seconds)
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
            # Return raw planner stdout — the C# execution engine
            # handles the transformation via Planner.TransformToAPTreeModel
            raw_output = plan_result.get('raw_output', '')

            return jsonify({
                'success': True,
                'plan': raw_output,
                'planningTimeSeconds': planning_time,
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


def call_enhsp(domain_file, problem_file, planner_path, timeout_seconds, enhsp_config=None):
    """Call ENHSP planner"""
    try:
        # Build ENHSP command (Java application)
        planner_cfg = enhsp_config if enhsp_config else 'pt-blind'
        cmd = [
            'java', '-jar', planner_path,
            '-o', domain_file,
            '-f', problem_file,
            '-planner', planner_cfg
        ]
        print(f"ENHSP config: {planner_cfg}")
        
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
            return {'success': True, 'raw_output': result.stdout}
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
        print(f"🔍 Using existing Docker container: {DOCKER_CONTAINER_NAME}")
        
        # Get the domain and problem file names (without path)
        domain_filename = os.path.basename(domain_file)
        problem_filename = os.path.basename(problem_file)
        
        print(f"🔍 Domain file: {domain_filename}")
        print(f"🔍 Problem file: {problem_filename}")
        
        # Pipe file contents via docker exec -i to bypass Docker Desktop's
        # broken symlink/junction resolution on Windows (APTreeExecutionEngine
        # is a junction and docker cp can't follow it).
        print(f"🔍 DEBUG: Domain source path: {domain_file}")
        print(f"🔍 DEBUG: Problem source path: {problem_file}")

        for label, src_path, dest_name in [
            ('domain', domain_file, domain_filename),
            ('problem', problem_file, problem_filename),
        ]:
            print(f"Piping {label} file into container: /root/{dest_name}")
            try:
                with open(src_path, 'r') as f:
                    content = f.read()
                pipe_result = subprocess.run(
                    ['docker', 'exec', '-i', DOCKER_CONTAINER_NAME,
                     'bash', '-c', f'cat > /root/{dest_name}'],
                    input=content, capture_output=True, text=True, timeout=30
                )
                if pipe_result.returncode != 0:
                    print(f"⚠️ Warning: Failed to pipe {label} file: {pipe_result.stderr}")
                else:
                    print(f"✅ {label.capitalize()} file piped successfully to /root/{dest_name}")
            except Exception as copy_err:
                print(f"⚠️ Warning: Error piping {label} file: {copy_err}")

        # Execute the FF planning command in the Docker container
        ff_cmd = [
            'docker', 'exec', DOCKER_CONTAINER_NAME,
            'bash', '-c',
            f'planutils activate && planutils run ff {domain_filename} {problem_filename}'
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
            return {'success': True, 'raw_output': result.stdout}
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
        print(f"🔍 Using existing Docker container: {DOCKER_CONTAINER_NAME}")
        
        # Get the domain and problem file names (without path)
        domain_filename = os.path.basename(domain_file)
        problem_filename = os.path.basename(problem_file)
        
        print(f"🔍 Domain file: {domain_filename}")
        print(f"🔍 Problem file: {problem_filename}")
        
        # Pipe file contents via docker exec -i to bypass Docker Desktop's
        # broken symlink/junction resolution on Windows (APTreeExecutionEngine
        # is a junction and docker cp can't follow it).
        print(f"🔍 DEBUG: Domain source path: {domain_file}")
        print(f"🔍 DEBUG: Problem source path: {problem_file}")

        for label, src_path, dest_name in [
            ('domain', domain_file, domain_filename),
            ('problem', problem_file, problem_filename),
        ]:
            print(f"Piping {label} file into container: /root/{dest_name}")
            try:
                with open(src_path, 'r') as f:
                    content = f.read()
                pipe_result = subprocess.run(
                    ['docker', 'exec', '-i', DOCKER_CONTAINER_NAME,
                     'bash', '-c', f'cat > /root/{dest_name}'],
                    input=content, capture_output=True, text=True, timeout=30
                )
                if pipe_result.returncode != 0:
                    print(f"⚠️ Warning: Failed to pipe {label} file: {pipe_result.stderr}")
                else:
                    print(f"✅ {label.capitalize()} file piped successfully to /root/{dest_name}")
            except Exception as copy_err:
                print(f"⚠️ Warning: Error piping {label} file: {copy_err}")

        # Execute the LAMA-first planning command in the Docker container.
        # LAMA (Fast Downward) writes the plan to a file (sas_plan) instead of
        # stdout, so we run the planner AND then cat the plan file in one command.
        lama_first_cmd = [
            'docker', 'exec', DOCKER_CONTAINER_NAME,
            'bash', '-c',
            f'planutils activate && planutils run lama-first {domain_filename} {problem_filename} && cat sas_plan'
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
            return {'success': True, 'raw_output': result.stdout}
        else:
            return {
                'success': False, 
                'error': f'LAMA-first failed with return code {result.returncode}: {result.stderr}'
            }
            
    except subprocess.TimeoutExpired:
        return {'success': False, 'error': 'LAMA-first planning timed out'}
    except Exception as e:
        return {'success': False, 'error': f'Error calling LAMA-first: {str(e)}'}

def _pipe_files_to_container(domain_file, problem_file):
    """Pipes domain and problem files into the planutils Docker container."""
    domain_filename = os.path.basename(domain_file)
    problem_filename = os.path.basename(problem_file)
    for label, src_path, dest_name in [
        ('domain', domain_file, domain_filename),
        ('problem', problem_file, problem_filename),
    ]:
        try:
            with open(src_path, 'r') as f:
                content = f.read()
            pipe_result = subprocess.run(
                ['docker', 'exec', '-i', DOCKER_CONTAINER_NAME,
                 'bash', '-c', f'cat > /root/{dest_name}'],
                input=content, capture_output=True, text=True, timeout=30
            )
            if pipe_result.returncode != 0:
                print(f"⚠️ Warning: Failed to pipe {label} file: {pipe_result.stderr}")
            else:
                print(f"✅ {label.capitalize()} file piped successfully to /root/{dest_name}")
        except Exception as copy_err:
            print(f"⚠️ Warning: Error piping {label} file: {copy_err}")
    return domain_filename, problem_filename


def _run_planutils_sas(package_name, domain_file, problem_file, timeout_seconds, extra_args=""):
    """
    Runs a Fast Downward-based planner via planutils and returns the sas_plan content.
    Used by all FD-derived planners (scorpion, downward, etc.).
    """
    domain_filename, problem_filename = _pipe_files_to_container(domain_file, problem_file)
    args = extra_args + " " if extra_args else ""
    cmd = [
        'docker', 'exec', DOCKER_CONTAINER_NAME,
        'bash', '-c',
        f'planutils activate && planutils run {package_name} {domain_filename} {problem_filename} {args}&& cat sas_plan'
    ]
    print(f"Calling {package_name} with command: {' '.join(cmd)}")
    result = subprocess.run(cmd, capture_output=True, text=True, timeout=timeout_seconds)
    print(f"{package_name} stdout: {result.stdout}")
    print(f"{package_name} stderr: {result.stderr}")
    if result.returncode == 0:
        return {'success': True, 'raw_output': result.stdout}
    return {'success': False, 'error': f'{package_name} failed (code {result.returncode}): {result.stderr}'}


def _run_planutils_stdout(package_name, domain_file, problem_file, timeout_seconds, extra_args=""):
    """
    Runs a planner via planutils whose plan is printed directly to stdout.
    Used by temporal planners (optic, popf) that do not write sas_plan.
    """
    domain_filename, problem_filename = _pipe_files_to_container(domain_file, problem_file)
    args = extra_args + " " if extra_args else ""
    cmd = [
        'docker', 'exec', DOCKER_CONTAINER_NAME,
        'bash', '-c',
        f'planutils activate && planutils run {package_name} {domain_filename} {problem_filename} {args}'
    ]
    print(f"Calling {package_name} with command: {' '.join(cmd)}")
    result = subprocess.run(cmd, capture_output=True, text=True, timeout=timeout_seconds)
    print(f"{package_name} stdout: {result.stdout}")
    print(f"{package_name} stderr: {result.stderr}")
    if result.returncode == 0:
        return {'success': True, 'raw_output': result.stdout}
    return {'success': False, 'error': f'{package_name} failed (code {result.returncode}): {result.stderr}'}


def call_scorpion(domain_file, problem_file, timeout_seconds):
    """Call Scorpion optimal classical planner via planutils Docker container."""
    try:
        return _run_planutils_sas('scorpion', domain_file, problem_file, timeout_seconds)
    except subprocess.TimeoutExpired:
        return {'success': False, 'error': 'Scorpion planning timed out'}
    except Exception as e:
        return {'success': False, 'error': f'Error calling Scorpion: {str(e)}'}


def call_downward(domain_file, problem_file, timeout_seconds, fd_config=None):
    """
    Call Fast Downward via planutils Docker container.
    fd_config maps to a --search argument shorthand:
      'astar-lmcut'  → --search "astar(lmcut())"       (optimal)
      'astar-blind'  → --search "astar(blind())"        (optimal)
      'lazy-ff'      → --search "lazy_greedy([ff()])"   (satisficing)
      None/default   → --search "lazy_greedy([ff(), cea()])"  (satisficing)
    """
    search_map = {
        'astar-lmcut': 'astar(lmcut())',
        'astar-blind': 'astar(blind())',
        'astar-ipdb':  'astar(ipdb())',
        'lazy-ff':     'lazy_greedy([ff()])',
        'lazy-cea':    'lazy_greedy([cea()])',
    }
    search = search_map.get(fd_config, 'lazy_greedy([ff(), cea()])')
    extra_args = f'--search "{search}"'
    try:
        return _run_planutils_sas('downward', domain_file, problem_file, timeout_seconds, extra_args)
    except subprocess.TimeoutExpired:
        return {'success': False, 'error': 'Fast Downward planning timed out'}
    except Exception as e:
        return {'success': False, 'error': f'Error calling Fast Downward: {str(e)}'}


def call_optic(domain_file, problem_file, timeout_seconds):
    """Call OPTIC temporal planner via planutils Docker container."""
    try:
        return _run_planutils_stdout('optic', domain_file, problem_file, timeout_seconds)
    except subprocess.TimeoutExpired:
        return {'success': False, 'error': 'OPTIC planning timed out'}
    except Exception as e:
        return {'success': False, 'error': f'Error calling OPTIC: {str(e)}'}


def call_popf(domain_file, problem_file, timeout_seconds):
    """Call POPF2 temporal planner via planutils Docker container."""
    try:
        return _run_planutils_stdout('popf', domain_file, problem_file, timeout_seconds)
    except subprocess.TimeoutExpired:
        return {'success': False, 'error': 'POPF planning timed out'}
    except Exception as e:
        return {'success': False, 'error': f'Error calling POPF: {str(e)}'}


def call_pyperplan(domain_file, problem_file, timeout_seconds):
    """
    Call Pyperplan via planutils Docker container.
    Pyperplan writes the plan to <problem>.soln; we cat it after planning.
    """
    domain_filename, problem_filename = _pipe_files_to_container(domain_file, problem_file)
    soln_file = problem_filename.replace('.pddl', '.soln')
    cmd = [
        'docker', 'exec', DOCKER_CONTAINER_NAME,
        'bash', '-c',
        f'planutils activate && planutils run pyperplan {domain_filename} {problem_filename} && cat {soln_file}'
    ]
    print(f"Calling pyperplan with command: {' '.join(cmd)}")
    try:
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=timeout_seconds)
        print(f"pyperplan stdout: {result.stdout}")
        print(f"pyperplan stderr: {result.stderr}")
        if result.returncode == 0:
            return {'success': True, 'raw_output': result.stdout}
        return {'success': False, 'error': f'Pyperplan failed (code {result.returncode}): {result.stderr}'}
    except subprocess.TimeoutExpired:
        return {'success': False, 'error': 'Pyperplan planning timed out'}
    except Exception as e:
        return {'success': False, 'error': f'Error calling Pyperplan: {str(e)}'}


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

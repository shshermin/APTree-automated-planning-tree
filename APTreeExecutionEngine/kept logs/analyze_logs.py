"""
Log Analyzer for APTree Execution Engine
=========================================
Parses FullTreeTest log files and extracts key metrics:
  1. How many times new PDDL problem files were generated
  2. How many times a NodeGraph was cleared (re-planning happened)
  3. How many action nodes were added during runtime and how many remained at the end
  
Usage:
    python analyze_logs.py                          # analyzes the default log
    python analyze_logs.py <path_to_log_file>       # analyzes a specific log
"""

import re
import sys
import os
from collections import Counter


def analyze_log(log_path: str):
    """Analyze a FullTreeTest log file and print key metrics."""

    if not os.path.exists(log_path):
        print(f"Error: Log file not found: {log_path}")
        sys.exit(1)

    with open(log_path, "r", encoding="utf-8") as f:
        lines = f.readlines()

    print(f"Analyzing: {os.path.basename(log_path)}")
    print(f"Total log lines: {len(lines)}")
    print("=" * 70)

    # ─── 1. PDDL Problem File Generation ─────────────────────────────────
    problem_gen_starts = []
    problem_gen_successes = []
    problem_file_names = []

    for line in lines:
        if "Starting GenerateDynamicPDDLProblem" in line:
            # Extract instance name
            m = re.search(r"for instance:\s*(\S+)", line)
            instance = m.group(1) if m else "unknown"
            problem_gen_starts.append(instance)

        if "Successfully completed GenerateDynamicPDDLProblem" in line:
            problem_gen_successes.append(line)

        if "Generated PDDL problem file:" in line:
            m = re.search(r"problem file:\s*(\S+)", line)
            if m:
                problem_file_names.append(m.group(1))

    print("\n1. PDDL PROBLEM FILE GENERATION")
    print("-" * 40)
    print(f"   GenerateDynamicPDDLProblem started:    {len(problem_gen_starts)}")
    print(f"   GenerateDynamicPDDLProblem succeeded:  {len(problem_gen_successes)}")
    print(f"   Unique problem files written:          {len(set(problem_file_names))}")

    # Count by action type
    action_type_counts = Counter()
    for inst in problem_gen_starts:
        action_type = inst.split("_")[0] if "_" in inst else inst
        action_type_counts[action_type] += 1

    if action_type_counts:
        print(f"\n   Breakdown by action type:")
        for action_type, count in sorted(action_type_counts.items(), key=lambda x: -x[1]):
            print(f"     {action_type:30s} {count}")

    # ─── 2. NodeGraph Cleared / Re-planning ──────────────────────────────
    destroy_calls = 0
    destroy_results = []
    planning_resets = 0
    reset_on_subtree = 0

    for line in lines:
        if "DestroyAllNodes called" in line:
            destroy_calls += 1

        if "DestroyAllNodes completed" in line:
            m = re.search(r"destroyed (\d+), kept (\d+)", line)
            if m:
                destroyed = int(m.group(1))
                kept = int(m.group(2))
                destroy_results.append((destroyed, kept))

        if "Planning service reset" in line:
            planning_resets += 1

        if "resetting planning state" in line:
            reset_on_subtree += 1

    total_destroyed = sum(d for d, k in destroy_results)
    total_kept_after_destroy = sum(k for d, k in destroy_results)
    non_empty_destroys = sum(1 for d, k in destroy_results if d > 0)

    print(f"\n2. NODEGRAPH CLEARED / RE-PLANNING")
    print("-" * 40)
    print(f"   DestroyAllNodes calls:                 {destroy_calls}")
    print(f"   DestroyAllNodes with nodes removed:    {non_empty_destroys}")
    print(f"   Total nodes destroyed:                 {total_destroyed}")
    print(f"   Total nodes kept (already executed):   {total_kept_after_destroy}")
    print(f"   Planning service resets:               {planning_resets}")
    print(f"   ResetOnSubtreeSuccess triggers:        {reset_on_subtree}")

    # ─── 3. Action Nodes Added / Remaining ───────────────────────────────
    actions_created = []
    nodegraph_actions = []

    for line in lines:
        # Actions created by the factory at runtime
        if "Successfully created action instance:" in line:
            m = re.search(r"action instance:\s*(\S+)", line)
            if m:
                actions_created.append(m.group(1))

        # NodeGraphs generated with N actions
        if "Generated NodeGraph with" in line:
            m = re.search(r"Generated NodeGraph with (\d+) action", line)
            if m:
                nodegraph_actions.append(int(m.group(1)))

    # Actions added via ParseNodeGraph
    parse_created = []
    for line in lines:
        if "ParseNodeGraph: Created action instance:" in line:
            m = re.search(r"-> (\S+)", line)
            if m:
                parse_created.append(m.group(1))

    # Final actions remaining (from BehaviorTreeComponentLogger)
    final_actions_remaining = []
    for line in lines:
        if "FinalActionsRemaining" in line or "Final.*actions.*remaining" in line:
            m = re.search(r"(\d+)", line)
            if m:
                final_actions_remaining.append(int(m.group(1)))

    # Check for "Added action to NodeGraph" entries
    added_to_nodegraph = 0
    for line in lines:
        if "Adding action to NodeGraph:" in line:
            added_to_nodegraph += 1

    # Check for service setup completion count
    service_setup_actions = 0
    for line in lines:
        if "Completed service setup for" in line:
            m = re.search(r"for (\d+) actions", line)
            if m:
                service_setup_actions += int(m.group(1))

    print(f"\n3. ACTION NODES ADDED / REMAINING")
    print("-" * 40)
    print(f"   Action instances created (factory):    {len(actions_created)}")
    print(f"   Action instances created (parser):     {len(parse_created)}")
    print(f"   Actions added to NodeGraphs:           {added_to_nodegraph}")
    print(f"   Actions attached via service setup:    {service_setup_actions}")
    print(f"   NodeGraphs generated:                  {len(nodegraph_actions)}")
    print(f"   Total actions across all NodeGraphs:   {sum(nodegraph_actions)}")
    if nodegraph_actions:
        print(f"   Avg actions per NodeGraph:             {sum(nodegraph_actions)/len(nodegraph_actions):.1f}")

    # Unique action instance names
    unique_actions = set(parse_created)
    print(f"   Unique action instances created:       {len(unique_actions)}")

    # Count by action type
    action_types_created = Counter()
    for name in parse_created:
        action_type = name.split("_")[0] if "_" in name else name
        action_types_created[action_type] += 1

    if action_types_created:
        print(f"\n   Breakdown of created actions by type:")
        for action_type, count in sorted(action_types_created.items(), key=lambda x: -x[1]):
            print(f"     {action_type:30s} {count}")

    # ─── Also check companion CSV if present ─────────────────────────────
    log_dir = os.path.dirname(log_path)
    # Try to find BehaviorTreeComponentSummary CSV in WrittenLogs
    written_logs = os.path.join(os.path.dirname(log_dir), "WrittenLogs")
    csv_files = []
    if os.path.isdir(written_logs):
        csv_files = [f for f in os.listdir(written_logs)
                     if f.startswith("BehaviorTreeComponentSummary") and f.endswith(".csv")]
    # Also check current directory
    if os.path.isdir(log_dir):
        csv_files += [f for f in os.listdir(log_dir)
                      if f.startswith("BehaviorTreeComponentSummary") and f.endswith(".csv")]

    if csv_files:
        csv_files.sort(reverse=True)
        csv_path = os.path.join(written_logs, csv_files[0]) if os.path.exists(os.path.join(written_logs, csv_files[0])) else os.path.join(log_dir, csv_files[0])
        print(f"\n{'=' * 70}")
        print(f"COMPANION CSV: {os.path.basename(csv_path)}")
        print("-" * 40)
        with open(csv_path, "r") as f:
            csv_lines = f.readlines()
        for line in csv_lines:
            parts = line.strip().split(",")
            if len(parts) >= 6:
                comp_type = parts[0]
                if comp_type in ("GenericBTAction", "DynamicFlowNode", "ServicePDDLPlanning",
                                 "ServiceSubtreeInject", "TOTAL", "ComponentType"):
                    print(f"   {line.strip()}")

    print(f"\n{'=' * 70}")
    print("Analysis complete.")


if __name__ == "__main__":
    if len(sys.argv) > 1:
        log_file = sys.argv[1]
    else:
        # Default: look for the latest FullTreeTest log in the same directory
        script_dir = os.path.dirname(os.path.abspath(__file__))
        log_files = [f for f in os.listdir(script_dir)
                     if f.startswith("FullTreeTest") and f.endswith(".log")]
        if log_files:
            log_files.sort(reverse=True)
            log_file = os.path.join(script_dir, log_files[0])
        else:
            print("No FullTreeTest log files found in the current directory.")
            print("Usage: python analyze_logs.py <path_to_log_file>")
            sys.exit(1)

    analyze_log(log_file)

import argparse
import csv
import glob
import re
from pathlib import Path
from typing import Dict, List, Tuple
from datetime import datetime


SCRIPT_DIR = Path(__file__).resolve().parent
ENGINE_ROOT = SCRIPT_DIR.parents[2]
DEFAULT_WRITTEN_LOGS = ENGINE_ROOT / "WrittenLogs"
DEFAULT_LATEX_OUTPUT = DEFAULT_WRITTEN_LOGS / "latex_tables"


def latest_file(log_dir: Path, pattern: str) -> Path:
    files = [Path(path) for path in glob.glob(str(log_dir / pattern))]
    if not files:
        raise FileNotFoundError(f"No files found for pattern: {pattern} in {log_dir}")
    return max(files, key=lambda path: path.stat().st_mtime)


def extract_stamp(path: Path) -> str:
    match = re.search(r"(\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2})", path.name)
    return match.group(1) if match else ""


def files_by_stamp(log_dir: Path, pattern: str) -> Dict[str, Path]:
    out: Dict[str, Path] = {}
    for file_path in [Path(path) for path in glob.glob(str(log_dir / pattern))]:
        stamp = extract_stamp(file_path)
        if stamp:
            out[stamp] = file_path
    return out


def read_csv_rows(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def normalize_text(value: str) -> str:
    return str(value or "").strip()


def collect_inputs(log_dir: Path, run_stamp: str | None = None) -> Dict[str, Path]:
    required_patterns = {
        "planner_calls": "PlannerCalls_*.csv",
        "hl_trace": "HierarchicalTrace_HL_*.csv",
        "ml_trace": "HierarchicalTrace_ML_*.csv",
        "ll_trace": "HierarchicalTrace_LL_*.csv",
    }

    by_key_and_stamp = {
        key: files_by_stamp(log_dir, pattern) for key, pattern in required_patterns.items()
    }

    if run_stamp:
        missing = [key for key, mapping in by_key_and_stamp.items() if run_stamp not in mapping]
        if missing:
            raise FileNotFoundError(
                f"Requested run stamp '{run_stamp}' is missing required files for: {', '.join(missing)}"
            )
        return {key: mapping[run_stamp] for key, mapping in by_key_and_stamp.items()}

    common_stamps = None
    for mapping in by_key_and_stamp.values():
        stamps = set(mapping.keys())
        common_stamps = stamps if common_stamps is None else common_stamps.intersection(stamps)

    if common_stamps:
        chosen_stamp = max(common_stamps)
        return {key: mapping[chosen_stamp] for key, mapping in by_key_and_stamp.items()}

    return {key: latest_file(log_dir, pattern) for key, pattern in required_patterns.items()}


def classify_planner_call_layer(
    call_name: str,
    hl_names: set[str],
    ml_names: set[str],
    ll_names: set[str],
) -> str:
    name = normalize_text(call_name)
    lower = name.lower()

    if name in hl_names:
        return "HL"
    if name in ml_names:
        return "ML"
    if name in ll_names:
        return "LL"

    if "layers" in lower or "hl" in lower:
        return "HL"
    if "ml" in lower:
        return "ML"
    if "ll" in lower:
        return "LL"

    return "HL"


def compute_counts(inputs: Dict[str, Path]) -> Dict[str, Dict[str, int]]:
    planner_rows = read_csv_rows(inputs["planner_calls"])
    hl_rows = read_csv_rows(inputs["hl_trace"])
    ml_rows = read_csv_rows(inputs["ml_trace"])
    ll_rows = read_csv_rows(inputs["ll_trace"])

    hl_names = {normalize_text(row.get("ActionName", "")) for row in hl_rows}
    ml_names = {normalize_text(row.get("InstanceName", "")) for row in ml_rows}
    ll_names = {normalize_text(row.get("InstanceName", "")) for row in ll_rows}

    task_planner = {"HL": 0, "ML": 0, "LL": 0}
    for row in planner_rows:
        planner = normalize_text(row.get("PlannerName", "")).upper()
        if planner != "ENHSP":
            continue
        layer = classify_planner_call_layer(row.get("HLActionInstance", ""), hl_names, ml_names, ll_names)
        task_planner[layer] += 1

    # User definition: subtree reuse is every LL subtree injection.
    subtree_reuse = {"HL": 0, "ML": 0, "LL": len(ll_rows)}

    # RRT-Connect invocations are represented by LL commands with CommandType="planned".
    motion_planner = {
        "HL": 0,
        "ML": 0,
        "LL": sum(1 for row in ll_rows if normalize_text(row.get("CommandType", "")).lower() == "planned"),
    }

    # Direct robot execution uses non-planned LL command types (movej/movel/gripper/program, etc.).
    direct_robot = {
        "HL": 0,
        "ML": 0,
        "LL": sum(1 for row in ll_rows if normalize_text(row.get("CommandType", "")).lower() != "planned"),
    }

    return {
        "Task Planner Calls (ENHSP)": task_planner,
        "SubTree reuse": subtree_reuse,
        "Motion Planner Calls (RRT-Connect)": motion_planner,
        "Direct Robot Execution (internal UR10 Motion planning)": direct_robot,
    }


def latex_table(counts: Dict[str, Dict[str, int]]) -> str:
    lines = [
        r"\begin{table}[h]",
        r"\centering",
        r"\caption{Planner Invocations per Hierarchical Layer (M1)}",
        r"\label{tab:m1_planner_invocations}",
        r"\begin{tabular}{lccc}",
        r"\hline",
        r"\textbf{Planner Type} & \textbf{HL} & \textbf{ML} & \textbf{LL} \\",
        r"\hline",
    ]

    for row_name in [
        "Task Planner Calls (ENHSP)",
        "SubTree reuse",
        "Motion Planner Calls (RRT-Connect)",
        "Direct Robot Execution (internal UR10 Motion planning)",
    ]:
        c = counts[row_name]
        lines.append(rf"{row_name} & {c['HL']} & {c['ML']} & {c['LL']} \\")

    lines.extend([
        r"\hline",
        r"\end{tabular}",
        r"\end{table}",
    ])

    return "\n".join(lines)


def build_source_map(inputs: Dict[str, Path], counts: Dict[str, Dict[str, int]]) -> str:
    lines = ["Input files:"]
    for key, path in inputs.items():
        lines.append(f"- {key}: {path}")

    lines.extend([
        "",
        "Metric definitions:",
        "- Task Planner Calls (ENHSP): count PlannerCalls rows with PlannerName=ENHSP, layer inferred from HLActionInstance matched to HL/ML/LL traces.",
        "- SubTree reuse: user-defined as every LL subtree injection; counted as number of HierarchicalTrace_LL rows.",
        "- Motion Planner Calls (RRT-Connect): count HierarchicalTrace_LL rows with CommandType=planned.",
        "- Direct Robot Execution (internal UR10 Motion planning): count HierarchicalTrace_LL rows with CommandType!=planned.",
        "",
        "Counts:",
    ])

    for row_name, vals in counts.items():
        lines.append(f"- {row_name}: HL={vals['HL']}, ML={vals['ML']}, LL={vals['LL']}")

    return "\n".join(lines)


def write_outputs(latex_dir: Path, counts: Dict[str, Dict[str, int]], inputs: Dict[str, Path]) -> Tuple[Path, Path]:
    timestamp = datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
    latex_dir.mkdir(parents=True, exist_ok=True)

    latex_path = latex_dir / f"M1_Planner_Invocations_Table_{timestamp}.tex"
    latex_path.write_text(latex_table(counts), encoding="utf-8")

    source_path = latex_dir / f"M1_Planner_Invocations_Sources_{timestamp}.txt"
    source_path.write_text(build_source_map(inputs, counts), encoding="utf-8")

    return latex_path, source_path


def main() -> None:
    parser = argparse.ArgumentParser(description="Compute M1 planner invocation counts by hierarchy layer and emit LaTeX.")
    parser.add_argument(
        "--written-logs",
        default=str(DEFAULT_WRITTEN_LOGS),
        help="Path to APTreeExecutionEngine/WrittenLogs",
    )
    parser.add_argument(
        "--run-stamp",
        default=None,
        help="Optional run timestamp stamp in format YYYY-MM-DD_HH-mm-ss. If omitted, newest common stamp across required files is used.",
    )
    parser.add_argument(
        "--latex-output",
        default=str(DEFAULT_LATEX_OUTPUT),
        help="Output directory for generated LaTeX table and source map files.",
    )
    args = parser.parse_args()

    log_dir = Path(args.written_logs).resolve()
    if not log_dir.exists():
        raise FileNotFoundError(f"WrittenLogs directory not found: {log_dir}")

    latex_dir = Path(args.latex_output).resolve()

    inputs = collect_inputs(log_dir, run_stamp=args.run_stamp)
    counts = compute_counts(inputs)

    print("Input files:")
    for key, path in inputs.items():
        print(f"- {key}: {path}")

    print()
    print("Counts:")
    for row_name, vals in counts.items():
        print(f"- {row_name}: HL={vals['HL']}, ML={vals['ML']}, LL={vals['LL']}")

    print()
    print(latex_table(counts))

    latex_path, source_path = write_outputs(latex_dir, counts, inputs)
    print()
    print(f"LaTeX table written to: {latex_path}")
    print(f"Source map written to: {source_path}")


if __name__ == "__main__":
    main()

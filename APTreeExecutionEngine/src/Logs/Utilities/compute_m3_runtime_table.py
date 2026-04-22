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


def to_int(value: str, default: int = 0) -> int:
    try:
        return int(float(value))
    except Exception:
        return default


def to_bool(value: str) -> bool:
    return str(value).strip().lower() in {"true", "1", "yes"}


def normalize_path(value: str) -> str:
    return str(value or "").replace("\\", "/").lower()


def collect_inputs(log_dir: Path, run_stamp: str | None = None) -> Dict[str, Path]:
    required_patterns = {
        "blackboard_summary": "BlackboardSummary_*.csv",
        "planner_calls": "PlannerCalls_*.csv",
        "hl_trace": "HierarchicalTrace_HL_*.csv",
        "ml_trace": "HierarchicalTrace_ML_*.csv",
        "ll_trace": "HierarchicalTrace_LL_*.csv",
        "robot_summary": "RobotCommandSummary_*.csv",
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

    # Fallback (legacy behavior): latest per file type if no common stamp exists.
    return {key: latest_file(log_dir, pattern) for key, pattern in required_patterns.items()}


def runtime_blackboard_instances_from_summary(path: Path) -> int:
    rows = read_csv_rows(path)
    by_category = {row.get("Category", "").strip(): row for row in rows}

    # Runtime contribution is interpreted as "after ticking" additions.
    action_after = to_int(by_category.get("ActionInstances", {}).get("AfterTicking", "0"))
    parameter_after = to_int(by_category.get("ParameterInstances", {}).get("AfterTicking", "0"))
    predicate_after = to_int(by_category.get("PredicateInstances", {}).get("AfterTicking", "0"))

    return action_after + parameter_after + predicate_after


def compute_counts(inputs: Dict[str, Path]) -> Dict[str, int]:
    planner_rows = read_csv_rows(inputs["planner_calls"])
    hl_rows = read_csv_rows(inputs["hl_trace"])
    ml_rows = read_csv_rows(inputs["ml_trace"])
    ll_rows = read_csv_rows(inputs["ll_trace"])
    robot_rows = read_csv_rows(inputs["robot_summary"])

    row3 = runtime_blackboard_instances_from_summary(inputs["blackboard_summary"])

    row4 = sum(
        1
        for row in planner_rows
        if "plannerinputs/generated/" in normalize_path(row.get("ProblemFile", ""))
    )

    row5 = sum(
        1
        for row in planner_rows
        if to_bool(row.get("Success", "")) and to_int(row.get("ActionsGenerated", "0")) > 0
    )

    row6 = len(hl_rows) + len(ml_rows) + len(ll_rows)
    row7 = len(ll_rows)

    row8 = 0
    for row in robot_rows:
        if row.get("CommandType", "").strip().upper() == "TOTAL":
            row8 = to_int(row.get("Count", "0"))
            break

    return {
        "row1": 0,
        "row2": 0,
        "row3": row3,
        "row4": row4,
        "row5": row5,
        "row6": row6,
        "row7": row7,
        "row8": row8,
    }


def build_row_sources(inputs: Dict[str, Path]) -> List[Tuple[str, str, int, str]]:
    return [
        (
            "CAD models -> APTree DSL",
            "manual",
            0,
            "Forced to 0 by script; compute manually.",
        ),
        (
            "APTree DSL -> static PDDL domain/problem files (initial)",
            "manual",
            0,
            "Forced to 0 by script; compute manually.",
        ),
        (
            "APTree DSL -> blackboard instances (nodes, scene objects, etc.)",
            inputs["blackboard_summary"].name,
            3,
            "Sum runtime additions from BlackboardSummary AfterTicking: ActionInstances + ParameterInstances + PredicateInstances.",
        ),
        (
            "Blackboard instances -> dynamic PDDL planning problems",
            inputs["planner_calls"].name,
            4,
            "Count PlannerCalls rows whose ProblemFile contains Plannerinputs/generated/.",
        ),
        (
            "PDDL plan -> APTree DSL (NodeGraph)",
            inputs["planner_calls"].name,
            5,
            "Count successful PlannerCalls rows with ActionsGenerated > 0.",
        ),
        (
            "APTree DSL -> HL + ML + LL action nodes",
            f"{inputs['hl_trace'].name} + {inputs['ml_trace'].name} + {inputs['ll_trace'].name}",
            6,
            "Count rows in HierarchicalTrace_HL + HierarchicalTrace_ML + HierarchicalTrace_LL.",
        ),
        (
            "LL action instances -> robot/motion commands",
            inputs["robot_summary"].name,
            8,
            "Use TOTAL Count from RobotCommandSummary.",
        ),
    ]


def latex_table(counts: Dict[str, int]) -> str:
    included_rows = ["row1", "row2", "row3", "row4", "row5", "row6", "row8"]
    total = sum(counts[row] for row in included_rows)
    lines = [
        r"\begin{table}[h]",
        r"\centering",
        r"\caption{Automation of Model and Runtime Transformations (M3)}",
        r"\label{tab:m3_runtime_counts}",
        r"\begin{tabular}{p{7.6cm}cc p{4.0cm}}",
        r"\hline",
        r"\textbf{Transformation} & \textbf{Manual} & \textbf{Automated} & \textbf{Notes} \\",
        r"\hline",
        rf"CAD models $\rightarrow$ APTree DSL & 0 & {counts['row1']} & Manual outside this script \\",
        rf"APTree DSL $\rightarrow$ static PDDL domain/problem files (initial) & 0 & {counts['row2']} & Manual outside this script \\",
        rf"APTree DSL $\rightarrow$ blackboard instances (nodes, scene objects, etc.) & 0 & {counts['row3']} & Runtime additions via AfterTicking counters \\",
        rf"Blackboard instances $\rightarrow$ dynamic PDDL planning problems & 0 & {counts['row4']} & Generated runtime problem files \\",
        rf"PDDL plan $\rightarrow$ APTree DSL (NodeGraph) & 0 & {counts['row5']} & Successful planner outputs with generated actions \\",
        rf"APTree DSL $\rightarrow$ HL + ML + LL action nodes & 0 & {counts['row6']} & Counted from HL + ML + LL trace rows \\",
        rf"LL action instances $\rightarrow$ robot/motion commands & 0 & {counts['row8']} & Robot command TOTAL count \\",
        r"\hline",
        rf"\textbf{{Total}} & \textbf{{0}} & \textbf{{{total}}} & \\",
        r"\hline",
        r"\end{tabular}",
        r"\end{table}",
    ]
    return "\n".join(lines)


def print_source_map(inputs: Dict[str, Path], counts: Dict[str, int]) -> None:
    rows = build_row_sources(inputs)
    print("Source mapping:")
    for name, source_name, row_id, note in rows:
        count = counts[f"row{row_id}"] if row_id else 0
        print(f"- {name}")
        print(f"  count: {count}")
        print(f"  source: {source_name}")
        print(f"  note: {note}")


def write_outputs(latex_dir: Path, counts: Dict[str, int], inputs: Dict[str, Path]) -> Tuple[Path, Path]:
    timestamp = datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
    latex_dir.mkdir(parents=True, exist_ok=True)

    latex_path = latex_dir / f"M3_Runtime_Table_{timestamp}.tex"
    latex_path.write_text(latex_table(counts), encoding="utf-8")

    source_lines = ["Input files:"]
    for key, path in inputs.items():
        source_lines.append(f"- {key}: {path}")
    source_lines.append("")
    source_lines.append("Counts:")
    for row_name in sorted(counts):
        source_lines.append(f"- {row_name}: {counts[row_name]}")
    source_lines.append("")
    source_lines.append("Source mapping:")
    for name, source_name, row_id, note in build_row_sources(inputs):
        count = counts[f"row{row_id}"] if row_id else 0
        source_lines.append(f"- {name}")
        source_lines.append(f"  count: {count}")
        source_lines.append(f"  source: {source_name}")
        source_lines.append(f"  note: {note}")

    source_map_path = latex_dir / f"M3_Runtime_Table_Sources_{timestamp}.txt"
    source_map_path.write_text("\n".join(source_lines), encoding="utf-8")

    return latex_path, source_map_path


def main() -> None:
    parser = argparse.ArgumentParser(description="Compute M3 runtime transformation counts and emit LaTeX.")
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
    for row_name in sorted(counts):
        print(f"- {row_name}: {counts[row_name]}")

    print()
    print(latex_table(counts))
    print()
    print_source_map(inputs, counts)

    latex_path, source_map_path = write_outputs(latex_dir, counts, inputs)
    print()
    print(f"LaTeX table written to: {latex_path}")
    print(f"Source map written to: {source_map_path}")


if __name__ == "__main__":
    main()
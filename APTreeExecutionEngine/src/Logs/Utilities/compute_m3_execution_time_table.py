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
    files = [Path(p) for p in glob.glob(str(log_dir / pattern))]
    if not files:
        raise FileNotFoundError(f"No files found for pattern: {pattern} in {log_dir}")
    return max(files, key=lambda p: p.stat().st_mtime)


def extract_stamp(path: Path) -> str:
    m = re.search(r"(\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2})", path.name)
    return m.group(1) if m else ""


def files_by_stamp(log_dir: Path, pattern: str) -> Dict[str, Path]:
    out: Dict[str, Path] = {}
    for p in [Path(x) for x in glob.glob(str(log_dir / pattern))]:
        stamp = extract_stamp(p)
        if stamp:
            out[stamp] = p
    return out


def read_csv_rows(path: Path) -> List[Dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def to_float(value: str, default: float = 0.0) -> float:
    try:
        return float(str(value).strip())
    except Exception:
        return default


def norm(value: str) -> str:
    return str(value or "").strip()


def collect_inputs(log_dir: Path, run_stamp: str | None = None) -> Dict[str, Path]:
    required = {
        "planner_calls": "PlannerCalls_*.csv",
        "ml_trace": "HierarchicalTrace_ML_*.csv",
        "ll_trace": "HierarchicalTrace_LL_*.csv",
    }

    by_key = {key: files_by_stamp(log_dir, pat) for key, pat in required.items()}

    if run_stamp:
        missing = [key for key, m in by_key.items() if run_stamp not in m]
        if missing:
            raise FileNotFoundError(
                f"Requested run stamp '{run_stamp}' missing files for: {', '.join(missing)}"
            )
        return {key: m[run_stamp] for key, m in by_key.items()}

    common = None
    for m in by_key.values():
        stamps = set(m.keys())
        common = stamps if common is None else common.intersection(stamps)

    if common:
        chosen = max(common)
        return {key: m[chosen] for key, m in by_key.items()}

    return {key: latest_file(log_dir, pat) for key, pat in required.items()}


def compute_phases(inputs: Dict[str, Path]) -> Dict[str, Dict[str, float]]:
    planner_rows = read_csv_rows(inputs["planner_calls"])
    ml_rows = read_csv_rows(inputs["ml_trace"])
    ll_rows = read_csv_rows(inputs["ll_trace"])

    # Task Planning: ENHSP call times (external) vs service overhead (APTree).
    planner_external = sum(to_float(r.get("PlannerTimeMs", "0")) for r in planner_rows)
    planner_total = sum(to_float(r.get("TotalTimeMs", "0")) for r in planner_rows)
    planner_aptree = max(planner_total - planner_external, 0.0)

    # Subtree Generation & Parameter Resolution: ML-layer time not attributable to LL execution.
    ml_total = sum(to_float(r.get("TotalTimeMs", "0")) for r in ml_rows)
    ll_exec_total = sum(to_float(r.get("ExecTimeMs", "0")) for r in ll_rows)
    subtree_aptree = max(ml_total - ll_exec_total, 0.0)
    subtree_total = subtree_aptree
    subtree_external = 0.0

    # Motion Planning: LL commands that go through external motion planner (MoveIt/RRT-Connect).
    motion_plan_total = sum(
        to_float(r.get("ExecTimeMs", "0"))
        for r in ll_rows
        if norm(r.get("CommandType", "")).lower() == "planned"
    )
    motion_plan_external = motion_plan_total
    motion_plan_aptree = 0.0

    # Motion Execution: direct UR10 commands (movej/movel/gripper/program).
    motion_exec_total = sum(
        to_float(r.get("ExecTimeMs", "0"))
        for r in ll_rows
        if norm(r.get("CommandType", "")).lower() != "planned"
    )
    motion_exec_external = motion_exec_total
    motion_exec_aptree = 0.0

    return {
        "Task Planning": {
            "total": planner_total,
            "external": planner_external,
            "aptree": planner_aptree,
        },
        "Subtree Generation & Parameter Resolution": {
            "total": subtree_total,
            "external": subtree_external,
            "aptree": subtree_aptree,
        },
        "Motion Planning": {
            "total": motion_plan_total,
            "external": motion_plan_external,
            "aptree": motion_plan_aptree,
        },
        "Motion Execution": {
            "total": motion_exec_total,
            "external": motion_exec_external,
            "aptree": motion_exec_aptree,
        },
    }


def fmt_ms(value: float) -> str:
    return f"{value:,.1f}"


def latex_table(phases: Dict[str, Dict[str, float]]) -> str:
    order = [
        "Task Planning",
        "Subtree Generation & Parameter Resolution",
        "Motion Planning",
        "Motion Execution",
    ]

    total = sum(phases[p]["total"] for p in order)
    total_ext = sum(phases[p]["external"] for p in order)
    total_apt = sum(phases[p]["aptree"] for p in order)

    lines = [
        r"\begin{table}[h]",
        r"\centering",
        r"\caption{Execution Time Breakdown with System Overhead (M3)}",
        r"\label{tab:m3_execution_time_breakdown}",
        r"\begin{tabular}{lccc}",
        r"\hline",
        r"\textbf{Phase} & \textbf{Total Time (ms)} & "
        r"\makecell{\textbf{External} \\ \textbf{Computation (ms)}} & "
        r"\makecell{\textbf{APTree} \\ \textbf{Processing (ms)}} \\",
        r"\hline",
    ]

    for phase in order:
        vals = phases[phase]
        phase_label = phase.replace("&", r"\&")
        lines.append(
            rf"{phase_label} & {fmt_ms(vals['total'])} & {fmt_ms(vals['external'])} & {fmt_ms(vals['aptree'])} \\"
        )

    lines.extend([
        r"\hline",
        rf"\textbf{{Total Execution Time}} & \textbf{{{fmt_ms(total)}}} & \textbf{{{fmt_ms(total_ext)}}} & \textbf{{{fmt_ms(total_apt)}}} \\",
        r"\hline",
        r"\end{tabular}",
        r"\end{table}",
    ])

    return "\n".join(lines)


def build_source_map(inputs: Dict[str, Path], phases: Dict[str, Dict[str, float]]) -> str:
    lines = ["Input files:"]
    for key, path in inputs.items():
        lines.append(f"- {key}: {path}")

    lines.extend([
        "",
        "Metric definitions:",
        "- Task Planning: sum of PlannerCalls.TotalTimeMs (Total); sum of PlannerCalls.PlannerTimeMs (External = ENHSP); APTree = Total - External.",
        "- Subtree Generation & Parameter Resolution: sum(HierarchicalTrace_ML.TotalTimeMs) - sum(HierarchicalTrace_LL.ExecTimeMs). Attributed fully to APTree.",
        "- Motion Planning: sum(HierarchicalTrace_LL.ExecTimeMs where CommandType=planned). External only (MoveIt/RRT-Connect + robot execution are not separated in the current LL log; plan-only time is not isolated).",
        "- Motion Execution: sum(HierarchicalTrace_LL.ExecTimeMs where CommandType!=planned). External only (direct UR10 execution).",
        "",
        "Counts (ms):",
    ])

    for phase, vals in phases.items():
        lines.append(
            f"- {phase}: total={vals['total']:.2f}, external={vals['external']:.2f}, aptree={vals['aptree']:.2f}"
        )

    return "\n".join(lines)


def write_outputs(latex_dir: Path, phases: Dict[str, Dict[str, float]], inputs: Dict[str, Path]) -> Tuple[Path, Path]:
    timestamp = datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
    latex_dir.mkdir(parents=True, exist_ok=True)

    latex_path = latex_dir / f"M3_Execution_Time_Table_{timestamp}.tex"
    latex_path.write_text(latex_table(phases), encoding="utf-8")

    source_path = latex_dir / f"M3_Execution_Time_Sources_{timestamp}.txt"
    source_path.write_text(build_source_map(inputs, phases), encoding="utf-8")

    return latex_path, source_path


def main() -> None:
    parser = argparse.ArgumentParser(description="Compute M3 execution time breakdown and emit LaTeX.")
    parser.add_argument("--written-logs", default=str(DEFAULT_WRITTEN_LOGS))
    parser.add_argument("--run-stamp", default=None)
    parser.add_argument("--latex-output", default=str(DEFAULT_LATEX_OUTPUT))
    args = parser.parse_args()

    log_dir = Path(args.written_logs).resolve()
    if not log_dir.exists():
        raise FileNotFoundError(f"WrittenLogs directory not found: {log_dir}")

    latex_dir = Path(args.latex_output).resolve()

    inputs = collect_inputs(log_dir, run_stamp=args.run_stamp)
    phases = compute_phases(inputs)

    print("Input files:")
    for key, path in inputs.items():
        print(f"- {key}: {path}")

    print()
    print("Phase timings (ms):")
    for phase, vals in phases.items():
        print(f"- {phase}: total={vals['total']:.2f}, external={vals['external']:.2f}, aptree={vals['aptree']:.2f}")

    print()
    print(latex_table(phases))

    latex_path, source_path = write_outputs(latex_dir, phases, inputs)
    print()
    print(f"LaTeX table written to: {latex_path}")
    print(f"Source map written to: {source_path}")


if __name__ == "__main__":
    main()

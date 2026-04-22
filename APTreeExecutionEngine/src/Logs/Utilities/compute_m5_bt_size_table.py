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


def to_int(value: str, default: int = 0) -> int:
    try:
        return int(float(str(value).strip()))
    except Exception:
        return default


def collect_inputs(log_dir: Path, run_stamp: str | None = None) -> Dict[str, Path]:
    required = {
        "component_summary": "BehaviorTreeComponentSummary_*.csv",
        "hl_trace": "HierarchicalTrace_HL_*.csv",
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


def compute_rows(inputs: Dict[str, Path]) -> Dict[str, int]:
    comp_rows = read_csv_rows(inputs["component_summary"])
    by_comp = {
        row.get("ComponentType", "").strip(): to_int(row.get("AdditionCount", "0"))
        for row in comp_rows
    }

    hl = len(read_csv_rows(inputs["hl_trace"]))
    ml = len(read_csv_rows(inputs["ml_trace"]))
    ll = len(read_csv_rows(inputs["ll_trace"]))

    flow_control = sum(
        count for comp, count in by_comp.items()
        if comp.startswith("BTFlowNode") or comp == "DynamicFlowNode"
    )

    services = sum(
        count for comp, count in by_comp.items()
        if comp.startswith("Service")
    )

    decorators = sum(
        count for comp, count in by_comp.items()
        if comp.startswith("Decorator")
    )

    return {
        "HL Action Nodes": hl,
        "ML Action Nodes": ml,
        "LL Action Nodes": ll,
        "Flow/Control Nodes": flow_control,
        "Services": services,
        "Decorators": decorators,
    }


def latex_table(counts: Dict[str, int]) -> str:
    order = [
        "HL Action Nodes",
        "ML Action Nodes",
        "LL Action Nodes",
        "Flow/Control Nodes",
        "Services",
        "Decorators",
    ]

    total_auto = sum(counts[r] for r in order)

    lines = [
        r"\begin{table}[h]",
        r"\centering",
        r"\caption{Behavior Tree Size Profile (M5)}",
        r"\label{tab:m5_bt_size_profile}",
        r"\begin{tabular}{lcc}",
        r"\hline",
        r"\textbf{Node Type} & \textbf{Auto Generated} & \textbf{Manual} \\",
        r"\hline",
    ]

    for row in order:
        lines.append(rf"{row} & {counts[row]} & 0 \\")

    lines.extend([
        r"\hline",
        rf"\textbf{{Total Nodes}} & \textbf{{{total_auto}}} & \textbf{{0}} \\",
        r"\hline",
        r"\end{tabular}",
        r"\end{table}",
    ])

    return "\n".join(lines)


def build_source_map(inputs: Dict[str, Path], counts: Dict[str, int]) -> str:
    lines = ["Input files:"]
    for key, path in inputs.items():
        lines.append(f"- {key}: {path}")

    lines.extend([
        "",
        "Metric definitions (Auto Generated = added at runtime):",
        "- HL Action Nodes: row count of HierarchicalTrace_HL.",
        "- ML Action Nodes: row count of HierarchicalTrace_ML.",
        "- LL Action Nodes: row count of HierarchicalTrace_LL.",
        "- Flow/Control Nodes: sum of AdditionCount for ComponentType starting with 'BTFlowNode' or equal to 'DynamicFlowNode' in BehaviorTreeComponentSummary.",
        "- Services: sum of AdditionCount for ComponentType starting with 'Service' in BehaviorTreeComponentSummary.",
        "- Decorators: sum of AdditionCount for ComponentType starting with 'Decorator' in BehaviorTreeComponentSummary.",
        "Manual column: forced to 0 by script; fill manually from the authored DSL (DemonstratorFinal.bt).",
        "",
        "Counts:",
    ])

    for row, value in counts.items():
        lines.append(f"- {row}: {value}")

    return "\n".join(lines)


def write_outputs(latex_dir: Path, counts: Dict[str, int], inputs: Dict[str, Path]) -> Tuple[Path, Path]:
    timestamp = datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
    latex_dir.mkdir(parents=True, exist_ok=True)

    latex_path = latex_dir / f"M5_BT_Size_Table_{timestamp}.tex"
    latex_path.write_text(latex_table(counts), encoding="utf-8")

    source_path = latex_dir / f"M5_BT_Size_Sources_{timestamp}.txt"
    source_path.write_text(build_source_map(inputs, counts), encoding="utf-8")

    return latex_path, source_path


def main() -> None:
    parser = argparse.ArgumentParser(description="Compute M5 behavior tree size profile and emit LaTeX.")
    parser.add_argument("--written-logs", default=str(DEFAULT_WRITTEN_LOGS))
    parser.add_argument("--run-stamp", default=None)
    parser.add_argument("--latex-output", default=str(DEFAULT_LATEX_OUTPUT))
    args = parser.parse_args()

    log_dir = Path(args.written_logs).resolve()
    if not log_dir.exists():
        raise FileNotFoundError(f"WrittenLogs directory not found: {log_dir}")

    latex_dir = Path(args.latex_output).resolve()

    inputs = collect_inputs(log_dir, run_stamp=args.run_stamp)
    counts = compute_rows(inputs)

    print("Input files:")
    for key, path in inputs.items():
        print(f"- {key}: {path}")

    print()
    print("Counts (auto generated at runtime):")
    for row, value in counts.items():
        print(f"- {row}: {value}")

    print()
    print(latex_table(counts))

    latex_path, source_path = write_outputs(latex_dir, counts, inputs)
    print()
    print(f"LaTeX table written to: {latex_path}")
    print(f"Source map written to: {source_path}")


if __name__ == "__main__":
    main()

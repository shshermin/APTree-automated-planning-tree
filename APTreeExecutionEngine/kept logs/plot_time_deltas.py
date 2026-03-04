"""
Plot Time Delta Between Actions: With Replanning vs Without Replanning
======================================================================
Compares two MLActionResult logs:
  - "With replanning": run where the planner is invoked dynamically (large gaps)
  - "Without replanning": run where actions are pre-planned (small gaps)

Produces a scatter plot of the time delta (ms) between consecutive actions.

Usage:
    python plot_time_deltas.py
    python plot_time_deltas.py <with_replanning_log> <without_replanning_log>
"""

import re
import sys
import os
from datetime import datetime
import matplotlib.pyplot as plt


def parse_csv(path):
    """Parse MLActionResult CSV format (Counter,Timestamp,ActionName,InstanceName,Result,ElapsedMs)."""
    timestamps = []
    with open(path, "r", encoding="utf-8") as f:
        first_line = f.readline()
        # If header row, skip; otherwise rewind
        if first_line.strip().startswith("Counter"):
            pass  # header skipped
        else:
            f.seek(0)
        for line in f:
            parts = line.strip().split(",")
            if len(parts) >= 2:
                ts_str = parts[1].strip()
                try:
                    ts = datetime.strptime(ts_str, "%H:%M:%S.%f")
                    timestamps.append(ts)
                except ValueError:
                    continue
    return timestamps


def parse_log(path):
    """Parse MLActionResult log format: [Counter] [Timestamp] [ActionName] [InstanceName] [Result] (+elapsed)."""
    timestamps = []
    pattern = re.compile(r"\[\d+\]\s+\[(\d{2}:\d{2}:\d{2}\.\d{3})\]")
    with open(path, "r", encoding="utf-8") as f:
        for line in f:
            m = pattern.search(line)
            if m:
                ts = datetime.strptime(m.group(1), "%H:%M:%S.%f")
                timestamps.append(ts)
    return timestamps


def parse_file(path):
    """Auto-detect format and parse."""
    if path.endswith(".csv"):
        return parse_csv(path)
    else:
        return parse_log(path)


def compute_deltas_ms(timestamps):
    """Compute time deltas in milliseconds between consecutive timestamps."""
    deltas = []
    for i in range(1, len(timestamps)):
        delta = (timestamps[i] - timestamps[i - 1]).total_seconds() * 1000
        deltas.append(delta)
    return deltas


def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))

    if len(sys.argv) >= 3:
        with_replan_path = os.path.join(script_dir, sys.argv[1]) if not os.path.isabs(sys.argv[1]) else sys.argv[1]
        without_replan_path = os.path.join(script_dir, sys.argv[2]) if not os.path.isabs(sys.argv[2]) else sys.argv[2]
    else:
        # Default files
        with_replan_path = os.path.join(script_dir, "MLActionResult_2026-02-19_19-12-47.log")
        without_replan_path = os.path.join(script_dir, "MLActionResult_2026-02-18_15-51-18.log")

    # Optional output filename
    out_name = sys.argv[3] if len(sys.argv) >= 4 else "time_delta_chart.png"

    print(f"With replanning:    {os.path.basename(with_replan_path)}")
    print(f"Without replanning: {os.path.basename(without_replan_path)}")

    # Parse timestamps
    with_ts = parse_file(with_replan_path)
    without_ts = parse_file(without_replan_path)

    print(f"  Actions loaded: {len(with_ts)} (with replan), {len(without_ts)} (without replan)")

    # Compute deltas
    with_deltas = compute_deltas_ms(with_ts)
    without_deltas = compute_deltas_ms(without_ts)

    print(f"  Transitions:    {len(with_deltas)} (with replan), {len(without_deltas)} (without replan)")

    # --- Bucket averages ---
    def bucket_averages(deltas, label):
        buckets = {"< 500 ms": [], ">= 500 ms": []}
        for d in deltas:
            if d < 500:
                buckets["< 500 ms"].append(d)
            else:
                buckets[">= 500 ms"].append(d)
        print(f"\n  {label}:")
        for name, vals in buckets.items():
            if vals:
                avg = sum(vals) / len(vals)
                print(f"    {name}: count={len(vals)}, avg={avg:.1f} ms")
            else:
                print(f"    {name}: count=0")

    bucket_averages(with_deltas, "With replanning")
    bucket_averages(without_deltas, "Without replanning")

    # Plot
    fig, ax = plt.subplots(figsize=(16, 5.5))

    ax.scatter(
        range(len(with_deltas)),
        with_deltas,
        color="#5B9FD6",
        alpha=0.7,
        s=36,
        label="With replanning",
        edgecolors="none",
    )
    ax.scatter(
        range(len(without_deltas)),
        without_deltas,
        color="#F07040",
        alpha=0.7,
        s=36,
        label="Without replanning",
        edgecolors="none",
    )

    ax.set_xlabel("Action Step (transition index)", fontsize=11)
    ax.set_ylabel("Time to next action (ms)", fontsize=11)
    ax.set_title(
        "Time Delta Between Actions: With Replanning vs Without Replanning",
        fontsize=13,
    )
    ax.legend(fontsize=10)
    ax.grid(False)

    plt.tight_layout()

    out_path = os.path.join(script_dir, out_name)
    fig.savefig(out_path, dpi=150)
    print(f"\nChart saved to: {out_path}")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""
Analyze time deltas between consecutive ML actions from two log files.
Prints min, max, average, and median for each.

Usage:
    python analyze_time_deltas.py <with_replan_log> <without_replan_log>
"""

import sys
import re
import csv
from pathlib import Path


def parse_timestamps(filepath):
    """Parse timestamps from either .log or .csv format, return list of ms values."""
    timestamps = []
    path = Path(filepath)

    if path.suffix == ".csv":
        with open(filepath, "r") as f:
            reader = csv.DictReader(f)
            for row in reader:
                ts = row.get("Timestamp", "")
                m = re.search(r"(\d{2}):(\d{2}):(\d{2})\.(\d{3})", ts)
                if m:
                    h, mi, s, ms = int(m[1]), int(m[2]), int(m[3]), int(m[4])
                    timestamps.append(h * 3600000 + mi * 60000 + s * 1000 + ms)
    else:
        pattern = re.compile(r"\[\d+\]\s+\[(\d{2}:\d{2}:\d{2}\.\d{3})\]")
        with open(filepath, "r") as f:
            for line in f:
                m = pattern.search(line)
                if m:
                    parts = m.group(1)
                    h, mi, rest = parts.split(":")
                    s, ms = rest.split(".")
                    timestamps.append(
                        int(h) * 3600000 + int(mi) * 60000 + int(s) * 1000 + int(ms)
                    )

    return timestamps


def compute_deltas(timestamps):
    """Compute time deltas (ms) between consecutive timestamps."""
    return [timestamps[i + 1] - timestamps[i] for i in range(len(timestamps) - 1)]


def print_stats(label, deltas):
    """Print statistics for a list of deltas."""
    if not deltas:
        print(f"  {label}: No data")
        return

    avg = sum(deltas) / len(deltas)
    mn = min(deltas)
    mx = max(deltas)
    sorted_d = sorted(deltas)
    n = len(sorted_d)
    median = sorted_d[n // 2] if n % 2 == 1 else (sorted_d[n // 2 - 1] + sorted_d[n // 2]) / 2

    print(f"  {label}:")
    print(f"    Count:   {len(deltas)} transitions")
    print(f"    Min:     {mn} ms")
    print(f"    Max:     {mx} ms")
    print(f"    Average: {avg:.1f} ms")
    print(f"    Median:  {median:.1f} ms")


def main():
    if len(sys.argv) < 3:
        print("Usage: python analyze_time_deltas.py <with_replan_log> <without_replan_log>")
        sys.exit(1)

    with_file = sys.argv[1]
    without_file = sys.argv[2]

    print(f"With replanning:    {with_file}")
    print(f"Without replanning: {without_file}")
    print()

    ts_with = parse_timestamps(with_file)
    ts_without = parse_timestamps(without_file)

    deltas_with = compute_deltas(ts_with)
    deltas_without = compute_deltas(ts_without)

    print(f"=== Time Delta Statistics (ms) ===")
    print()
    print_stats("With replanning (red)", deltas_with)
    print()
    print_stats("Without replanning (blue)", deltas_without)


if __name__ == "__main__":
    main()

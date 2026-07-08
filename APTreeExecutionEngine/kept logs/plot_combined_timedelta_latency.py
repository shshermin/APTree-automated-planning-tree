"""
Combined diagram: Time Delta Between Actions (left) + Planning Service Latency (right)
"""
import re
import os
from datetime import datetime
import matplotlib.pyplot as plt
import pandas as pd

script_dir = os.path.dirname(os.path.abspath(__file__))

# ===== LEFT: Time Delta Between Actions =====
def parse_log(path):
    timestamps = []
    pattern = re.compile(r"\[\d+\]\s+\[(\d{2}:\d{2}:\d{2}\.\d{3})\]")
    with open(path, "r", encoding="utf-8") as f:
        for line in f:
            m = pattern.search(line)
            if m:
                ts = datetime.strptime(m.group(1), "%H:%M:%S.%f")
                timestamps.append(ts)
    return timestamps

with_replan_path = os.path.join(script_dir, "version2_with_replanning_2026-07-01", "MLActionResult_2026-07-01_14-05-19.log")
without_replan_path = os.path.join(script_dir, "version1_without_replanning_2026-07-01", "MLActionResult_2026-07-01_12-20-47.log")

with_ts = parse_log(with_replan_path)
without_ts = parse_log(without_replan_path)

with_deltas = [(with_ts[i] - with_ts[i-1]).total_seconds() * 1000 for i in range(1, len(with_ts))]
without_deltas = [(without_ts[i] - without_ts[i-1]).total_seconds() * 1000 for i in range(1, len(without_ts))]

# ===== RIGHT: Planner Service Latency =====
planner_csv = os.path.join(script_dir, "version2_with_replanning_2026-07-01", "PlannerCalls_2026-07-01_14-18-41.csv")
df_planner = pd.read_csv(planner_csv)
df_planner['TotalTimeMs'] = pd.to_numeric(df_planner['TotalTimeMs'], errors='coerce')

# ===== PLOT =====
fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(14, 3.5))

# Left: Time Delta
ax1.scatter(range(len(with_deltas)), with_deltas, color='#2196F3', alpha=0.6, s=10, label='With replanning', edgecolors='none')
ax1.scatter(range(len(without_deltas)), without_deltas, color='#FF9800', alpha=0.6, s=10, label='Without replanning', edgecolors='none')
ax1.set_xlabel('Action Step (transition index)')
ax1.set_ylabel('Time to next action (ms)')
ax1.set_title('Time Delta Between Consecutive ML Actions')
ax1.legend(fontsize=9)
ax1.grid(True, alpha=0.3)

# Right: Planner Service Latency
ax2.scatter(df_planner['CallNumber'], df_planner['TotalTimeMs'], color='#2196F3', alpha=0.6, s=10, edgecolors='none')
avg_latency = df_planner['TotalTimeMs'].mean()
ax2.axhline(y=avg_latency, color='red', linestyle='--', linewidth=1.5, label=f'Average: {avg_latency:.0f} ms')
ax2.set_xlabel('Planner Call Number')
ax2.set_ylabel('Total Service Time (ms)')
ax2.set_title('Planning Service Latency Per Call')
ax2.legend(fontsize=9)
ax2.grid(True, alpha=0.3)

plt.tight_layout()
out_png = os.path.join(script_dir, "TimeDelta_and_PlannerLatency_combined.png")
out_pdf = os.path.join(script_dir, "TimeDelta_and_PlannerLatency_combined.pdf")
plt.savefig(out_png, dpi=150, bbox_inches='tight')
plt.savefig(out_pdf, bbox_inches='tight')
print(f"Saved: {out_png}")
print(f"Saved: {out_pdf}")
plt.close()

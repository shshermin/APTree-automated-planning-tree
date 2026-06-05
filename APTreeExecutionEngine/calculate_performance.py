"""
Performance Breakdown Calculator
=================================
Produces a time-breakdown table with four categories:
  1. Task Planning      – ENHSP planner wall-clock time
  2. Motion Planning    – per-command trajectory planning time (plannedj/plannedl)
  3. Robot Execution    – time from BT sends command until hardware completes it
                          (ExecTimeMs in RobotCommands, includes all sleep/wait)
  4. BT Overhead        – everything else: ticking, decorators, services,
                          gap time between commands not covered above
"""

import csv
import os

LOG_DIR = os.path.join(os.path.dirname(__file__),
                       r"kept logs\demonstrator_2026-05-29_16-58-37")

def load_csv(filename):
    path = os.path.join(LOG_DIR, filename)
    with open(path, newline="", encoding="utf-8") as f:
        return list(csv.DictReader(f))

# ── 1. Total wall-clock time ──────────────────────────────────────────────────
e2e = load_csv("EndToEndSummary_2026-05-29_20-07-55.csv")[0]
total_sec  = float(e2e["WallClockSec"])
total_ms   = total_sec * 1000
print(f"Total wall-clock time: {total_sec:.2f} s  ({total_ms/1000:.2f} s)")

# ── 2. Task planning time (ENHSP) ─────────────────────────────────────────────
planner_calls = load_csv("PlannerCalls_2026-05-29_20-07-55.csv")
hl_calls = [r for r in planner_calls if "static" in r["ProblemFile"]]
ml_calls = [r for r in planner_calls if "static" not in r["ProblemFile"]]

hl_plan_ms  = sum(float(r["PlannerTimeMs"]) for r in hl_calls)
ml_plan_ms  = sum(float(r["PlannerTimeMs"]) for r in ml_calls)
total_plan_ms = hl_plan_ms + ml_plan_ms

hl_avg_ms = hl_plan_ms / len(hl_calls) if hl_calls else 0
ml_avg_ms = ml_plan_ms / len(ml_calls) if ml_calls else 0

# ── 3. Robot commands: execution & motion-planning time ───────────────────────
robot_cmds = load_csv("RobotCommands_2026-05-29_20-07-55.csv")
# Only top-level commands (IsSubStep == False)
top_cmds = [r for r in robot_cmds if r["IsSubStep"].strip().lower() == "false"]

motion_planned_types = {"plannedj", "plannedl"}

exec_ms_total = 0.0
exec_by_type  = {}
count_by_type = {}

# Non-exec overhead per command = TotalTimeMs - ExecTimeMs
# This includes: BT service call overhead + motion planning (for plannedj/plannedl)
# Baseline service overhead is estimated from movej (same code path, no planning)
movej_cmds = [r for r in top_cmds if r["CommandType"].strip() == "movej"]
movej_nonexec = [(float(r["TotalTimeMs"]) - float(r["ExecTimeMs"])) for r in movej_cmds]
movej_baseline_avg = sum(movej_nonexec) / len(movej_nonexec) if movej_nonexec else 0.0

# For each planned command: motion planning round-trip = (Total-Exec) - movej_baseline
mplan_roundtrip = {}   # per type: total round-trip planning ms
mplan_count     = {}

for r in top_cmds:
    ct      = r["CommandType"].strip()
    exec_ms = float(r["ExecTimeMs"])
    exec_ms_total += exec_ms
    exec_by_type[ct] = exec_by_type.get(ct, 0.0) + exec_ms
    count_by_type[ct] = count_by_type.get(ct, 0) + 1

    if ct in motion_planned_types:
        total_ms_cmd = float(r["TotalTimeMs"])
        rt = (total_ms_cmd - exec_ms) - movej_baseline_avg
        mplan_roundtrip[ct] = mplan_roundtrip.get(ct, 0.0) + rt
        mplan_count[ct]     = mplan_count.get(ct, 0) + 1

motion_plan_ms_total = sum(mplan_roundtrip.values())

# ── 4. BT overhead = total – task_planning – motion_planning – execution ──────
# BTOverheadMs per command is already a subset of the gap time (residual),
# so we must NOT add it again — just derive overhead as the remainder.
accounted_ms  = total_plan_ms + motion_plan_ms_total + exec_ms_total
bt_total_ms   = total_ms - accounted_ms   # everything not in the three categories above

# ── 5. Per-command type execution breakdown ───────────────────────────────────
cmd_summary = {}
for r in top_cmds:
    ct = r["CommandType"].strip()
    if ct not in cmd_summary:
        cmd_summary[ct] = {"count": 0, "exec_ms": 0.0, "plan_ms": 0.0}
    cmd_summary[ct]["count"]   += 1
    cmd_summary[ct]["exec_ms"] += float(r["ExecTimeMs"])
    cmd_summary[ct]["plan_ms"] += float(r["PlanTimeMs"])

# ── Print results ─────────────────────────────────────────────────────────────
def pct(val, total):
    return f"{val/total*100:.1f}%"

def fmt(ms):
    return f"{ms/1000:.1f} s"

SEP = "=" * 70

print(f"\n{SEP}")
print("  PERFORMANCE BREAKDOWN")
print(SEP)
print(f"  Total wall-clock time : {total_ms/1000:.2f} s")
print(SEP)

rows = [
    ("Task Planning – HL layer plans (ENHSP, 12 calls)",
     hl_plan_ms,   len(hl_calls),  f"avg {hl_avg_ms/1000:.2f} s/call"),
    ("Task Planning – ML decomposition (ENHSP, 330 calls)",
     ml_plan_ms,   len(ml_calls),  f"avg {ml_avg_ms/1000:.2f} s/call"),
    ("Motion Planning – Joint space (plannedj)",
     mplan_by_type.get("plannedj", 0), count_by_type.get("plannedj", 0),
     f"avg {mplan_by_type.get('plannedj',0)/max(count_by_type.get('plannedj',1),1)/1000:.3f} s/call"),
    ("Motion Planning – Cartesian (plannedl)",
     mplan_by_type.get("plannedl", 0), count_by_type.get("plannedl", 0),
     f"avg {mplan_by_type.get('plannedl',0)/max(count_by_type.get('plannedl',1),1)/1000:.3f} s/call"),
    ("Robot Execution (all commands, hardware time)",
     exec_ms_total, len(top_cmds),
     f"avg {exec_ms_total/len(top_cmds)/1000:.2f} s/cmd"),
    ("BT Overhead (ticking, decorators, services, gaps)",
     bt_total_ms, None, ""),
]

for label, ms, count, note in rows:
    cnt_str = f"  n={count}" if count is not None else ""
    print(f"  {label}")
    print(f"    {fmt(ms):>10}  ({pct(ms, total_ms):>5}){cnt_str}  {note}")

total_task_plan = hl_plan_ms + ml_plan_ms
total_motion_plan = motion_plan_ms_total
print()
print(f"  ── Subtotals ──")
print(f"  Task Planning total   : {fmt(total_task_plan):>10}  ({pct(total_task_plan, total_ms):>5})")
print(f"  Motion Planning total : {fmt(total_motion_plan):>10}  ({pct(total_motion_plan, total_ms):>5})")
print(f"  Robot Execution total : {fmt(exec_ms_total):>10}  ({pct(exec_ms_total, total_ms):>5})")
print(f"  BT Overhead total     : {fmt(bt_total_ms):>10}  ({pct(bt_total_ms, total_ms):>5})")
print(f"  ─────────────────────────────────────────")
print(f"  Check sum             : {fmt(total_task_plan+total_motion_plan+exec_ms_total+bt_total_ms):>10}")

print(f"\n{SEP}")
print("  EXECUTION BREAKDOWN BY COMMAND TYPE")
print(SEP)
print(f"  {'CommandType':<22} {'Count':>6} {'Exec (s)':>10} {'MotionPlan (s)':>15} {'Avg Exec (s)':>13}")
print(f"  {'-'*22} {'-'*6} {'-'*10} {'-'*15} {'-'*13}")
for ct, d in sorted(cmd_summary.items(), key=lambda x: -x[1]["exec_ms"]):
    avg = d["exec_ms"] / d["count"] / 1000
    print(f"  {ct:<22} {d['count']:>6} {d['exec_ms']/1000:>10.1f} {d['plan_ms']/1000:>15.2f} {avg:>13.2f}")

print(f"\n{SEP}")
print("  LATEX TABLE")
print(SEP)
print(r"""
\begin{table}[h]
\centering
\caption{Execution Time Breakdown by Phase}
\label{tab:performance_breakdown}
\begin{tabular}{lrrr}
\hline
\textbf{Phase} & \textbf{Time (s)} & \textbf{\%} & \textbf{Calls / Avg} \\
\hline""")

latex_rows = [
    ("Task Planning – HL (ENHSP, static)",
     hl_plan_ms, len(hl_calls), f"avg {hl_avg_ms/1000:.2f}\\,s/call"),
    ("Task Planning – ML (ENHSP, dynamic)",
     ml_plan_ms, len(ml_calls), f"avg {ml_avg_ms/1000:.2f}\\,s/call"),
    (r"Motion Planning – Joint space (\texttt{plannedj})",
     mplan_by_type.get("plannedj",0), count_by_type.get("plannedj",0),
     f"avg {mplan_by_type.get('plannedj',0)/max(count_by_type.get('plannedj',1),1)/1000:.3f}\\,s/call"),
    (r"Motion Planning – Cartesian (\texttt{plannedl})",
     mplan_by_type.get("plannedl",0), count_by_type.get("plannedl",0),
     f"avg {mplan_by_type.get('plannedl',0)/max(count_by_type.get('plannedl',1),1)/1000:.3f}\\,s/call"),
    ("Robot Execution (hardware, all commands)",
     exec_ms_total, len(top_cmds), f"avg {exec_ms_total/len(top_cmds)/1000:.2f}\\,s/cmd"),
    ("BT Overhead (ticking, decorators, gaps)",
     bt_total_ms, None, "—"),
    (r"\textbf{Total}", total_ms, None, ""),
]
for label, ms, count, note in latex_rows:
    cnt = f"n={count}, {note}" if count is not None else (note if note else "")
    print(f"    {label} & {ms/1000:.1f} & {ms/total_ms*100:.1f}\\% & {cnt} \\\\")

print(r"""\hline
\end{tabular}
\end{table}""")

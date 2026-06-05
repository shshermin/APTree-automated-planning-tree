"""
Precise fault timing calculations.

Timestamp = when planner call STARTED (confirmed: call#2 to call#3 gap = HL2.TotalTimeMs).
TotalTimeMs for failed HL actions = 0 (logging artifact — reconstruct from ML durations).

Fault injection order (by execution time):
  F3: dislodge_stick5  → ML12 StackML_stick2 fails  (OnActivationCount=1)
  F2: blocker_on_stick3 → ML13 TravelML_stick3 fails (OnActivationCount=1)
  F1: drop_stick6       → ML24 PickUpML_stick6 fails (OnActivationCount=1)
"""
import csv
from datetime import datetime

def ts(s):
    return datetime.strptime(s, "%Y-%m-%d %H:%M:%S.%f")

pc = list(csv.DictReader(open('PlannerCalls_2026-05-29_20-07-55.csv', encoding='utf-8-sig')))
calls = {int(r['CallNumber']): r for r in pc}

def call_start(n):  return ts(calls[n]['Timestamp'])
def call_plan_ms(n): return float(calls[n]['PlannerTimeMs'])
def call_done(n):    return ts(calls[n]['Timestamp']) if False else None  # use delta math below

from datetime import timedelta

def add_ms(t, ms):
    return t + timedelta(milliseconds=ms)

# ── FAULT 3: dislodge_stick5_after_stack ────────────────────────────────────
# Triggered at ML12 (StackML_stick2) under HL7 (call #7)
c7_start = call_start(7)                  # 17:01:34.323
c7_plan_done = add_ms(c7_start, call_plan_ms(7))  # +2053ms → HL7 execution starts
ML11_duration_ms = 13545.0
t_ML12_start  = add_ms(c7_plan_done, ML11_duration_ms)   # after TravelML11
t_fault3 = t_ML12_start                  # ML12 fails at 0ms (instant)

# HL replan (call #8)
c8_start = call_start(8)                 # 17:02:03.615
c8_plan_ms = call_plan_ms(8)             # 2191ms
c8_done  = add_ms(c8_start, c8_plan_ms) # HL plan ready → HL8 starts

# First action of new plan (call #9 for PickUpHL_stick3)
c9_start = call_start(9)                 # 17:02:05.885

gap_fault3_to_replan = (c8_start - t_fault3).total_seconds()
gap_fault3_to_plan_done = (c8_done - t_fault3).total_seconds()
gap_fault3_to_action = (c9_start - t_fault3).total_seconds()

print("=== FAULT 3: dislodge_stick5_after_stack ===")
print(f"  t_fault3       = {t_fault3.strftime('%H:%M:%S.%f')[:-3]}")
print(f"  HL replan call #8 at {c8_start.strftime('%H:%M:%S.%f')[:-3]} ({gap_fault3_to_replan:.1f}s after fault)")
print(f"  M3 (replan latency) = {c8_plan_ms:.0f}ms = {c8_plan_ms/1000:.2f}s  [HL PDDL replan]")
print(f"  HL plan ready at {c8_done.strftime('%H:%M:%S.%f')[:-3]} ({gap_fault3_to_plan_done:.1f}s after fault)")
print(f"  First new action call #9 starts at {c9_start.strftime('%H:%M:%S.%f')[:-3]}")
print(f"  M2 (time to first action of recovery plan) = {gap_fault3_to_action:.1f}s")
print()

# ── FAULT 2: blocker_on_stick3 ───────────────────────────────────────────────
# Triggered at ML13 (TravelML for PickUpHL_stick3_dup2 under HL9 = call #9)
c9_plan_ms = call_plan_ms(9)             # 2123ms
t_ML13_start = add_ms(c9_start, c9_plan_ms)   # after plan ready, ML13 starts
t_fault2 = t_ML13_start                  # ML13 fails at 0ms

# ML replan (call #10)
c10_start = call_start(10)               # 17:02:16.579
c10_plan_ms = call_plan_ms(10)           # 2193ms
c10_done = add_ms(c10_start, c10_plan_ms)

# Recovery ML actions under call #10 plan
ML14_ms = 17038.0  # PickUpML_stickdummy
ML15_ms = 17438.0  # PutDownML_stickdummy
ML16_ms = 16300.0  # PickUpML_stick3_dup2
t_ML14_start = c10_done
t_ML15_start = add_ms(t_ML14_start, ML14_ms)
t_ML16_start = add_ms(t_ML15_start, ML15_ms)
t_ML16_done  = add_ms(t_ML16_start, ML16_ms)  # stick3 successfully picked up

gap_fault2_to_replan = (c10_start - t_fault2).total_seconds()
M2_fault2 = (t_ML16_done - t_fault2).total_seconds()
gap_fault2_to_plan = (c10_done - t_fault2).total_seconds()

print("=== FAULT 2: blocker_on_stick3 ===")
print(f"  t_fault2       = {t_fault2.strftime('%H:%M:%S.%f')[:-3]}")
print(f"  ML replan call #10 at {c10_start.strftime('%H:%M:%S.%f')[:-3]} ({gap_fault2_to_replan:.1f}s after fault)")
print(f"  M3 (replan latency) = {c10_plan_ms:.0f}ms = {c10_plan_ms/1000:.2f}s  [ML PDDL replan]")
print(f"  Plan ready at {c10_done.strftime('%H:%M:%S.%f')[:-3]} ({gap_fault2_to_plan:.1f}s after fault)")
print(f"  Recovery: ML14({ML14_ms:.0f}ms) + ML15({ML15_ms:.0f}ms) + ML16({ML16_ms:.0f}ms) = {(ML14_ms+ML15_ms+ML16_ms)/1000:.1f}s")
print(f"  Stick3 successfully picked up at {t_ML16_done.strftime('%H:%M:%S.%f')[:-3]}")
print(f"  M2 (fault to stick3 recovered) = {M2_fault2:.1f}s")
print()

# ── FAULT 1: drop_stick6_on_pickup ──────────────────────────────────────────
# Triggered at ML24 (PickUpML_stick6) under HL14 (call #14)
c14_start = call_start(14)               # 17:05:08.257
c14_plan_ms = call_plan_ms(14)           # 2189ms
c14_done = add_ms(c14_start, c14_plan_ms)

ML23_ms = 13743.0   # TravelML before PickUpML_stick6
t_ML24_start = add_ms(c14_done, ML23_ms)
t_fault1 = t_ML24_start                  # ML24 fails at 0ms

# ML replan (call #15)
c15_start = call_start(15)               # 17:05:31.061
c15_plan_ms = call_plan_ms(15)           # 2099ms
c15_done = add_ms(c15_start, c15_plan_ms)

# Recovery ML actions under call #15 plan
ML25_ms = 6174.0    # TravelML to new pickup location (temploc1)
ML26_ms = 17447.0   # PickUpML_stick6_temploc1
t_ML25_start = c15_done
t_ML26_start = add_ms(t_ML25_start, ML25_ms)
t_ML26_done  = add_ms(t_ML26_start, ML26_ms)  # stick6 successfully picked up

gap_fault1_to_replan = (c15_start - t_fault1).total_seconds()
M2_fault1 = (t_ML26_done - t_fault1).total_seconds()
gap_fault1_to_plan = (c15_done - t_fault1).total_seconds()

print("=== FAULT 1: drop_stick6_on_pickup ===")
print(f"  t_fault1       = {t_fault1.strftime('%H:%M:%S.%f')[:-3]}")
print(f"  ML replan call #15 at {c15_start.strftime('%H:%M:%S.%f')[:-3]} ({gap_fault1_to_replan:.1f}s after fault)")
print(f"  M3 (replan latency) = {c15_plan_ms:.0f}ms = {c15_plan_ms/1000:.2f}s  [ML PDDL replan]")
print(f"  Plan ready at {c15_done.strftime('%H:%M:%S.%f')[:-3]} ({gap_fault1_to_plan:.1f}s after fault)")
print(f"  Recovery: ML25({ML25_ms:.0f}ms travel) + ML26({ML26_ms:.0f}ms pickup) = {(ML25_ms+ML26_ms)/1000:.1f}s")
print(f"  Stick6 successfully picked up at {t_ML26_done.strftime('%H:%M:%S.%f')[:-3]}")
print(f"  M2 (fault to stick6 recovered) = {M2_fault1:.1f}s")
print()

print("=== SUMMARY ===")
print(f"  M1 (recovery success rate, execution faults) = 3/3 = 100%")
print()
print(f"  F3 dislodge_stick5: M2 = {gap_fault3_to_action:.1f}s (to first action of recovery plan)  M3 = {c8_plan_ms/1000:.2f}s (HL PDDL)")
print(f"  F2 blocker_on_stick3: M2 = {M2_fault2:.1f}s (fault to stick3 recovered)  M3 = {c10_plan_ms/1000:.2f}s (ML PDDL)")
print(f"  F1 drop_stick6:      M2 = {M2_fault1:.1f}s (fault to stick6 recovered)  M3 = {c15_plan_ms/1000:.2f}s (ML PDDL)")
print()
# Note on F3 M2: For F3, the "recovery" is the HL replan itself (plan regenerated to accommodate dislodged stick5).
# t_resume = when first action of new HL plan started executing.
# F3 triggered HL-level replan; F2 and F1 triggered ML-level replan.

import csv
from datetime import datetime

hl = list(csv.DictReader(open('HierarchicalTrace_HL_2026-05-29_20-07-55.csv', encoding='utf-8-sig')))
ml = list(csv.DictReader(open('HierarchicalTrace_ML_2026-05-29_20-07-55.csv', encoding='utf-8-sig')))
pc = list(csv.DictReader(open('PlannerCalls_2026-05-29_20-07-55.csv', encoding='utf-8-sig')))

# Key HL fault events identified:
# HL7:  StackHL_stick2 → FAILED   (fault 3: dislodge_stick5_after_stack triggered during StackML for stick2)
# HL8:  Layers1_2      → OK       (HL replan succeeded after fault 3)
# HL9:  PickUpHL_stick3_dup2 → FAILED  (fault 2: blocker placed on stick3 during TravelML)
# HL10: PickUpHL_stick3_dup2 → OK      (fault 2 recovery)
# HL14: PickUpHL_stick6_dup2 → FAILED  (fault 1: drop after close_gripper)
# HL15: PickUpHL_stick6_dup2 → OK      (fault 1 recovery)

print("=== PLANNER CALLS for fault-related HL actions ===")
fault_keywords = ['stick6', 'stick3', 'stick2', 'temploc', 'stickdummy', 'Layers1_2', 'Layers']
for r in pc:
    hi = r['HLActionInstance']
    pf = r['ProblemFile']
    # Show calls for key fault actions
    if ('PickUpHL_stick6' in hi or 'PickUpHL_stick3' in hi or 
        'StackHL_stick2' in hi or 'Layers' in hi or 'temploc' in pf):
        print(f"  #{r['CallNumber']:>3}  {r['Timestamp']}  {hi[:65]:<65}  plan={float(r['PlannerTimeMs']):.0f}ms  ok={r['Success']}")

print()
print("=== PLANNER CALLS 1-26 (to find fault 1, 2, 3 timing) ===")
for r in pc:
    if int(r['CallNumber']) <= 26:
        print(f"  #{r['CallNumber']:>3}  {r['Timestamp']}  {r['HLActionInstance'][:65]:<65}  plan={float(r['PlannerTimeMs']):.0f}ms")

print()
print("=== ML rows 1-30 with parent HL ===")
for r in ml:
    if int(r['MLId']) <= 30:
        print(f"  ML{r['MLId']:>3}  {r['ActionType']:<18} {r['InstanceName'][:55]:<55} ok={r['Success']} t={float(r['TotalTimeMs']):.0f}ms  HL='{r['ParentHLAction'][:40]}'")

print()
print("=== HL rows 1-20 key timing ===")
for r in hl:
    if int(r['HLId']) <= 20:
        print(f"  HL{r['HLId']:>3}  ok={r['Success']}  plan={float(r['PlannerTimeMs']):.0f}ms  total={float(r['TotalTimeMs'])/1000:.1f}s  {r['ActionName'][:60]}")

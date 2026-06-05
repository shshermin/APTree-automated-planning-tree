import csv

hl = list(csv.DictReader(open('HierarchicalTrace_HL_2026-05-29_20-07-55.csv', encoding='utf-8-sig')))
ml = list(csv.DictReader(open('HierarchicalTrace_ML_2026-05-29_20-07-55.csv', encoding='utf-8-sig')))
pc = list(csv.DictReader(open('PlannerCalls_2026-05-29_20-07-55.csv', encoding='utf-8-sig')))

print('=== ALL HL rows (Id, Name, Success, PlannerTimeMs, TotalTimeMs) ===')
for r in hl:
    print(f"  HL{r['HLId']:>3}  {r['ActionName'][:70]:<70}  ok={r['Success']}  plan={float(r['PlannerTimeMs']):.0f}ms  total={float(r['TotalTimeMs']):.0f}ms")

print()
print('=== ML rows 1-30 ===')
for r in ml:
    if int(r['MLId']) <= 30:
        print(f"  ML{r['MLId']:>3}  {r['ActionType']:<18}  {r['InstanceName'][:65]:<65}  ok={r['Success']}  total={float(r['TotalTimeMs']):.0f}ms  parent={r['ParentHLAction'][:50]}")

print()
print('=== Planner calls 1-50 (CallNumber, Timestamp, HLActionInstance, PlannerTimeMs) ===')
for r in pc:
    if int(r['CallNumber']) <= 50:
        print(f"  #{r['CallNumber']:>3}  {r['Timestamp']}  {r['HLActionInstance'][:65]:<65}  plan={float(r['PlannerTimeMs']):.0f}ms  ok={r['Success']}")

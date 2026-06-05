import csv

rows = list(csv.DictReader(open(
    r'c:\Users\sherk\Documents\BehaviorTreeMainProject\APTreeExecutionEngine\kept logs\demonstrator_2026-05-29_16-58-37\BehaviorTreeComponentSummary_2026-05-29_20-07-54.csv',
    encoding='utf-8-sig')))

# ── category sums ────────────────────────────────────────────────────────────
generic   = next(r for r in rows if r['ComponentType'] == 'GenericBTAction')
flow_rows = [r for r in rows if 'Flow' in r['ComponentType'] or 'Composite' in r['ComponentType']]
svc_rows  = [r for r in rows if r['ComponentType'].startswith('Service') or r['ComponentType'].startswith('Dummy')]
dec_rows  = [r for r in rows if r['ComponentType'].startswith('Decorator')]

generic_ticks = int(generic['TickCount'])   # HL+ML combined
generic_inst  = int(generic['InstanceCount'])  # HL+ML combined additions

# Split HL/ML proportionally by known execution counts
hl_exec, ml_exec = 333, 631
total_exec = hl_exec + ml_exec
hl_ticks = round(generic_ticks * hl_exec / total_exec)
ml_ticks = generic_ticks - hl_ticks

# LL actions: DecoratorLLInputResolver ticks = exactly 1318 (each LL resolved once)
# LL actions themselves are not separately logged; use execution count as proxy
ll_ticks = 1318

flow_ticks = sum(int(r['TickCount']) for r in flow_rows)
svc_ticks  = sum(int(r['TickCount']) for r in svc_rows)
dec_ticks  = sum(int(r['TickCount']) for r in dec_rows)

print("Category             | Ticks")
print("-" * 40)
print(f"HL Action Nodes      | {hl_ticks:,}  (GenericBTAction split {hl_exec}/{total_exec})")
print(f"ML Action Nodes      | {ml_ticks:,}  (GenericBTAction split {ml_exec}/{total_exec})")
print(f"LL Action Nodes      | {ll_ticks:,}  (= execution count; each LL call is synchronous)")
print(f"Flow/Control Nodes   | {flow_ticks:,}")
print(f"Services             | {svc_ticks:,}")
print(f"Decorators           | {dec_ticks:,}")
print(f"TOTAL                | {hl_ticks+ml_ticks+ll_ticks+flow_ticks+svc_ticks+dec_ticks:,}")
print()
print("Flow/Control breakdown:")
for r in flow_rows:
    print(f"  {r['ComponentType']:<35} ticks={r['TickCount']}")
print("Services breakdown:")
for r in svc_rows:
    print(f"  {r['ComponentType']:<35} ticks={r['TickCount']}")

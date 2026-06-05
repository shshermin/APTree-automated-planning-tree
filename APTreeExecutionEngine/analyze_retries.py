"""
Find retry-on-failure timing from logs that have LL-level failures.
Strategy: a failed LL row followed immediately by the same LL action succeeding = one retry cycle.
M2_retry = ExecTime of retry attempt (the successful one, since the failed one is 0ms).
Also check ML context to understand whether the retry was transparent or caused an ML failure.
"""
import csv, os

def analyze_retries(ll_path, ml_path, pc_path=None):
    ll = list(csv.DictReader(open(ll_path, encoding='utf-8-sig')))
    ml = list(csv.DictReader(open(ml_path, encoding='utf-8-sig')))
    pc = list(csv.DictReader(open(pc_path, encoding='utf-8-sig'))) if pc_path and os.path.exists(pc_path) else []

    failed_ll = [(i, r) for i, r in enumerate(ll) if r['Success'] == 'False']
    print(f'Failed LL rows: {len(failed_ll)}')

    for idx, r in failed_ll:
        action_base = r['ActionType']
        inst = r['InstanceName']
        parent = r['ParentMLAction']
        print(f"\n  Failed: LL{r['LLId']} {action_base} | {inst[:60]}")
        print(f"          Parent ML: {parent[:60]}")

        # Look at surrounding LL rows ±5
        window = ll[max(0, idx-2): idx+6]
        for w in window:
            marker = ' <-- FAILED' if w['LLId'] == r['LLId'] else ''
            print(f"    LL{w['LLId']:>4}  ok={w['Success']}  {w['ActionType']:<18}  {w['InstanceName'][:50]:<50}  exec={float(w['ExecTimeMs']):.0f}ms{marker}")

        # Check if the parent ML also failed
        parent_ml = [m for m in ml if m['InstanceName'].startswith(parent[:30]) or parent[:30] in m.get('InstanceName','')]
        for m in parent_ml[:3]:
            print(f"    → ML{m['MLId']} ok={m['Success']} {m['ActionType']} total={float(m['TotalTimeMs']):.0f}ms")

# ── 2026-04-13 run ───────────────────────────────────────────────────────────
print("=" * 70)
print("RUN: 2026-04-13")
print("=" * 70)
base = r'c:\Users\sherk\Documents\BehaviorTreeMainProject\APTreeExecutionEngine\kept logs'
analyze_retries(
    os.path.join(base, 'HierarchicalTrace_LL_2026-04-13_19-51-18.csv'),
    os.path.join(base, 'HierarchicalTrace_ML_2026-04-13_19-51-18.csv'),
)

# ── 2026-05-04 run ───────────────────────────────────────────────────────────
print()
print("=" * 70)
print("RUN: 2026-05-04")
print("=" * 70)
base2 = r'c:\Users\sherk\Documents\BehaviorTreeMainProject\APTreeExecutionEngine\kept logs\demonstrator'
analyze_retries(
    os.path.join(base2, 'HierarchicalTrace_LL_2026-05-04_17-44-29.csv'),
    os.path.join(base2, 'HierarchicalTrace_ML_2026-05-04_17-44-29.csv'),
    os.path.join(base2, 'PlannerCalls_2026-05-04_17-44-28.csv'),
)

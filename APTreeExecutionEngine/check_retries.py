import csv, os

log_dirs = [
    r'c:\Users\sherk\Documents\BehaviorTreeMainProject\APTreeExecutionEngine\kept logs',
    r'c:\Users\sherk\Documents\BehaviorTreeMainProject\APTreeExecutionEngine\kept logs\demonstrator',
    r'c:\Users\sherk\Documents\BehaviorTreeMainProject\APTreeExecutionEngine\kept logs\demonstrator_2026-05-29_16-58-37',
]

for d in log_dirs:
    for fname in os.listdir(d):
        if 'BehaviorTreeComponentSummary' in fname and fname.endswith('.csv'):
            path = os.path.join(d, fname)
            rows = list(csv.DictReader(open(path, encoding='utf-8-sig')))
            retry_rows = [r for r in rows if 'Retry' in r.get('ComponentType','')]
            fault_rows = [r for r in rows if 'Fault' in r.get('ComponentType','')]
            ll_rows = [r for r in rows if 'LL' in r.get('ComponentType','')]
            print(f'=== {path} ===')
            for r in retry_rows + fault_rows:
                print(f"  {r['ComponentType']:<40} ticks={r['TickCount']}  success={r['SuccessCount']}  fail={r['FailureCount']}  additions={r['AdditionCount']}")
            print()

# Also check LL trace for failures in all logs
for d in log_dirs:
    for fname in os.listdir(d):
        if 'HierarchicalTrace_LL' in fname and fname.endswith('.csv'):
            path = os.path.join(d, fname)
            ll = list(csv.DictReader(open(path, encoding='utf-8-sig')))
            failed = [r for r in ll if r.get('Success','') == 'False']
            print(f'=== LL trace: {os.path.basename(path)} ===')
            print(f'  Total: {len(ll)}  Failed: {len(failed)}')
            for r in failed[:10]:
                print(f"  LL{r['LLId']}  {r['ActionType']}  {r['InstanceName'][:60]}  ExecTime={r['ExecTimeMs']}ms")
            print()

# Also check ML trace for failures in all logs
for d in log_dirs:
    for fname in os.listdir(d):
        if 'HierarchicalTrace_ML' in fname and fname.endswith('.csv'):
            path = os.path.join(d, fname)
            ml = list(csv.DictReader(open(path, encoding='utf-8-sig')))
            failed = [r for r in ml if r.get('Success','') == 'False']
            print(f'=== ML trace: {os.path.basename(path)} ===')
            print(f'  Total: {len(ml)}  Failed: {len(failed)}')
            for r in failed[:10]:
                print(f"  ML{r['MLId']}  {r['ActionType']}  {r['InstanceName'][:60]}  TotalTimeMs={r['TotalTimeMs']}")
            print()

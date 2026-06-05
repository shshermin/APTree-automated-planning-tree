import csv

rows = list(csv.DictReader(open(
    r'c:\Users\sherk\Documents\BehaviorTreeMainProject\APTreeExecutionEngine\kept logs\demonstrator_2026-05-29_16-58-37\BehaviorTreeComponentSummary_2026-05-29_20-07-54.csv',
    encoding='utf-8-sig')))

print(f"{'ComponentType':<45} instances  ticks      additions")
for r in rows:
    print(f"{r['ComponentType']:<45} {r['InstanceCount']:>9}  {r['TickCount']:>9}  {r['AdditionCount']:>9}")

# Group into table categories
# HL/ML/LL action nodes: from HierarchicalTrace CSVs (counts already known)
# Flow/Control: BTFlowNodeComposite
# Services: Service* types
# Decorators: Decorator* types

flow_ticks = sum(int(r['TickCount']) for r in rows if 'Flow' in r['ComponentType'] or 'Composite' in r['ComponentType'])
service_ticks = sum(int(r['TickCount']) for r in rows if r['ComponentType'].startswith('Service') or r['ComponentType'].startswith('Dummy'))
decorator_ticks = sum(int(r['TickCount']) for r in rows if r['ComponentType'].startswith('Decorator'))

print()
print(f"Flow/Control nodes total ticks: {flow_ticks}")
print(f"Services total ticks:           {service_ticks}")
print(f"Decorators total ticks:         {decorator_ticks}")

# HL/ML/LL ticks: from HierarchicalTrace row counts (each row = 1 execution = ticked once to completion)
# But they are ticked multiple times during execution. Check HLActionNode etc.
hl_ticks = sum(int(r['TickCount']) for r in rows if 'HL' in r['ComponentType'] and 'Decorator' not in r['ComponentType'] and 'Service' not in r['ComponentType'])
ml_ticks = sum(int(r['TickCount']) for r in rows if 'ML' in r['ComponentType'] and 'Decorator' not in r['ComponentType'] and 'Service' not in r['ComponentType'])
ll_ticks = sum(int(r['TickCount']) for r in rows if 'LL' in r['ComponentType'] and 'Decorator' not in r['ComponentType'] and 'Service' not in r['ComponentType'])

print(f"HL action ticks:                {hl_ticks}")
print(f"ML action ticks:                {ml_ticks}")
print(f"LL action ticks:                {ll_ticks}")

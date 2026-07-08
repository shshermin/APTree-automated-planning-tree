import pandas as pd
import re
import os
from datetime import datetime

def parse_log(path):
    timestamps = []
    pattern = re.compile(r'\[\d+\]\s+\[(\d{2}:\d{2}:\d{2}\.\d{3})\]')
    with open(path, 'r', encoding='utf-8') as f:
        for line in f:
            m = pattern.search(line)
            if m:
                ts = datetime.strptime(m.group(1), '%H:%M:%S.%f')
                timestamps.append(ts)
    return timestamps

with_ts = parse_log('version2_with_replanning_2026-07-01/MLActionResult_2026-07-01_14-05-19.log')
without_ts = parse_log('version1_without_replanning_2026-07-01/MLActionResult_2026-07-01_12-20-47.log')
with_d = [(with_ts[i]-with_ts[i-1]).total_seconds()*1000 for i in range(1,len(with_ts))]
without_d = [(without_ts[i]-without_ts[i-1]).total_seconds()*1000 for i in range(1,len(without_ts))]

print('=== Time Delta (With Replanning) ===')
print(f'Mean: {sum(with_d)/len(with_d):.1f} ms')
print(f'Median: {sorted(with_d)[len(with_d)//2]:.1f} ms')
print(f'Max: {max(with_d):.1f} ms')
print(f'<2000ms: {sum(1 for d in with_d if d<2000)}/{len(with_d)}')
print(f'2000-6000ms: {sum(1 for d in with_d if 2000<=d<6000)}/{len(with_d)}')
print()
print('=== Time Delta (Without Replanning) ===')
print(f'Mean: {sum(without_d)/len(without_d):.1f} ms')
print(f'Median: {sorted(without_d)[len(without_d)//2]:.1f} ms')
print(f'Max: {max(without_d):.1f} ms')
print()

df = pd.read_csv('version2_with_replanning_2026-07-01/PlannerCalls_2026-07-01_14-18-41.csv')
df['TotalTimeMs'] = pd.to_numeric(df['TotalTimeMs'], errors='coerce')
df['PlannerTimeMs'] = pd.to_numeric(df['PlannerTimeMs'], errors='coerce')
print('=== Planner Service Latency ===')
print(f'Total calls: {len(df)}')
print(f'Mean total time: {df["TotalTimeMs"].mean():.1f} ms')
print(f'Mean planner time: {df["PlannerTimeMs"].mean():.1f} ms')
print(f'Median total time: {df["TotalTimeMs"].median():.1f} ms')
print(f'Max total time: {df["TotalTimeMs"].max():.1f} ms')
overhead = df["TotalTimeMs"].mean() - df["PlannerTimeMs"].mean()
print(f'Network overhead (total-planner): {overhead:.1f} ms')

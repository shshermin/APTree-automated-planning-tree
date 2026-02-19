import csv
import os
import matplotlib.pyplot as plt

script_dir = os.path.dirname(os.path.abspath(__file__))

def read_elapsed(csv_path):
    """Read ElapsedMs column and compute inter-action deltas (ms)."""
    elapsed = []
    with open(csv_path, encoding="utf-8-sig") as f:
        reader = csv.DictReader(f)
        for row in reader:
            elapsed.append(int(row["ElapsedMs"]))
    # Compute delta between consecutive actions
    deltas = [elapsed[i] - elapsed[i - 1] for i in range(1, len(elapsed))]
    return deltas

csv1 = os.path.join(script_dir, "MLActionResult_2026-02-17_16-53-04.csv")
csv2 = os.path.join(script_dir, "MLActionResult_2026-02-18_15-17-06.csv")

deltas1 = read_elapsed(csv1)
deltas2 = read_elapsed(csv2)

# Use the shorter length so both arrays align
n = min(len(deltas1), len(deltas2))
deltas1 = deltas1[:n]
deltas2 = deltas2[:n]
indices = list(range(1, n + 1))

fig, ax = plt.subplots(figsize=(14, 6))

ax.scatter(indices, deltas1, s=18, alpha=0.7, label="With replanning", color="#2196F3", zorder=3)
ax.scatter(indices, deltas2, s=18, alpha=0.7, label="Without replanning", color="#FF5722", zorder=3)

ax.set_xlabel("Action Step (transition index)", fontsize=12)
ax.set_ylabel("Time to next action (ms)", fontsize=12)
ax.set_title("Time Delta Between Actions: With Replanning vs Without Replanning", fontsize=14)
ax.legend(fontsize=10)
ax.grid(True, alpha=0.3)

plt.tight_layout()
out_path = os.path.join(script_dir, "MLActionResult_ScatterComparison.png")
plt.savefig(out_path, dpi=150)
print(f"Chart saved to: {out_path}")
plt.close()

"""
PlanSys2: All Runs Timeline Comparison (one row per run)
Shows monolithic plan-then-execute pattern and where it fails
"""
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
import numpy as np

# === Color scheme (same as APTree diagram) ===
colors = {
    'hl_plan': '#2196F3',      # Blue
    'ml_plan': '#FF9800',      # Orange  
    'execute': '#9E9E9E',      # Grey
    'failed': '#F44336',       # Red
}

# === P0: PickUp lp1 only — TBD ===
ps2_p0_phases = [
    (0.0, 0.5, "TFD Planning", "ml_plan"),
    (0.5, 0.5, "Exec (5 actions)", "execute"),
]

# === P1: Pick+Place lp1 (no gluing) ===
# From log: Search=0.85s, Plan=7 steps, Makespan=23.05, Total=2.0s
ps2_p1_phases = [
    (0.0, 0.85, "TFD Planning\n(7 actions)", "ml_plan"),
    (0.85, 1.17, "Execution\n(7 actions)", "execute"),
]

# === P2: Pick+Place+Glue lp1 ===
# From log: Search=5.7s, Plan=15 steps, Makespan=49.14, Total=7.6s, Exec=1.6s
ps2_p2_phases = [
    (0.0, 5.71, "TFD Planning\n(15 actions)", "ml_plan"),
    (5.71, 1.60, "Execution\n(15 actions)", "execute"),
]

# === P3: P2 + Pick+Stack b1 — OOM ===
ps2_p3_phases = [
    (0.0, 10.0, "TFD Planning → OUT OF MEMORY", "failed"),
]

# === P4: P3 + Pick+Stack b2 — OOM ===
ps2_p4_phases = [
    (0.0, 10.0, "TFD Planning → OUT OF MEMORY", "failed"),
]

all_runs = [
    ("P0: PickUp lp1", ps2_p0_phases, 1.0),
    ("P1: Pick+Place lp1", ps2_p1_phases, 2.0),
    ("P2: Pick+Place+Glue lp1", ps2_p2_phases, 7.6),
    ("P3: P2 + Stack b1", ps2_p3_phases, None),
    ("P4: P3 + Stack b2", ps2_p4_phases, None),
]

def draw_timeline(ax, phases, y_center, height, show_labels=True, min_label_width=0.5):
    """Draw phases as colored blocks on a timeline"""
    for start, duration, label, phase_type in phases:
        color = colors[phase_type]
        rect = mpatches.FancyBboxPatch(
            (start, y_center - height/2), duration, height,
            boxstyle="round,pad=0.01",
            facecolor=color, edgecolor='black', linewidth=0.5, alpha=0.85
        )
        ax.add_patch(rect)
        if show_labels and duration >= min_label_width:
            fontsize = 5.5 if duration < 0.8 else 6.5
            text_color = 'white' if phase_type == 'failed' else 'black'
            ax.text(start + duration/2, y_center, label,
                   ha='center', va='center', fontsize=fontsize, fontweight='bold',
                   color=text_color)


# ========== FIGURE: All 5 PlanSys2 runs ==========
fig, ax = plt.subplots(figsize=(14, 6))
fig.suptitle("PlanSys2 Monolithic Planning: Execution Timeline for Increasing Problem Complexity\n"
             "(Each row = one run; TFD planning in orange, execution in grey, failure in red)",
             fontsize=11, fontweight='bold')

n_runs = len(all_runs)
row_height = 0.6
row_spacing = 1.2

for i, (label, phases, total_time) in enumerate(reversed(all_runs)):
    y = i * row_spacing + 0.5
    draw_timeline(ax, phases, y, row_height, min_label_width=0.55)
    
    # End marker
    end_x = phases[-1][0] + phases[-1][1]
    if total_time is not None:
        ax.axvline(x=end_x, ymin=(y - row_height/2 - 0.1) / (n_runs * row_spacing),
                   ymax=(y + row_height/2 + 0.1) / (n_runs * row_spacing),
                   color='green', linestyle='--', alpha=0.6, linewidth=1)
        ax.text(end_x + 0.1, y + row_height/2 + 0.05, f"✓ {total_time:.1f}s",
                fontsize=7, color='green', va='bottom')
    else:
        ax.text(end_x + 0.1, y, "✗ OOM",
                fontsize=8, color='red', va='center', fontweight='bold')

# Y-axis labels
y_positions = [i * row_spacing + 0.5 for i in range(n_runs)]
y_labels = [label for label, _, _ in reversed(all_runs)]
ax.set_yticks(y_positions)
ax.set_yticklabels(y_labels, fontsize=9, fontweight='bold')

ax.set_xlabel("Time (seconds)", fontsize=10)
ax.set_xlim(-0.5, 12)
ax.set_ylim(-0.3, n_runs * row_spacing)
ax.grid(axis='x', alpha=0.2)

# Legend
legend_patches = [
    mpatches.Patch(color=colors['ml_plan'], label='TFD Planning (monolithic)'),
    mpatches.Patch(color=colors['execute'], label='Action Execution'),
    mpatches.Patch(color=colors['failed'], label='Failed (Out of Memory)'),
]
ax.legend(handles=legend_patches, loc='lower right', fontsize=9)

plt.tight_layout()
plt.savefig("PlanSys2_all_runs_timeline.png", dpi=150, bbox_inches='tight')
plt.savefig("PlanSys2_all_runs_timeline.pdf", bbox_inches='tight')
print("Saved: PlanSys2_all_runs_timeline.png/pdf")
plt.show()

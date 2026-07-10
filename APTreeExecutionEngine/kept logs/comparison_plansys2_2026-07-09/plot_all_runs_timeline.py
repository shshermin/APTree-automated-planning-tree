"""
APTree: All 5 Runs Timeline Comparison (one row per run)
Shows how the interleaved plan-execute pattern scales with problem complexity
"""
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
import numpy as np

# === Color scheme ===
colors = {
    'hl_plan': '#2196F3',      # Blue
    'ml_plan': '#FF9800',      # Orange  
    'execute': '#9E9E9E',      # Grey
    'bt_overhead': '#BDBDBD',  # Light Grey
}

# === P0: PickUp lp1 only (1 HL action, 1 ML call) ===
p0_phases = [
    (0.0, 0.71, "HL Plan", "hl_plan"),
    (0.71, 0.74, "ML Plan: PickUp lp1", "ml_plan"),
    (1.45, 0.50, "Exec (5 actions)", "execute"),
]

# === P1: Pick+Place lp1 (2 HL actions, 2 ML calls) ===
p1_phases = [
    (0.0, 0.71, "HL Plan", "hl_plan"),
    (0.71, 0.72, "ML: PickUp", "ml_plan"),
    (1.43, 0.50, "Exec (5)", "execute"),
    (1.93, 0.72, "ML: Place", "ml_plan"),
    (2.65, 0.20, "Exec (2)", "execute"),
]

# === P2: Pick+Place+Glue lp1 (3 HL actions, 3 ML calls) ===
p2_phases = [
    (0.0, 0.70, "HL Plan", "hl_plan"),
    (0.70, 0.74, "ML: PickUp", "ml_plan"),
    (1.44, 0.50, "Exec (5)", "execute"),
    (1.94, 0.72, "ML: Place", "ml_plan"),
    (2.66, 0.20, "Exec (2)", "execute"),
    (2.86, 0.75, "ML: Glue", "ml_plan"),
    (3.61, 0.80, "Exec (8)", "execute"),
]

# === P3: P2 + Pick+Stack b1 (5 HL actions, 5 ML calls) ===
p3_phases = [
    (0.0, 0.74, "HL Plan", "hl_plan"),
    (0.74, 0.74, "ML: PickUp lp1", "ml_plan"),
    (1.48, 0.50, "Exec (5)", "execute"),
    (1.98, 0.72, "ML: Place", "ml_plan"),
    (2.70, 0.20, "Exec (2)", "execute"),
    (2.90, 0.75, "ML: Glue", "ml_plan"),
    (3.65, 0.80, "Exec (8)", "execute"),
    (4.45, 0.69, "ML: PickUp b1", "ml_plan"),
    (5.14, 0.20, "Exec (2)", "execute"),
    (5.34, 0.72, "ML: Stack b1", "ml_plan"),
    (6.06, 0.20, "Exec (2)", "execute"),
]

# === P4: P3 + Pick+Stack b2 (7 HL actions, 7 ML calls) ===
p4_phases = [
    (0.0, 0.84, "HL Plan", "hl_plan"),
    (0.84, 0.74, "ML: PickUp lp1", "ml_plan"),
    (1.58, 0.50, "Exec (5)", "execute"),
    (2.08, 0.72, "ML: Place", "ml_plan"),
    (2.80, 0.20, "Exec (2)", "execute"),
    (3.00, 0.75, "ML: Glue", "ml_plan"),
    (3.75, 0.80, "Exec (8)", "execute"),
    (4.55, 0.75, "ML: PickUp b2", "ml_plan"),
    (5.30, 0.90, "Exec (9)", "execute"),
    (6.20, 0.75, "ML: Stack b2", "ml_plan"),
    (6.95, 0.20, "Exec (2)", "execute"),
    (7.15, 0.75, "ML: PickUp b1", "ml_plan"),
    (7.90, 0.20, "Exec (2)", "execute"),
    (8.10, 0.75, "ML: Stack b1", "ml_plan"),
    (8.85, 0.20, "Exec (2)", "execute"),
]

all_runs = [
    ("P0: PickUp lp1", p0_phases, 4.1),
    ("P1: Pick+Place lp1", p1_phases, 6.0),
    ("P2: Pick+Place+Glue lp1", p2_phases, 9.8),
    ("P3: P2 + Stack b1", p3_phases, 16.6),
    ("P4: P3 + Stack b2", p4_phases, 10.2),
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
            ax.text(start + duration/2, y_center, label,
                   ha='center', va='center', fontsize=fontsize, fontweight='bold')


# ========== FIGURE: All 5 APTree runs ==========
fig, ax = plt.subplots(figsize=(14, 6))
fig.suptitle("APTree Hierarchical Planning: Execution Timeline for Increasing Problem Complexity\n"
             "(Each row = one run; HL planning in blue, ML planning in orange, execution in green)",
             fontsize=11, fontweight='bold')

n_runs = len(all_runs)
row_height = 0.6
row_spacing = 1.2

for i, (label, phases, total_time) in enumerate(reversed(all_runs)):
    y = i * row_spacing + 0.5
    draw_timeline(ax, phases, y, row_height, min_label_width=0.55)
    
    # End marker
    end_x = phases[-1][0] + phases[-1][1]
    ax.axvline(x=end_x, ymin=(y - row_height/2 - 0.1) / (n_runs * row_spacing),
               ymax=(y + row_height/2 + 0.1) / (n_runs * row_spacing),
               color='green', linestyle='--', alpha=0.6, linewidth=1)
    ax.text(end_x + 0.1, y + row_height/2 + 0.05, f"✓ {end_x:.1f}s",
            fontsize=7, color='green', va='bottom')

# Y-axis labels
y_positions = [i * row_spacing + 0.5 for i in range(n_runs)]
y_labels = [label for label, _, _ in reversed(all_runs)]
ax.set_yticks(y_positions)
ax.set_yticklabels(y_labels, fontsize=9, fontweight='bold')

ax.set_xlabel("Time (seconds)", fontsize=10)
ax.set_xlim(-0.5, 14)
ax.set_ylim(-0.3, n_runs * row_spacing)
ax.grid(axis='x', alpha=0.2)

# Legend
legend_patches = [
    mpatches.Patch(color=colors['hl_plan'], label='HL Planning (ENHSP)'),
    mpatches.Patch(color=colors['ml_plan'], label='ML Planning (TFD per sub-goal)'),
    mpatches.Patch(color=colors['execute'], label='Action Execution'),
]
ax.legend(handles=legend_patches, loc='lower right', fontsize=9)



plt.tight_layout()
plt.savefig("APTree_all_runs_timeline.png", dpi=150, bbox_inches='tight')
plt.savefig("APTree_all_runs_timeline.pdf", bbox_inches='tight')
print("Saved: APTree_all_runs_timeline.png/pdf")
plt.show()

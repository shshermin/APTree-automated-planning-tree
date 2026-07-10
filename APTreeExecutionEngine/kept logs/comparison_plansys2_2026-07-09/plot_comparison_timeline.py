"""
APTree vs PlanSys2: Timeline Comparison Diagram
Shows interleaved plan-execute pattern (APTree) vs monolithic plan-then-execute (PlanSys2)
"""
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
import numpy as np

# === APTree P4 data (Pick+Place+Glue lp1 + Stack b1 + b2) ===
# From planning logs: timestamps show when each call starts/ends
# HL planning: 4082ms, then 7 ML calls interleaved with execution

aptree_phases = [
    # (start_s, duration_s, label, phase_type)
    # Phase types: 'hl_plan', 'ml_plan', 'execute', 'bt_overhead'
    
    # Tick 1: HL Planning
    (0.0, 4.08, "HL Plan\n(ENHSP)", "hl_plan"),
    
    # Tick 2: Phase switch + ML plan PickUpHL_lp1
    (4.08, 0.74, "ML Plan\nPickUp lp1", "ml_plan"),
    (4.82, 0.50, "Execute\n5 ML actions", "execute"),
    
    # PlaceHL_lp1
    (5.32, 0.72, "ML Plan\nPlace lp1", "ml_plan"),
    (6.04, 0.20, "Execute\n2 ML actions", "execute"),
    
    # GluingPlateHL_lp1
    (6.24, 0.75, "ML Plan\nGlue lp1", "ml_plan"),
    (6.99, 0.80, "Execute\n8 ML actions", "execute"),
    
    # PickUpHL_b2
    (7.79, 0.69, "ML Plan\nPickUp b2", "ml_plan"),
    (8.48, 0.90, "Execute\n9 ML actions", "execute"),
    
    # StackHL_b2
    (9.38, 0.72, "ML Plan\nStack b2", "ml_plan"),
    (10.10, 0.20, "Execute\n2 ML actions", "execute"),
    
    # PickUpHL_b1
    (10.30, 0.64, "ML Plan\nPickUp b1", "ml_plan"),
    (10.94, 0.20, "Execute\n2 ML actions", "execute"),
    
    # StackHL_b1
    (11.14, 0.88, "ML Plan\nStack b1", "ml_plan"),
    (12.02, 0.20, "Execute\n2 ML actions", "execute"),
]

# === PlanSys2 P4 equivalent (monolithic TFD) ===
plansys2_phases = [
    # Monolithic planning attempt - runs out of memory
    (0.0, 20.0, "TFD Planning\n(monolithic)\n→ OUT OF MEMORY", "failed"),
]

# === PlanSys2 P2 (Pick+Place+Glue - the one that works) ===
# Approximate from user's reported data
plansys2_p2_phases = [
    (0.0, 1.1, "TFD Planning\n(monolithic)\n30 actions", "ml_plan"),
    (1.1, 3.0, "Execute\n30 actions\nsequentially", "execute"),
]

aptree_p2_phases = [
    (0.0, 0.70, "HL Plan", "hl_plan"),
    (0.70, 0.74, "ML Plan\nPickUp", "ml_plan"),
    (1.44, 0.50, "Exec", "execute"),
    (1.94, 0.72, "ML Plan\nPlace", "ml_plan"),
    (2.66, 0.20, "Exec", "execute"),
    (2.86, 0.75, "ML Plan\nGlue", "ml_plan"),
    (3.61, 0.80, "Exec", "execute"),
]

# === Color scheme ===
colors = {
    'hl_plan': '#2196F3',      # Blue
    'ml_plan': '#FF9800',      # Orange
    'execute': '#4CAF50',      # Green
    'bt_overhead': '#9E9E9E',  # Grey
    'failed': '#F44336',       # Red
}

def draw_timeline(ax, phases, y_center, height, show_labels=True):
    """Draw phases as colored blocks on a timeline"""
    for start, duration, label, phase_type in phases:
        color = colors[phase_type]
        rect = mpatches.FancyBboxPatch(
            (start, y_center - height/2), duration, height,
            boxstyle="round,pad=0.02",
            facecolor=color, edgecolor='black', linewidth=0.5, alpha=0.85
        )
        ax.add_patch(rect)
        if show_labels and duration > 0.4:
            ax.text(start + duration/2, y_center, label,
                   ha='center', va='center', fontsize=6, fontweight='bold',
                   color='white' if phase_type == 'failed' else 'black')


# ========== FIGURE 1: P4 Comparison (full problem) ==========
fig, axes = plt.subplots(2, 1, figsize=(14, 4), sharex=True)
fig.suptitle("APTree vs PlanSys2: Execution Timeline Comparison\n(Problem P4: Pick+Place+Glue lp1 + Stack b1,b2)",
             fontsize=11, fontweight='bold')

# APTree timeline
ax1 = axes[0]
ax1.set_ylabel("APTree\n(hierarchical)", fontsize=9, fontweight='bold')
draw_timeline(ax1, aptree_phases, 0.5, 0.7)
ax1.set_ylim(0, 1)
ax1.set_yticks([])
ax1.axvline(x=12.22, color='green', linestyle='--', alpha=0.7, linewidth=1.5)
ax1.text(12.4, 0.9, "✓ Done (12.2s)", fontsize=8, color='green')

# PlanSys2 timeline
ax2 = axes[1]
ax2.set_ylabel("PlanSys2\n(monolithic)", fontsize=9, fontweight='bold')
draw_timeline(ax2, plansys2_phases, 0.5, 0.7)
ax2.set_ylim(0, 1)
ax2.set_yticks([])
ax2.axvline(x=20.0, color='red', linestyle='--', alpha=0.7, linewidth=1.5)
ax2.text(14.0, 0.9, "✗ OOM after 20s", fontsize=8, color='red')

ax2.set_xlabel("Time (seconds)", fontsize=9)
ax2.set_xlim(0, 22)

# Legend
legend_patches = [
    mpatches.Patch(color=colors['hl_plan'], label='HL Planning (ENHSP)'),
    mpatches.Patch(color=colors['ml_plan'], label='ML Planning (TFD)'),
    mpatches.Patch(color=colors['execute'], label='Action Execution'),
    mpatches.Patch(color=colors['failed'], label='Failed (OOM)'),
]
fig.legend(handles=legend_patches, loc='lower center', ncol=4, fontsize=8,
           bbox_to_anchor=(0.5, -0.02))

plt.tight_layout()
plt.savefig("APTree_vs_PlanSys2_timeline_P4.png", dpi=150, bbox_inches='tight')
plt.savefig("APTree_vs_PlanSys2_timeline_P4.pdf", bbox_inches='tight')
print("Saved: APTree_vs_PlanSys2_timeline_P4.png/pdf")


# ========== FIGURE 2: P2 Comparison (subset that both solve) ==========
fig2, axes2 = plt.subplots(2, 1, figsize=(10, 4), sharex=True)
fig2.suptitle("APTree vs PlanSys2: Execution Timeline\n(Problem P2: Pick+Place+Glue lp1 — both systems succeed)",
              fontsize=11, fontweight='bold')

ax3 = axes2[0]
ax3.set_ylabel("APTree\n(hierarchical)", fontsize=9, fontweight='bold')
draw_timeline(ax3, aptree_p2_phases, 0.5, 0.7)
ax3.set_ylim(0, 1)
ax3.set_yticks([])
ax3.axvline(x=4.41, color='green', linestyle='--', alpha=0.7, linewidth=1.5)
ax3.text(4.5, 0.9, "✓ Done (4.4s)", fontsize=8, color='green')

ax4 = axes2[1]
ax4.set_ylabel("PlanSys2\n(monolithic)", fontsize=9, fontweight='bold')
draw_timeline(ax4, plansys2_p2_phases, 0.5, 0.7)
ax4.set_ylim(0, 1)
ax4.set_yticks([])
ax4.axvline(x=4.1, color='green', linestyle='--', alpha=0.7, linewidth=1.5)
ax4.text(4.2, 0.9, "✓ Done (4.1s)", fontsize=8, color='green')

ax4.set_xlabel("Time (seconds)", fontsize=9)
ax4.set_xlim(0, 6)

fig2.legend(handles=legend_patches[:3], loc='lower center', ncol=3, fontsize=8,
            bbox_to_anchor=(0.5, -0.02))

plt.tight_layout()
plt.savefig("APTree_vs_PlanSys2_timeline_P2.png", dpi=150, bbox_inches='tight')
plt.savefig("APTree_vs_PlanSys2_timeline_P2.pdf", bbox_inches='tight')
print("Saved: APTree_vs_PlanSys2_timeline_P2.png/pdf")


# ========== FIGURE 3: Scalability bar chart ==========
fig3, ax5 = plt.subplots(figsize=(10, 5))
fig3.suptitle("Planning Time Scalability: APTree vs PlanSys2",
              fontsize=11, fontweight='bold')

problems = ['P0\nPickUp lp1', 'P1\nPick+Place', 'P2\n+Glue', 'P3\n+Stack b1', 'P4\n+Stack b2']
aptree_hl = [0.710, 0.708, 0.701, 0.743, 0.840]
aptree_ml = [0.735, 1.435, 2.262, 3.562, 5.281]
aptree_total = [t+m for t, m in zip(aptree_hl, aptree_ml)]

x = np.arange(len(problems))
width = 0.35

bars_hl = ax5.bar(x - width/2, aptree_hl, width/2, label='APTree HL (ENHSP)', color=colors['hl_plan'])
bars_ml = ax5.bar(x - width/2, aptree_ml, width/2, bottom=aptree_hl, label='APTree ML (TFD, sum)', color=colors['ml_plan'])

# PlanSys2 bars - actual data from runs
# P0: TBD, P1: 0.849s, P2: 5.706s, P3: OOM, P4: OOM
plansys2_bar_data = [0, 0.849, 5.706, 12.0, 12.0]  # P3/P4 = OOM (shown as bar hitting limit)
plansys2_colors = ['#9E9E9E', '#4CAF50', '#4CAF50', '#F44336', '#F44336']

for i, (val, col) in enumerate(zip(plansys2_bar_data, plansys2_colors)):
    ax5.bar(x[i] + width/2, val, width, color=col, alpha=0.7,
            label='PlanSys2 (TFD monolithic)' if i == 1 else '')

# Mark OOM
ax5.text(3 + width/2, 12.3, "OOM", ha='center', fontsize=9, color='red', fontweight='bold')
ax5.text(4 + width/2, 12.3, "OOM", ha='center', fontsize=9, color='red', fontweight='bold')
ax5.text(0 + width/2, 0.3, "TBD", ha='center', fontsize=7, color='grey')

ax5.set_ylabel('Planning Time (seconds)', fontsize=10)
ax5.set_xlabel('Problem Complexity', fontsize=10)
ax5.set_xticks(x)
ax5.set_xticklabels(problems, fontsize=9)
ax5.legend(fontsize=9)
ax5.set_ylim(0, 14)
ax5.grid(axis='y', alpha=0.3)

# Add value labels on APTree bars
for i, (hl, ml) in enumerate(zip(aptree_hl, aptree_ml)):
    total = hl + ml
    ax5.text(i - width/2, total + 0.2, f'{total:.1f}s', ha='center', fontsize=7)

# Add PlanSys2 value labels
ax5.text(1 + width/2, 0.849 + 0.2, '0.85s', ha='center', fontsize=7, color='green')
ax5.text(2 + width/2, 5.706 + 0.2, '5.7s', ha='center', fontsize=7, color='green')

plt.tight_layout()
plt.savefig("APTree_vs_PlanSys2_scalability.png", dpi=150, bbox_inches='tight')
plt.savefig("APTree_vs_PlanSys2_scalability.pdf", bbox_inches='tight')
print("Saved: APTree_vs_PlanSys2_scalability.png/pdf")

plt.show()

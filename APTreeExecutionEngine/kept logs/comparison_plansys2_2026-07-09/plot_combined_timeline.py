"""
APTree vs PlanSys2: Combined Timeline Comparison (stacked)
Top: APTree (5 runs), Bottom: PlanSys2 (5 runs), same x-axis scale
"""
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
import numpy as np

# === Color scheme ===
colors = {
    'hl_plan': '#9E9E9E',      # Grey
    'ml_plan': '#FF9800',      # Orange  
    'execute': '#2196F3',      # Blue
    'bt_gen': '#4CAF50',       # Green (BT generation)
    'failed': '#F44336',       # Red
}

# === APTree phases (BT generation ~27ms noted as text annotations) ===

p0_phases = [
    (0.0, 0.710, "1", "hl_plan"),
    (0.710, 0.735, "5", "ml_plan"),
    (1.445, 0.50, "5", "execute"),
]

p1_phases = [
    (0.0, 0.708, "2", "hl_plan"),
    (0.708, 0.745, "5", "ml_plan"),
    (1.453, 0.50, "5", "execute"),
    (1.953, 0.690, "2", "ml_plan"),
    (2.643, 0.20, "2", "execute"),
]

p2_phases = [
    (0.0, 0.701, "3", "hl_plan"),
    (0.701, 0.765, "5", "ml_plan"),
    (1.466, 0.50, "5", "execute"),
    (1.966, 0.746, "2", "ml_plan"),
    (2.712, 0.20, "2", "execute"),
    (2.912, 0.751, "8", "ml_plan"),
    (3.663, 0.80, "8", "execute"),
]

p3_phases = [
    (0.0, 0.743, "5", "hl_plan"),
    (0.743, 0.698, "5", "ml_plan"),
    (1.441, 0.50, "5", "execute"),
    (1.941, 0.699, "2", "ml_plan"),
    (2.640, 0.20, "2", "execute"),
    (2.840, 0.696, "8", "ml_plan"),
    (3.536, 0.80, "8", "execute"),
    (4.336, 0.708, "9", "ml_plan"),
    (5.044, 0.20, "9", "execute"),
    (5.244, 0.761, "2", "ml_plan"),
    (6.005, 0.20, "2", "execute"),
]

p4_phases = [
    (0.0, 0.840, "7", "hl_plan"),
    (0.840, 0.754, "5", "ml_plan"),
    (1.594, 0.50, "5", "execute"),
    (2.094, 0.753, "2", "ml_plan"),
    (2.847, 0.20, "2", "execute"),
    (3.047, 0.740, "8", "ml_plan"),
    (3.787, 0.80, "8", "execute"),
    (4.587, 0.736, "9", "ml_plan"),
    (5.323, 0.90, "9", "execute"),
    (6.223, 0.717, "2", "ml_plan"),
    (6.940, 0.20, "2", "execute"),
    (7.140, 0.729, "2", "ml_plan"),
    (7.869, 0.20, "2", "execute"),
    (8.069, 0.851, "2", "ml_plan"),
    (8.920, 0.20, "2", "execute"),
]

# BT generation times per transition (ms) for annotation
bt_gen_times = {
    'PickUp lp1': 29, 'Place lp1': 22, 'Glue lp1': 24,
    'PickUp b2': 33, 'Stack b2': 29, 'PickUp b1': 27, 'Stack b1': 27
}

aptree_runs = [
    ("P0", p0_phases, 4.1),
    ("P1", p1_phases, 6.0),
    ("P2", p2_phases, 9.8),
    ("P3", p3_phases, 16.6),
    ("P4", p4_phases, 10.2),
]

# === PlanSys2 phases ===
ps2_p0_phases = [
    (0.0, 0.168, "5", "ml_plan"),
    (0.168, 0.60, "5", "execute"),
]

ps2_p1_phases = [
    (0.0, 0.85, "7", "ml_plan"),
    (0.85, 1.17, "7", "execute"),
]

ps2_p2_phases = [
    (0.0, 5.71, "15", "ml_plan"),
    (5.71, 1.60, "15", "execute"),
]

ps2_p3_phases = [
    (0.0, 10.5, "OOM", "failed"),
]

ps2_p4_phases = [
    (0.0, 10.5, "OOM", "failed"),
]

# PlanSys2 BT generation times (ms)
ps2_bt_gen = {'P1': 161, 'P2': 131}

plansys2_runs = [
    ("P0", ps2_p0_phases, 0.95),
    ("P1", ps2_p1_phases, 2.0),
    ("P2", ps2_p2_phases, 7.6),
    ("P3", ps2_p3_phases, None),
    ("P4", ps2_p4_phases, None),
]


def draw_timeline(ax, phases, y_center, height, show_labels=True, min_label_width=0.5, show_bt_gen=False, bt_gen_labels=None):
    """Draw phases as colored blocks on a timeline.
    bt_gen_labels: list of labels for each plan→exec transition (in order)"""
    bt_gen_idx = 0
    for idx, (start, duration, label, phase_type) in enumerate(phases):
        color = colors[phase_type]
        rect = mpatches.FancyBboxPatch(
            (start, y_center - height/2), duration, height,
            boxstyle="round,pad=0.01",
            facecolor=color, edgecolor='none', linewidth=0, alpha=0.6
        )
        ax.add_patch(rect)
        if show_labels and label:
            fontsize = 8
            text_color = 'black'
            ax.text(start + duration/2, y_center, label,
                   ha='center', va='center', fontsize=fontsize, fontweight='bold',
                   color=text_color)
        
        # Add BT gen time annotation at plan→exec transitions
        if show_bt_gen and bt_gen_labels and phase_type == 'ml_plan' and idx + 1 < len(phases):
            next_phase = phases[idx + 1]
            if next_phase[3] == 'execute' and bt_gen_idx < len(bt_gen_labels) and bt_gen_labels[bt_gen_idx]:
                transition_x = start + duration
                ax.text(transition_x, y_center + height/2 + 0.05, bt_gen_labels[bt_gen_idx],
                       ha='center', va='bottom', fontsize=8, color='black',
                       fontweight='bold')
                bt_gen_idx += 1


# ========== COMBINED FIGURE ==========
fig, (ax_top, ax_bot) = plt.subplots(2, 1, figsize=(14, 5), sharex=True)

x_max = 11
n_runs = 5
row_height = 0.6
row_spacing = 1.2

# --- TOP: APTree ---
ax_top.set_title("APTree", fontsize=10, fontweight='bold', loc='left')

# BT gen labels per row — each is a LIST of per-transition values from logs
aptree_bt_labels_per_row = [
    ["29ms", "22ms", "24ms", "33ms", "29ms", "27ms", "24ms"],  # P4: 7 transitions
    ["31ms", "23ms", "28ms", "29ms", "27ms"],                   # P3: 5 transitions
    ["31ms", "20ms", "24ms"],                                     # P2: 3 transitions
    ["30ms", "27ms"],                                             # P1: 2 transitions
    ["21ms"],                                                     # P0: 1 transition
]

for i, (label, phases, total_time) in enumerate(reversed(aptree_runs)):
    y = i * row_spacing + 0.5
    draw_timeline(ax_top, phases, y, row_height, min_label_width=0.55, show_bt_gen=True, bt_gen_labels=aptree_bt_labels_per_row[i])
    
    end_x = phases[-1][0] + phases[-1][1]
    ax_top.axvline(x=end_x, ymin=(y - row_height/2 - 0.1) / (n_runs * row_spacing),
               ymax=(y + row_height/2 + 0.1) / (n_runs * row_spacing),
               color='green', linestyle='--', alpha=0.6, linewidth=1)
    ax_top.text(end_x + 0.1, y, f"\u2713 {end_x:.1f}s",
            fontsize=9, color='black', va='center')

y_positions = [i * row_spacing + 0.5 for i in range(n_runs)]
y_labels = [label for label, _, _ in reversed(aptree_runs)]
ax_top.set_yticks(y_positions)
ax_top.set_yticklabels(y_labels, fontsize=9, fontweight='bold')
ax_top.set_ylim(-0.3, n_runs * row_spacing + 0.4)
ax_top.grid(axis='x', alpha=0.2)

# --- BOTTOM: PlanSys2 ---
ax_bot.set_title("PlanSys2", fontsize=10, fontweight='bold', loc='left')

ps2_bt_labels_per_row = [
    [],          # P4: OOM, no BT gen
    [],          # P3: OOM, no BT gen
    ["131ms"],   # P2: 1 transition
    ["161ms"],   # P1: 1 transition
    ["98ms"],    # P0: 1 transition
]

for i, (label, phases, total_time) in enumerate(reversed(plansys2_runs)):
    y = i * row_spacing + 0.5
    draw_timeline(ax_bot, phases, y, row_height, min_label_width=0.55, show_bt_gen=True, bt_gen_labels=ps2_bt_labels_per_row[i])
    
    end_x = phases[-1][0] + phases[-1][1]
    if total_time is not None:
        ax_bot.axvline(x=end_x, ymin=(y - row_height/2 - 0.1) / (n_runs * row_spacing),
                   ymax=(y + row_height/2 + 0.1) / (n_runs * row_spacing),
                   color='green', linestyle='--', alpha=0.6, linewidth=1)
        ax_bot.text(end_x + 0.1, y, f"\u2713 {total_time:.1f}s",
                fontsize=9, color='black', va='center')
    else:
        ax_bot.text(x_max - 0.1, y, "\u2717 OOM",
                fontsize=9, color='black', va='center', ha='right', fontweight='bold')

y_positions = [i * row_spacing + 0.5 for i in range(n_runs)]
y_labels = [label for label, _, _ in reversed(plansys2_runs)]
ax_bot.set_yticks(y_positions)
ax_bot.set_yticklabels(y_labels, fontsize=9, fontweight='bold')
ax_bot.set_ylim(-0.3, n_runs * row_spacing + 0.4)
ax_bot.set_xlim(-0.5, x_max)
ax_bot.set_xlabel("Time (seconds)", fontsize=10, labelpad=10)
ax_bot.grid(axis='x', alpha=0.2)

# Legend (shared)
legend_patches = [
    mpatches.Patch(color=colors['hl_plan'], label='HL Planning (ENHSP)'),
    mpatches.Patch(color=colors['ml_plan'], label='ML/TFD Planning'),
    mpatches.Patch(color=colors['execute'], label='Action Execution'),
    mpatches.Patch(color=colors['failed'], label='Failed (Out of Memory)'),
]
fig.legend(handles=legend_patches, loc='lower center', ncol=4, fontsize=9,
           bbox_to_anchor=(0.5, -0.04))

plt.tight_layout()
plt.savefig("APTree_vs_PlanSys2_combined_timeline.png", dpi=150, bbox_inches='tight')
plt.savefig("APTree_vs_PlanSys2_combined_timeline.pdf", bbox_inches='tight')
print("Saved: APTree_vs_PlanSys2_combined_timeline.png/pdf")
plt.close()

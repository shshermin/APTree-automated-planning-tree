"""
M1 Metric Calculator — Variable Reuse Across BT Action Nodes
============================================================
M1 = (# variables appearing in 2+ action instances across HL/ML/LL) /
     (# total variables defined in DemonstratorSceneObjects.bt)  * 100

Variables are extracted by tokenising each action InstanceName on '_' and
keeping only tokens that are known scene-object instance names.
"""

import csv
import re
import glob
import os
from collections import defaultdict

# ── Paths ──────────────────────────────────────────────────────────────────────
LOG_DIR = os.path.join(
    os.path.dirname(__file__),
    r"kept logs\demonstrator_2026-05-29_16-58-37",
)
SCENE_OBJECTS_BT = os.path.join(
    os.path.dirname(__file__),
    r"..\APTreeDSL\src\test\resources\valid\CRFConcrete\DemonstratorSceneObjects.bt",
)
TOTAL_VARIABLES_IN_DSL = 441  # total declarations in DemonstratorSceneObjects.bt

# Known type names in CRFTypesCon grammar (used to distinguish type keywords from
# instance names when parsing SceneObjects.bt)
KNOWN_TYPES = {
    "Robot", "RobotPosition", "Gripper", "StaplerGun",
    "EquipLocation", "InitialLocation", "FinalLocation", "NailLocation",
    "Stack", "Table", "Demo", "Stick", "Cube",
}


def load_known_variable_names(scene_bt_path: str) -> set[str]:
    """Parse DemonstratorSceneObjects.bt and return all instance names."""
    names: set[str] = set()
    # Lines look like:  TypeName instanceName ( ... )
    pattern = re.compile(r'^\s*(\w+)\s+(\w+)\s*\(')
    with open(scene_bt_path, encoding="utf-8") as fh:
        for line in fh:
            m = pattern.match(line)
            if m and m.group(1) in KNOWN_TYPES:
                names.add(m.group(2))
    return names


def tokenise(instance_name: str) -> list[str]:
    """Split on '_', drop empty tokens and trailing dup-markers (dup2, dup3…)."""
    return [
        t for t in instance_name.split("_")
        if t and not re.fullmatch(r"dup\d+", t)
    ]


def collect_variable_appearances(
    csv_path: str,
    instance_col: str,
    level_tag: str,
    known_vars: set[str],
    var_to_instances: dict,
) -> int:
    """Read one CSV and accumulate variable → action-instance mappings.
    Returns the number of rows processed."""
    rows = 0
    with open(csv_path, newline="", encoding="utf-8") as fh:
        reader = csv.DictReader(fh)
        for row in reader:
            instance = row[instance_col].strip()
            tokens = tokenise(instance)
            seen_in_this_row: set[str] = set()
            for token in tokens:
                if token in known_vars and token not in seen_in_this_row:
                    var_to_instances[token].add((level_tag, instance))
                    seen_in_this_row.add(token)
            rows += 1
    return rows


def main():
    print("=" * 60)
    print("  M1 Metric — Variable Reuse Across BT Action Nodes")
    print("=" * 60)

    # 1. Load known variable names from DSL model
    known_vars = load_known_variable_names(SCENE_OBJECTS_BT)
    print(f"\nKnown variables loaded from SceneObjects.bt : {len(known_vars)}")
    if len(known_vars) != TOTAL_VARIABLES_IN_DSL:
        print(f"  WARNING: expected {TOTAL_VARIABLES_IN_DSL}, got {len(known_vars)}")

    # 2. Collect appearances across all three trace levels
    # variable → set of (level, instance_name) pairs
    var_to_instances: dict[str, set[tuple[str, str]]] = defaultdict(set)

    total_rows = {"HL": 0, "ML": 0, "LL": 0}

    # ── HL ────────────────────────────────────────────────────────────────────
    for path in glob.glob(os.path.join(LOG_DIR, "HierarchicalTrace_HL_*.csv")):
        total_rows["HL"] += collect_variable_appearances(
            path, "ActionName", "HL", known_vars, var_to_instances
        )

    # ── ML ────────────────────────────────────────────────────────────────────
    for path in glob.glob(os.path.join(LOG_DIR, "HierarchicalTrace_ML_*.csv")):
        total_rows["ML"] += collect_variable_appearances(
            path, "InstanceName", "ML", known_vars, var_to_instances
        )

    # ── LL ────────────────────────────────────────────────────────────────────
    for path in glob.glob(os.path.join(LOG_DIR, "HierarchicalTrace_LL_*.csv")):
        total_rows["LL"] += collect_variable_appearances(
            path, "InstanceName", "LL", known_vars, var_to_instances
        )

    print(f"Trace rows processed  — HL: {total_rows['HL']}, "
          f"ML: {total_rows['ML']}, LL: {total_rows['LL']}")
    print(f"Total action instances: {sum(total_rows.values())}")

    # 3. Compute M1
    appeared    = {v: s for v, s in var_to_instances.items()}
    reused      = {v: s for v, s in var_to_instances.items() if len(s) >= 2}
    never_used  = TOTAL_VARIABLES_IN_DSL - len(appeared)
    single_use  = len(appeared) - len(reused)

    m1 = len(reused) / TOTAL_VARIABLES_IN_DSL * 100

    print(f"\n--- Results ---")
    print(f"Total variables defined in DSL model    : {TOTAL_VARIABLES_IN_DSL}")
    print(f"Variables appearing in any action node  : {len(appeared)}")
    print(f"  - reused in 2+ action instances       : {len(reused)}")
    print(f"  - used in exactly 1 action instance   : {single_use}")
    print(f"Variables never referenced in any trace : {never_used}")
    print(f"\nM1 = {len(reused)} / {TOTAL_VARIABLES_IN_DSL} × 100 = {m1:.2f}%")

    # 4. Breakdown by level
    print(f"\n--- Reuse breakdown by level ---")
    for level in ("HL", "ML", "LL"):
        count = sum(
            1 for s in reused.values()
            if any(tag == level for tag, _ in s)
        )
        print(f"  Variables appearing in 2+ {level} instances : {count}")

    # 5. Top 15 most-reused variables
    print(f"\n--- Top 15 most-reused variables (by # action instances) ---")
    top = sorted(reused.items(), key=lambda x: len(x[1]), reverse=True)[:15]
    for var, instances in top:
        hl_c = sum(1 for t, _ in instances if t == "HL")
        ml_c = sum(1 for t, _ in instances if t == "ML")
        ll_c = sum(1 for t, _ in instances if t == "LL")
        print(f"  {var:<22} total={len(instances):>4}  "
              f"(HL={hl_c}, ML={ml_c}, LL={ll_c})")

    # 6. Variables that never appear in any trace
    print(f"\n--- Variables in DSL but never used in any trace ---")
    unused = sorted(known_vars - set(appeared.keys()))
    print(f"  Count: {len(unused)}")
    if unused:
        preview = ", ".join(unused[:20])
        if len(unused) > 20:
            preview += f" … (+{len(unused)-20} more)"
        print(f"  {preview}")


if __name__ == "__main__":
    main()

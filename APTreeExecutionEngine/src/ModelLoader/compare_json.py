import json
import sys
import os


def load_json(path):
    with open(path, "r") as f:
        return json.load(f)


def build_index(data):
    """Index instances by lowercase name for case-insensitive matching."""
    return {inst["name"].lower(): inst for inst in data.get("instances", [])}


def parse_numbers(val):
    """Try to parse a comma-separated string of numbers."""
    if not isinstance(val, str):
        return None
    parts = val.replace(" ", "").split(",")
    try:
        return [float(p) for p in parts if p]
    except ValueError:
        return None


def compare_instances(name, old, new, tol):
    diffs = []
    old_props = old.get("properties", {})
    new_props = new.get("properties", {})

    # Normalize property keys to lowercase
    old_props_lower = {k.lower(): v for k, v in old_props.items()}
    new_props_lower = {k.lower(): v for k, v in new_props.items()}

    all_keys = set(old_props_lower.keys()) | set(new_props_lower.keys())
    for key in sorted(all_keys):
        if key not in old_props_lower:
            diffs.append(f"property '{key}' only in NEW: {new_props_lower[key]}")
            continue
        if key not in new_props_lower:
            diffs.append(f"property '{key}' only in OLD: {old_props_lower[key]}")
            continue
        v_old = old_props_lower[key]
        v_new = new_props_lower[key]
        if v_old == v_new:
            continue

        # Try numeric comparison
        nums_old = parse_numbers(v_old)
        nums_new = parse_numbers(v_new)
        if nums_old is not None and nums_new is not None and len(nums_old) == len(nums_new):
            for i, (a, b) in enumerate(zip(nums_old, nums_new)):
                delta = abs(a - b)
                if delta > tol:
                    labels = ["x", "y", "z", "ox", "oy", "oz"]
                    label = labels[i] if i < len(labels) else str(i)
                    diffs.append(
                        f"{key}[{label}]: old={a:.10f}  new={b:.10f}  "
                        f"delta={delta:.10f} ({delta * 1000:.4f} mm)"
                    )
        else:
            # String comparison (case-insensitive for references)
            if str(v_old).lower() != str(v_new).lower():
                diffs.append(f"{key}: old={v_old}  new={v_new}")
    return diffs


def compare(old_path, new_path, tolerance=0.003):
    old = load_json(old_path)
    new = load_json(new_path)

    old_idx = build_index(old)
    new_idx = build_index(new)

    old_names = set(old_idx.keys())
    new_names = set(new_idx.keys())

    # Objects only in old (backup) JSON
    only_old = sorted(old_names - new_names)
    # Objects only in new (generated) JSON
    only_new = sorted(new_names - old_names)

    print("=" * 70)
    print(f"OLD (backup): {os.path.basename(old_path)}  ({len(old_names)} instances)")
    print(f"NEW (generated): {os.path.basename(new_path)}  ({len(new_names)} instances)")
    print("=" * 70)

    if only_old:
        print(f"\n--- Objects in OLD but NOT in NEW ({len(only_old)}) ---")
        for name in only_old:
            obj = old_idx[name]
            print(f"  {obj['name']:30s}  type={obj.get('type', '?'):20s}  extends={obj.get('extends', '?')}")
    else:
        print("\nNo objects missing from the new JSON.")

    if only_new:
        print(f"\n--- Objects in NEW but NOT in OLD ({len(only_new)}) ---")
        for name in only_new:
            obj = new_idx[name]
            print(f"  {obj['name']:30s}  type={obj.get('type', '?'):20s}  extends={obj.get('extends', '?')}")
    else:
        print("\nNo extra objects in the new JSON.")

    # Compare shared objects
    shared = sorted(old_names & new_names)
    diffs = []
    for name in shared:
        obj_old = old_idx[name]
        obj_new = new_idx[name]
        obj_diffs = compare_instances(name, obj_old, obj_new, tolerance)
        if obj_diffs:
            diffs.append((old_idx[name]["name"], obj_diffs))

    if diffs:
        print(f"\n--- Numerical differences > {tolerance * 1000:.1f} mm ({len(diffs)} objects) ---")
        for name, d in diffs:
            print(f"\n  [{name}]")
            for line in d:
                print(f"    {line}")
    else:
        print(f"\nNo numerical differences > {tolerance * 1000:.1f} mm in shared objects.")

    # Summary
    print("\n" + "=" * 70)
    print(f"Summary: {len(only_old)} only-in-old, {len(only_new)} only-in-new, "
          f"{len(diffs)} with value differences > {tolerance * 1000:.1f} mm")
    print("=" * 70)


if __name__ == "__main__":
    script_dir = os.path.dirname(os.path.abspath(__file__))
    default_old = os.path.join(script_dir, "DemonstratorSetupObjects_backup.json")
    default_new = os.path.join(script_dir, "DemonstratorSetupObjects_new.json")

    old_path = sys.argv[1] if len(sys.argv) > 1 else default_old
    new_path = sys.argv[2] if len(sys.argv) > 2 else default_new

    compare(old_path, new_path)

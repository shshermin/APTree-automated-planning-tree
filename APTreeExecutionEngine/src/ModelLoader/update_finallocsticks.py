import json
import re
import os

script_dir = os.path.dirname(os.path.abspath(__file__))
json_path = os.path.join(script_dir, "DemonstratorSetupObjects.json")
temp_path = os.path.join(script_dir, "temp.txt")

with open(temp_path, "r") as f:
    new_data_raw = f.read()

if not new_data_raw.strip():
    print("ERROR: temp.txt is empty!")
    exit(1)

# Validate that every line matches the expected format
for line in new_data_raw.strip().split("\n"):
    if not re.match(r'FinalLocation\s+FinalLocStick\d+\s+\([^)]+\)', line.strip()):
        print(f"ERROR: Line does not match expected format: {line}")
        exit(1)

print(f"All lines in temp.txt validated OK")

# Parse the new data
new_entries = {}
for line in new_data_raw.strip().split("\n"):
    m = re.match(r'FinalLocation\s+FinalLocStick(\d+)\s+\(([^)]+)\)', line.strip())
    if m:
        stick_num = int(m.group(1))
        parts = m.group(2).strip()
        # Split by double space to separate position and orientation
        pos_ori = re.split(r'\s{2,}', parts)
        position = pos_ori[0].strip()
        orientation = pos_ori[1].strip()
        new_entries[stick_num] = {"position": position, "orientation": orientation}

print(f"Parsed {len(new_entries)} new entries")

# Read the JSON file
with open(json_path, "r", encoding="utf-8-sig") as f:
    data = json.load(f)

# Find and update finallocstick entries, tracking changes
changes = []
for item in data["instances"]:
    if item.get("type") == "FinalLocation" and item["name"].startswith("finallocstick"):
        num_match = re.match(r'finallocstick(\d+)', item["name"])
        if num_match:
            stick_num = int(num_match.group(1))
            if stick_num in new_entries:
                old_pos = item["properties"]["position"]
                old_ori = item["properties"]["orientation"]
                new_pos = new_entries[stick_num]["position"]
                new_ori = new_entries[stick_num]["orientation"]
                
                pos_changed = old_pos != new_pos
                ori_changed = old_ori != new_ori
                
                if pos_changed or ori_changed:
                    changes.append({
                        "stick": stick_num,
                        "old_pos": old_pos,
                        "new_pos": new_pos,
                        "old_ori": old_ori,
                        "new_ori": new_ori,
                        "pos_changed": pos_changed,
                        "ori_changed": ori_changed
                    })
                
                item["properties"]["position"] = new_pos
                item["properties"]["orientation"] = new_ori

# Print difference summary
print(f"\n{'='*80}")
print(f"DIFFERENCES SUMMARY: {len(changes)} sticks changed out of {len(new_entries)} provided")
print(f"{'='*80}\n")

for c in changes:
    print(f"Stick {c['stick']}:")
    if c['pos_changed']:
        # Parse old and new positions
        old_xyz = c['old_pos'].split(',')
        new_xyz = c['new_pos'].split(',')
        diffs = []
        for i, axis in enumerate(['X', 'Y', 'Z']):
            old_v = float(old_xyz[i])
            new_v = float(new_xyz[i])
            if abs(old_v - new_v) > 1e-15:
                diffs.append(f"  {axis}: {old_v} -> {new_v} (delta: {new_v - old_v:+.6e})")
        if diffs:
            print("  Position:")
            for d in diffs:
                print(d)
    if c['ori_changed']:
        old_xyz = c['old_ori'].split(',')
        new_xyz = c['new_ori'].split(',')
        diffs = []
        for i, axis in enumerate(['OX', 'OY', 'OZ']):
            old_v = float(old_xyz[i])
            new_v = float(new_xyz[i])
            if abs(old_v - new_v) > 1e-15:
                diffs.append(f"  {axis}: {old_v} -> {new_v} (delta: {new_v - old_v:+.6e})")
        if diffs:
            print("  Orientation:")
            for d in diffs:
                print(d)
    print()

# Write updated JSON back
with open(json_path, "w", encoding="utf-8") as f:
    json.dump(data, f, indent=2)
    f.write("\n")

print(f"Updated {json_path} successfully.")

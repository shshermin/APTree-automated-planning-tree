import re
import os

script_dir = os.path.dirname(os.path.abspath(__file__))
cs_path = os.path.join(script_dir, "NailCoordinates.cs")
temp_path = os.path.join(script_dir, "temp.txt")

with open(temp_path, "r") as f:
    new_data_raw = f.read()

if not new_data_raw.strip():
    print("ERROR: temp.txt is empty!")
    exit(1)

# Validate that every line matches the expected format
for line in new_data_raw.strip().split("\n"):
    if not re.match(r'nailed\(\w+,\s*\w+,\s*[-\d.]+,\s*[-\d.]+,\s*[-\d.]+\)', line.strip()):
        print(f"ERROR: Line does not match expected format: {line}")
        exit(1)

print(f"All lines in temp.txt validated OK")

# Parse new entries
new_entries = {}
for line in new_data_raw.strip().split("\n"):
    m = re.match(r'nailed\((\w+),\s*(\w+),\s*([-\d.]+),\s*([-\d.]+),\s*([-\d.]+)\)', line.strip())
    if m:
        obj1, obj2 = m.group(1).lower(), m.group(2).lower()
        x, y, z = m.group(3), m.group(4), m.group(5)
        new_entries[(obj1, obj2)] = (x, y, z)

print(f"Parsed {len(new_entries)} new nail entries")

# Read current CS file
with open(cs_path, "r", encoding="utf-8-sig") as f:
    content = f.read()

# Parse existing entries from the CS file
old_entries = {}
old_order = []
pattern = re.compile(r'\{\s*\("(\w+)",\s*"(\w+)"\),\s*new\s+Coordinate\(([-\d.]+),\s*([-\d.]+),\s*([-\d.]+)\)\s*\}')
for m in pattern.finditer(content):
    obj1, obj2 = m.group(1), m.group(2)
    x, y, z = m.group(3), m.group(4), m.group(5)
    key = (obj1, obj2)
    old_entries[key] = (x, y, z)
    old_order.append(key)

print(f"Found {len(old_entries)} existing nail entries")

# Find differences
changes = []
added = []
removed_from_new = []

for key in new_entries:
    if key in old_entries:
        old_x, old_y, old_z = old_entries[key]
        new_x, new_y, new_z = new_entries[key]
        if old_x != new_x or old_y != new_y or old_z != new_z:
            changes.append((key, old_entries[key], new_entries[key]))
    else:
        added.append(key)

for key in old_entries:
    if key not in new_entries:
        removed_from_new.append(key)

print(f"\n{'='*80}")
print(f"DIFFERENCES: {len(changes)} updated, {len(added)} new, {len(removed_from_new)} only in old")
print(f"{'='*80}\n")

if changes:
    print("UPDATED entries:")
    for key, old, new in changes:
        diffs = []
        labels = ['X', 'Y', 'Z']
        for i in range(3):
            if old[i] != new[i]:
                diffs.append(f"{labels[i]}: {old[i]} -> {new[i]}")
        print(f"  ({key[0]}, {key[1]}): {', '.join(diffs)}")
    print()

if added:
    print("NEW entries (not in old file):")
    for key in added:
        vals = new_entries[key]
        print(f"  ({key[0]}, {key[1]}): ({vals[0]}, {vals[1]}, {vals[2]})")
    print()

if removed_from_new:
    print("KEPT from old (not in new data, will be preserved):")
    for key in removed_from_new:
        vals = old_entries[key]
        print(f"  ({key[0]}, {key[1]}): ({vals[0]}, {vals[1]}, {vals[2]})")
    print()

# Now build the new content
# We'll follow the order of the new data, then append any old-only entries at the end
new_content = content

# Replace each existing entry inline
for key in new_entries:
    if key in old_entries:
        old_x, old_y, old_z = old_entries[key]
        new_x, new_y, new_z = new_entries[key]
        old_str = f'{{ ("{key[0]}", "{key[1]}"), new Coordinate({old_x}, {old_y}, {old_z}) }}'
        new_str = f'{{ ("{key[0]}", "{key[1]}"), new Coordinate({new_x}, {new_y}, {new_z}) }}'
        new_content = new_content.replace(old_str, new_str)

# For completely new entries, we need to insert them
# Find where to insert - before the entries that aren't in new data
# We'll insert new entries right before the closing of the dictionary
for key in added:
    new_x, new_y, new_z = new_entries[key]
    insert_line = f'                {{ ("{key[0]}", "{key[1]}"), new Coordinate({new_x}, {new_y}, {new_z}) }},'
    # Insert before the closing };
    new_content = new_content.replace(
        "            };\n",
        f"{insert_line}\n            }};\n",
        1
    )

with open(cs_path, "w", encoding="utf-8") as f:
    f.write(new_content)

print("Updated NailCoordinates.cs successfully.")

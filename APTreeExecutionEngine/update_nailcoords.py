import re

cs_path = r"src\ModelLoader\NailCoordinates.cs"

new_data_raw = """nailed(stick6, stick1, 0.312, 0.950, 0.028)
nailed(cube1, stick1, 0.188, 1.005, 0.028)
nailed(stick6, stick2, 0.356, 0.823, 0.028)
nailed(stick7, stick2, 0.301, 0.700, 0.028)
nailed(stick7, stick3, 0.174, 0.656, 0.028)
nailed(stick8, stick3, 0.013, 0.728, 0.028)
nailed(stick8, stick4, 0.031, 0.800, 0.028)
nailed(stick9, stick4, -0.027, 0.929, 0.028)
nailed(stick9, stick5, -0.154, 0.973, 0.028)
nailed(cube2, stick5, -0.221, 0.943, 0.028)
nailed(stick10, stick6, 0.314, 0.943, 0.046)
nailed(stick11, stick6, 0.358, 0.816, 0.046)
nailed(stick11, stick7, 0.295, 0.698, 0.046)
nailed(stick12, stick7, 0.168, 0.654, 0.046)
nailed(stick12, stick8, 0.015, 0.735, 0.046)
nailed(stick13, stick8, 0.033, 0.808, 0.046)
nailed(stick13, stick9, -0.033, 0.931, 0.046)
nailed(stick14, stick9, -0.160, 0.975, 0.046)
nailed(stick15, stick10, 0.320, 0.940, 0.064)
nailed(cube1, stick10, 0.199, 1.002, 0.046)
nailed(cube3, stick10, 0.207, 1.000, 0.064)
nailed(stick15, stick11, 0.355, 0.811, 0.064)
nailed(stick16, stick11, 0.292, 0.692, 0.064)
nailed(stick16, stick12, 0.162, 0.657, 0.064)
nailed(stick17, stick12, 0.006, 0.740, 0.064)
nailed(stick17, stick13, 0.027, 0.819, 0.064)
nailed(stick18, stick13, -0.036, 0.937, 0.064)
nailed(stick18, stick14, -0.166, 0.972, 0.064)
nailed(cube2, stick14, -0.221, 0.943, 0.046)
nailed(cube4, stick14, -0.231, 0.937, 0.064)
nailed(stick19, stick15, 0.322, 0.934, 0.082)
nailed(stick20, stick15, 0.357, 0.804, 0.082)
nailed(stick20, stick16, 0.286, 0.690, 0.082)
nailed(stick21, stick16, 0.156, 0.656, 0.082)
nailed(stick21, stick17, 0.008, 0.748, 0.082)
nailed(stick22, stick17, 0.029, 0.825, 0.082)
nailed(stick22, stick18, -0.042, 0.939, 0.082)
nailed(stick23, stick18, -0.172, 0.974, 0.082)
nailed(stick24, stick19, 0.328, 0.930, 0.100)
nailed(cube3, stick19, 0.218, 0.997, 0.082)
nailed(cube5, stick19, 0.225, 0.994, 0.100)
nailed(stick24, stick20, 0.353, 0.799, 0.100)
nailed(stick25, stick20, 0.282, 0.685, 0.100)
nailed(stick25, stick21, 0.150, 0.659, 0.100)
nailed(stick26, stick21, 0.009, 0.747, 0.100)
nailed(stick26, stick22, 0.025, 0.831, 0.100)
nailed(stick27, stick22, -0.046, 0.945, 0.100)
nailed(stick27, stick23, -0.178, 0.970, 0.100)
nailed(cube4, stick23, -0.231, 0.937, 0.082)
nailed(cube6, stick23, -0.241, 0.931, 0.100)
nailed(stick28, stick24, 0.329, 0.924, 0.118)
nailed(stick29, stick24, 0.354, 0.792, 0.118)
nailed(stick29, stick25, 0.276, 0.683, 0.118)
nailed(stick30, stick25, 0.144, 0.658, 0.118)
nailed(stick30, stick26, 0.010, 0.755, 0.118)
nailed(stick31, stick26, 0.026, 0.837, 0.118)
nailed(stick31, stick27, -0.052, 0.946, 0.118)
nailed(stick32, stick27, -0.184, 0.972, 0.118)
nailed(stick33, stick28, 0.334, 0.920, 0.136)
nailed(cube5, stick28, 0.235, 0.990, 0.118)
nailed(cube7, stick28, 0.251, 0.981, 0.136)
nailed(stick33, stick29, 0.351, 0.787, 0.136)
nailed(stick34, stick29, 0.272, 0.678, 0.136)
nailed(stick34, stick30, 0.138, 0.662, 0.136)
nailed(stick35, stick30, 0.012, 0.754, 0.136)
nailed(stick35, stick31, 0.023, 0.843, 0.136)
nailed(stick36, stick31, -0.056, 0.951, 0.136)
nailed(stick36, stick32, -0.190, 0.968, 0.136)
nailed(cube6, stick32, -0.240, 0.931, 0.118)
nailed(cube8, stick32, -0.250, 0.924, 0.136)
nailed(stick37, stick33, 0.335, 0.913, 0.154)
nailed(stick38, stick33, 0.351, 0.780, 0.154)
nailed(stick38, stick34, 0.265, 0.677, 0.154)
nailed(stick39, stick34, 0.132, 0.661, 0.154)
nailed(stick39, stick35, 0.013, 0.761, 0.154)
nailed(stick40, stick35, 0.023, 0.849, 0.154)
nailed(stick40, stick36, -0.063, 0.952, 0.154)
nailed(stick41, stick36, -0.196, 0.968, 0.154)
nailed(stick42, stick37, 0.340, 0.909, 0.172)
nailed(cube7, stick37, 0.255, 0.978, 0.154)
nailed(cube9, stick37, 0.259, 0.977, 0.172)
nailed(stick42, stick38, 0.347, 0.775, 0.172)
nailed(stick43, stick38, 0.261, 0.672, 0.172)
nailed(stick43, stick39, 0.127, 0.665, 0.172)
nailed(stick44, stick39, 0.014, 0.760, 0.172)
nailed(stick44, stick40, 0.019, 0.854, 0.172)
nailed(stick45, stick40, -0.067, 0.957, 0.172)
nailed(stick45, stick41, -0.201, 0.964, 0.172)
nailed(cube8, stick41, -0.249, 0.924, 0.154)
nailed(cube10, stick41, -0.258, 0.917, 0.172)
nailed(stick46, stick42, 0.340, 0.902, 0.190)
nailed(stick47, stick42, 0.347, 0.768, 0.190)
nailed(stick47, stick43, 0.254, 0.672, 0.190)
nailed(stick48, stick43, 0.120, 0.665, 0.190)
nailed(stick48, stick44, 0.015, 0.767, 0.190)
nailed(stick49, stick44, 0.019, 0.861, 0.190)
nailed(stick49, stick45, -0.074, 0.957, 0.190)
nailed(stick50, stick45, -0.208, 0.965, 0.190)
nailed(stick51, stick46, 0.345, 0.898, 0.208)
nailed(cube9, stick46, 0.268, 0.971, 0.190)
nailed(cube11, stick46, 0.275, 0.966, 0.208)
nailed(stick51, stick47, 0.343, 0.764, 0.208)
nailed(stick52, stick47, 0.250, 0.667, 0.208)
nailed(stick52, stick48, 0.115, 0.669, 0.208)
nailed(stick53, stick48, 0.017, 0.765, 0.208)
nailed(stick53, stick49, 0.015, 0.866, 0.208)
nailed(stick54, stick49, -0.078, 0.962, 0.208)
nailed(stick54, stick50, -0.213, 0.960, 0.208)
nailed(cube10, stick50, -0.258, 0.917, 0.190)
nailed(cube12, stick50, -0.266, 0.908, 0.208)
nailed(stick55, stick51, 0.345, 0.891, 0.226)
nailed(stick56, stick51, 0.343, 0.757, 0.226)
nailed(stick56, stick52, 0.243, 0.667, 0.226)
nailed(stick57, stick52, 0.109, 0.670, 0.226)
nailed(stick57, stick53, 0.016, 0.772, 0.226)
nailed(stick58, stick53, 0.015, 0.872, 0.226)
nailed(stick58, stick54, -0.085, 0.962, 0.226)
nailed(stick59, stick54, -0.219, 0.960, 0.226)
nailed(stick60, stick55, 0.350, 0.886, 0.244)
nailed(cube11, stick55, 0.283, 0.959, 0.226)
nailed(cube13, stick55, 0.289, 0.954, 0.244)
nailed(stick60, stick56, 0.338, 0.753, 0.244)
nailed(stick61, stick56, 0.238, 0.663, 0.244)
nailed(stick61, stick57, 0.104, 0.674, 0.244)
nailed(stick62, stick57, 0.019, 0.769, 0.244)
nailed(stick62, stick58, 0.010, 0.877, 0.244)
nailed(stick63, stick58, -0.090, 0.967, 0.244)
nailed(stick63, stick59, -0.224, 0.955, 0.244)
nailed(cube12, stick59, -0.266, 0.908, 0.226)
nailed(cube14, stick59, -0.273, 0.900, 0.244)
nailed(stick64, stick60, 0.349, 0.880, 0.262)
nailed(stick65, stick60, 0.337, 0.746, 0.262)
nailed(stick65, stick61, 0.231, 0.663, 0.262)
nailed(stick66, stick61, 0.098, 0.675, 0.262)
nailed(stick66, stick62, 0.019, 0.776, 0.262)
nailed(stick67, stick62, 0.009, 0.883, 0.262)
nailed(stick67, stick63, -0.097, 0.966, 0.262)
nailed(stick68, stick63, -0.230, 0.954, 0.262)
nailed(stick69, stick64, 0.353, 0.875, 0.280)
nailed(cube13, stick64, 0.296, 0.946, 0.262)
nailed(cube15, stick64, 0.302, 0.940, 0.280)
nailed(stick69, stick65, 0.332, 0.742, 0.280)
nailed(stick70, stick65, 0.226, 0.659, 0.280)
nailed(stick70, stick66, 0.094, 0.680, 0.280)
nailed(stick71, stick66, 0.022, 0.771, 0.280)
nailed(stick71, stick67, 0.004, 0.887, 0.280)
nailed(stick72, stick67, -0.102, 0.970, 0.280)
nailed(stick72, stick68, -0.234, 0.949, 0.280)
nailed(cube14, stick68, -0.273, 0.900, 0.262)
nailed(stick73, stick69, 0.352, 0.868, 0.298)
nailed(stick74, stick69, 0.331, 0.735, 0.298)
nailed(stick74, stick70, 0.220, 0.660, 0.298)
nailed(stick75, stick70, 0.087, 0.681, 0.298)
nailed(stick75, stick71, 0.021, 0.779, 0.298)
nailed(stick76, stick71, 0.003, 0.894, 0.298)
nailed(stick76, stick72, -0.108, 0.969, 0.298)
nailed(stick77, stick73, 0.356, 0.862, 0.316)
nailed(cube15, stick73, 0.308, 0.932, 0.298)
nailed(cube16, stick73, 0.313, 0.925, 0.316)
nailed(stick77, stick74, 0.325, 0.732, 0.316)
nailed(stick78, stick74, 0.214, 0.657, 0.316)
nailed(stick78, stick75, 0.083, 0.687, 0.316)
nailed(stick79, stick75, 0.027, 0.771, 0.316)
nailed(stick79, stick76, -0.003, 0.898, 0.316)
nailed(stick80, stick77, 0.354, 0.856, 0.334)
nailed(stick81, stick77, 0.324, 0.725, 0.334)
nailed(stick81, stick78, 0.208, 0.658, 0.334)
nailed(stick82, stick78, 0.077, 0.688, 0.334)
nailed(stick82, stick79, 0.025, 0.778, 0.334)
nailed(stick83, stick80, 0.357, 0.850, 0.352)
nailed(cube16, stick80, 0.319, 0.917, 0.334)
nailed(cube17, stick80, 0.324, 0.909, 0.352)
nailed(stick83, stick81, 0.318, 0.722, 0.352)
nailed(stick84, stick81, 0.202, 0.655, 0.352)
nailed(stick84, stick82, 0.074, 0.694, 0.352)
nailed(stick85, stick83, 0.356, 0.844, 0.370)
nailed(stick86, stick83, 0.316, 0.716, 0.370)
nailed(stick86, stick84, 0.196, 0.657, 0.370)
nailed(stick87, stick85, 0.358, 0.838, 0.388)
nailed(cube17, stick85, 0.328, 0.901, 0.370)
nailed(stick87, stick86, 0.310, 0.713, 0.388)"""

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

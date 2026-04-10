import json
import re

json_path = r"src\ModelLoader\DemonstratorSetupObjects.json"

new_data_raw = """FinalLocation  FinlLocCube1 (0.18845941910254105,1.0047352320092309,0.007999999977648265  -0.9135454541182548,0.40673665099161094,0 )
FinalLocation  FinlLocCube2 (-0.22086655934088423,0.943059269197909,0.007999999977648265  -0.9135454541182543,-0.4067366509916124,0 )
FinalLocation  FinlLocCube3 (0.20727682928039093,1.0003148695616448,0.04399999970197641  -0.8829475887909904,0.4694715704365665,0 )
FinalLocation  FinlLocCube4 (-0.2313441185902986,0.9371606309116555,0.044000000000000046  -0.8829475887909799,-0.4694715704365861,0 )
FinalLocation  FinlLocCube5 (0.22540741010396936,0.99418382763055,0.07999999999999997  -0.8480480915646952,0.5299192715815104,0 )
FinalLocation  FinlLocCube6 (-0.2407702014548989,0.9308722114989764,0.07999999999999999  -0.8480480915647162,-0.5299192715814768,0 )
FinalLocation  FinlLocCube7 (0.2505048768634444,0.9807460977146477,0.11599999952316171  -0.8090169892818193,0.5877852593025626,0 )
FinalLocation  FinlLocCube8 (-0.24973466489174168,0.9239415800580782,0.1159999999999999  -0.8090169892818297,-0.5877852593025482,0 )
FinalLocation  FinlLocCube9 (0.2593012640330377,0.9768851973483391,0.15200000000000014  -0.7660444375492698,0.6427876163242592,0 )
FinalLocation  FinlLocCube10 (-0.2581938349360671,0.9164025019286749,0.1519999999999999  -0.7660444375492698,-0.6427876163242592,0 )
FinalLocation  FinlLocCube11 (0.27473403818305375,0.9659245839327942,0.1879999993443469  -0.719339794319464,0.6946583766920481,0 )
FinalLocation  FinlLocCube12 (-0.2661064993617323,0.9082917067415889,0.18799999999999994  -0.7193397943194819,-0.6946583766920293,0 )
FinalLocation  FinlLocCube13 (0.2889880761709491,0.9535504857064833,0.22400000000000012  -0.6691305999195628,0.7431448312753619,0 )
FinalLocation  FinlLocCube14 (-0.27291959749712846,0.9002201318033848,0.22399999999999998  -0.6691305999195691,-0.743144831275356,0 )
FinalLocation  FinlLocCube15 (0.3019432000950162,0.9398757242773862,0.2599999999999999  -0.6156614684975493,0.7880107589414252,0 )
FinalLocation  FinlLocCube16 (0.3134893485601621,0.9250228689795346,0.29599999999999993  -0.5591928962871735,0.8290375774004229,0 )
FinalLocation  FinlLocCube17 (0.3235273415989105,0.9091233671580592,0.332  -0.499999992495954,0.8660254081169017,0 )"""

# Parse the new data
new_entries = {}
for line in new_data_raw.strip().split("\n"):
    m = re.match(r'FinalLocation\s+FinlLocCube(\d+)\s+\(([^)]+)\)', line.strip())
    if m:
        cube_num = int(m.group(1))
        parts = m.group(2).strip()
        pos_ori = re.split(r'\s{2,}', parts)
        position = pos_ori[0].strip()
        orientation = pos_ori[1].strip()
        new_entries[cube_num] = {"position": position, "orientation": orientation}

print(f"Parsed {len(new_entries)} new entries")

# Read the JSON file
with open(json_path, "r", encoding="utf-8-sig") as f:
    data = json.load(f)

# Find and update finloccube entries, tracking changes
changes = []
for item in data["instances"]:
    if item.get("type") == "FinalLocation" and item["name"].startswith("finloccube"):
        num_match = re.match(r'finloccube(\d+)', item["name"])
        if num_match:
            cube_num = int(num_match.group(1))
            if cube_num in new_entries:
                old_pos = item["properties"]["position"]
                old_ori = item["properties"]["orientation"]
                new_pos = new_entries[cube_num]["position"]
                new_ori = new_entries[cube_num]["orientation"]
                
                pos_changed = old_pos != new_pos
                ori_changed = old_ori != new_ori
                
                if pos_changed or ori_changed:
                    changes.append({
                        "cube": cube_num,
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
print(f"DIFFERENCES SUMMARY: {len(changes)} cubes changed out of {len(new_entries)} provided")
print(f"{'='*80}\n")

for c in changes:
    print(f"Cube {c['cube']}:")
    if c['pos_changed']:
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

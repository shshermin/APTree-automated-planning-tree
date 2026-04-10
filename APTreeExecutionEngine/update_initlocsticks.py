import json
import re

json_path = r"src\ModelLoader\DemonstratorSetupObjects.json"

new_data_raw = """InitialLocation  InitLocStick1 (0.6073699999999999,-0.2748200000000002,-0.009  -5.551115123125901E-15,-1,0 )
InitialLocation  InitLocStick2 (0.6073699999999999,-0.27482000000000023,-0.005999999999999999  0,-1,0 )
InitialLocation  InitLocStick3 (0.60737,-0.2748200000000001,-0.006  2.7755575615628736E-15,-1,0 )
InitialLocation  InitLocStick4 (0.60737,-0.27482,-0.006  -1.1102230246251556E-14,1,-0 )
InitialLocation  InitLocStick5 (0.60737,-0.27482000000000006,-0.009  -1.6653345369377427E-14,1,-0 )
InitialLocation  InitLocStick6 (0.6073700000000002,-0.27482,-0.006000000000000001  -2.2204460492503358E-14,-1,0 )
InitialLocation  InitLocStick7 (0.60737,-0.27482000000000006,-0.006  -2.7755575615629044E-15,-1,0 )
InitialLocation  InitLocStick8 (0.60737,-0.2748199999999999,-0.005999999999999998  -1.6653345369377427E-14,1,-0 )
InitialLocation  InitLocStick9 (0.6073699999999999,-0.2748200000000002,-0.006  5.551115123125778E-15,1,-0 )
InitialLocation  InitLocStick10 (0.6073700000000002,-0.2748200000000001,-0.009  -5.551115123125778E-15,-1,0 )
InitialLocation  InitLocStick11 (0.6073700000000001,-0.2748200000000001,-0.006000000000000002  0,-1,0 )
InitialLocation  InitLocStick12 (0.60737,-0.2748200000000001,-0.006000000000000002  0,-1,0 )
InitialLocation  InitLocStick13 (0.60737,-0.2748200000000001,-0.006000000000000002  -5.551115123125809E-15,1,-0 )
InitialLocation  InitLocStick14 (0.6073699999999999,-0.2748199999999998,-0.009  8.326672684688529E-15,1,-0 )
InitialLocation  InitLocStick15 (0.60737,-0.27482000000000023,-0.005999999999999998  0,-1,0 )
InitialLocation  InitLocStick16 (0.6073700000000002,-0.27481999999999995,-0.005999999999999998  -5.551115123125747E-15,-1,0 )
InitialLocation  InitLocStick17 (0.60737,-0.27482000000000006,-0.005999999999999998  -0,1,-0 )
InitialLocation  InitLocStick18 (0.6073700000000001,-0.27482000000000006,-0.005999999999999998  -1.1102230246251494E-14,1,-0 )
InitialLocation  InitLocStick19 (0.60737,-0.2748199999999999,-0.009  2.775557561562889E-15,-1,0 )
InitialLocation  InitLocStick20 (0.6073699999999999,-0.2748200000000002,-0.006000000000000012  5.551115123125778E-15,-1,0 )
InitialLocation  InitLocStick21 (0.6073699999999999,-0.2748200000000001,-0.006000000000000012  8.326672684688668E-15,-1,0 )
InitialLocation  InitLocStick22 (0.6073700000000001,-0.27482,-0.006000000000000012  -1.6653345369376963E-14,1,-0 )
InitialLocation  InitLocStick23 (0.6073700000000003,-0.27482000000000006,-0.009  6.938893903907222E-15,1,-0 )
InitialLocation  InitLocStick24 (0.6073700000000001,-0.2748200000000002,-0.006000000000000005  5.551115123125778E-15,-1,0 )
InitialLocation  InitLocStick25 (0.60737,-0.27482000000000006,-0.006000000000000005  0,-1,0 )
InitialLocation  InitLocStick26 (0.60737,-0.2748200000000001,-0.006000000000000005  -5.551115123125778E-15,1,-0 )
InitialLocation  InitLocStick27 (0.6073699999999999,-0.27482000000000006,-0.006000000000000005  -0,1,-0 )
InitialLocation  InitLocStick28 (0.6073699999999999,-0.27481999999999984,-0.009  5.5511151231257164E-15,-1,0 )
InitialLocation  InitLocStick29 (0.6073700000000001,-0.27482000000000006,-0.006000000000000005  -5.551115123125809E-15,-1,0 )
InitialLocation  InitLocStick30 (0.6073700000000001,-0.27482000000000006,-0.006000000000000005  -2.7755575615629044E-15,-1,0 )
InitialLocation  InitLocStick31 (0.60737,-0.27482000000000023,-0.006000000000000005  -5.55111512312587E-15,1,-0 )
InitialLocation  InitLocStick32 (0.60737,-0.2748199999999998,-0.009  -1.1102230246251556E-14,1,-0 )
InitialLocation  InitLocStick33 (0.60737,-0.27481999999999995,-0.006000000000000005  1.1102230246251556E-14,-1,0 )
InitialLocation  InitLocStick34 (0.60737,-0.2748200000000001,-0.006000000000000005  0,-1,0 )
InitialLocation  InitLocStick35 (0.60737,-0.27482,-0.006000000000000019  -5.551115123125809E-15,1,-0 )
InitialLocation  InitLocStick36 (0.60737,-0.27481999999999995,-0.006000000000000019  5.5511151231258395E-15,1,-0 )
InitialLocation  InitLocStick37 (0.6073700000000004,-0.27482000000000023,-0.009  -8.326672684688898E-15,-1,0 )
InitialLocation  InitLocStick38 (0.6073699999999999,-0.27482000000000006,-0.006000000000000005  -5.551115123125747E-15,-1,0 )
InitialLocation  InitLocStick39 (0.6073700000000002,-0.27482000000000006,-0.006000000000000005  -1.1102230246251494E-14,-1,0 )
InitialLocation  InitLocStick40 (0.6073699999999999,-0.27481999999999995,-0.006000000000000033  -0,1,-0 )
InitialLocation  InitLocStick41 (0.6073699999999999,-0.27482000000000006,-0.009  -5.5511151231257164E-15,1,-0 )
InitialLocation  InitLocStick42 (0.60737,-0.2748199999999999,-0.006000000000000005  -1.1102230246251433E-14,-1,0 )
InitialLocation  InitLocStick43 (0.6073700000000001,-0.2748200000000001,-0.006000000000000005  -2.7755575615629044E-15,-1,0 )
InitialLocation  InitLocStick44 (0.6073700000000001,-0.27482,-0.006000000000000005  -1.6653345369377335E-14,1,-0 )
InitialLocation  InitLocStick45 (0.6073699999999997,-0.27482000000000006,-0.006000000000000005  5.551115123125901E-15,1,2.8912057932947114E-15 )
InitialLocation  InitLocStick46 (0.6073699999999996,-0.27481999999999995,-0.009  -1.1102230246251679E-14,-1,0 )
InitialLocation  InitLocStick47 (0.6073700000000001,-0.2748200000000001,-0.005999999999999978  0,-1,0 )
InitialLocation  InitLocStick48 (0.6073699999999995,-0.27482000000000023,-0.006000000000000005  1.1102230246251494E-14,-1,0 )
InitialLocation  InitLocStick49 (0.60737,-0.27482000000000006,-0.006000000000000005  5.551115123125778E-15,1,-0 )
InitialLocation  InitLocStick50 (0.6073700000000002,-0.27481999999999995,-0.009  -5.5511151231258395E-15,1,-0 )
InitialLocation  InitLocStick51 (0.60737,-0.27482000000000006,-0.006000000000000005  0,-1,0 )
InitialLocation  InitLocStick52 (0.6073700000000001,-0.27482000000000006,-0.006000000000000005  -5.5511151231258395E-15,-1,0 )
InitialLocation  InitLocStick53 (0.6073699999999999,-0.27481999999999995,-0.006000000000000005  5.551115123125778E-15,1,-0 )
InitialLocation  InitLocStick54 (0.6073699999999999,-0.2748200000000001,-0.006000000000000005  8.326672684688805E-15,1,-0 )
InitialLocation  InitLocStick55 (0.60737,-0.2748200000000004,-0.009  -8.326672684688713E-15,-1,0 )
InitialLocation  InitLocStick56 (0.6073700000000001,-0.2748200000000001,-0.006000000000000005  1.1102230246251679E-14,-1,0 )
InitialLocation  InitLocStick57 (0.6073700000000001,-0.27482000000000006,-0.006000000000000005  0,-1,0 )
InitialLocation  InitLocStick58 (0.6073700000000001,-0.2748199999999999,-0.006000000000000033  1.66533453693777E-14,1,-0 )
InitialLocation  InitLocStick59 (0.6073699999999999,-0.27481999999999984,-0.009  -5.5511151231257164E-15,1,-0 )
InitialLocation  InitLocStick60 (0.60737,-0.27481999999999995,-0.005999999999999978  5.551115123125778E-15,-1,0 )
InitialLocation  InitLocStick61 (0.60737,-0.27482,-0.005999999999999978  2.7755575615629044E-15,-1,0 )
InitialLocation  InitLocStick62 (0.60737,-0.27482000000000023,-0.006000000000000033  1.1102230246251556E-14,1,-0 )
InitialLocation  InitLocStick63 (0.6073699999999999,-0.27482000000000006,-0.005999999999999978  1.3877787807814599E-15,1,-0 )
InitialLocation  InitLocStick64 (0.6073700000000001,-0.27482000000000095,-0.009  2.775557561562843E-15,-1,0 )
InitialLocation  InitLocStick65 (0.6073700000000001,-0.27482000000000006,-0.006000000000000005  0,-1,0 )
InitialLocation  InitLocStick66 (0.6073699999999999,-0.2748200000000001,-0.006000000000000005  -5.551115123125778E-15,-1,-5.7824115865893785E-15 )
InitialLocation  InitLocStick67 (0.60737,-0.27482000000000006,-0.006000000000000033  5.551115123125809E-15,1,-0 )
InitialLocation  InitLocStick68 (0.6073699999999994,-0.27482,-0.009  -0,1,-0 )
InitialLocation  InitLocStick69 (0.6073699999999999,-0.27482,-0.006000000000000005  5.551115123125809E-15,-1,0 )
InitialLocation  InitLocStick70 (0.60737,-0.27482000000000006,-0.006000000000000005  -2.7755575615628736E-15,-1,0 )
InitialLocation  InitLocStick71 (0.60737,-0.2748200000000003,-0.006000000000000005  -5.551115123125778E-15,1,-0 )
InitialLocation  InitLocStick72 (0.60737,-0.2748200000000001,-0.006000000000000005  9.714451465470112E-15,1,-0 )
InitialLocation  InitLocStick73 (0.6073699999999999,-0.2748200000000002,-0.009  5.551115123125747E-15,-1,0 )
InitialLocation  InitLocStick74 (0.6073700000000001,-0.27482,-0.006000000000000061  -5.551115123125778E-15,-1,0 )
InitialLocation  InitLocStick75 (0.6073700000000001,-0.27481999999999995,-0.006000000000000005  -5.551115123125778E-15,-1,0 )
InitialLocation  InitLocStick76 (0.60737,-0.27482,-0.006000000000000005  -5.551115123125778E-15,1,-0 )
InitialLocation  InitLocStick77 (0.60737,-0.27481999999999984,-0.006000000000000005  0,-1,0 )
InitialLocation  InitLocStick78 (0.6073700000000004,-0.2748200000000001,-0.006000000000000005  2.7755575615628736E-15,-1,0 )
InitialLocation  InitLocStick79 (0.6073699999999999,-0.2748199999999999,-0.006000000000000005  -1.1102230246251494E-14,1,-0 )
InitialLocation  InitLocStick80 (0.60737,-0.2748199999999997,-0.009  0,-1,0 )
InitialLocation  InitLocStick81 (0.6073700000000001,-0.27482000000000006,-0.006000000000000061  5.551115123125809E-15,-1,0 )
InitialLocation  InitLocStick82 (0.6073699999999999,-0.2748200000000001,-0.006000000000000005  0,-1,0 )
InitialLocation  InitLocStick83 (0.6073699999999999,-0.27481999999999995,-0.006000000000000005  -1.6653345369377335E-14,-1,0 )
InitialLocation  InitLocStick84 (0.6073700000000002,-0.27482,-0.00599999999999995  0,-1,0 )
InitialLocation  InitLocStick85 (0.6073700000000003,-0.27481999999999956,-0.009  2.220446049250299E-14,-1,0 )
InitialLocation  InitLocStick86 (0.6073699999999999,-0.27482,-0.006000000000000005  -5.551115123125778E-15,-1,0 )
InitialLocation  InitLocStick87 (0.6073699999999999,-0.27482,-0.006000000000000005  1.1102230246251556E-14,-1,5.7824126205651505E-15 )"""

# Parse the new data
new_entries = {}
for line in new_data_raw.strip().split("\n"):
    m = re.match(r'InitialLocation\s+InitLocStick(\d+)\s+\(([^)]+)\)', line.strip())
    if m:
        stick_num = int(m.group(1))
        parts = m.group(2).strip()
        pos_ori = re.split(r'\s{2,}', parts)
        position = pos_ori[0].strip()
        orientation = pos_ori[1].strip()
        new_entries[stick_num] = {"position": position, "orientation": orientation}

print(f"Parsed {len(new_entries)} new entries")

# Read the JSON file
with open(json_path, "r", encoding="utf-8-sig") as f:
    data = json.load(f)

# Find and update initlocstick entries, tracking changes
changes = []
for item in data["instances"]:
    if item.get("type") == "InitialLocation" and item["name"].startswith("initlocstick"):
        num_match = re.match(r'initlocstick(\d+)', item["name"])
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

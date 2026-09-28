# Manual regression test: `StackDetector.cs` (Grasshopper script)

`StackDetector.cs` runs inside a Grasshopper C# Script component and depends on
Rhino/Grasshopper types (`Brep`, `BoundingBox`), so it cannot be run by the
automated suite (`InputGeneratorTests`, GoogleTest). Its C++ counterpart
(`IsStackedOn` in `spatial_predicates.cpp`) is covered there. Re-run this
checklist by hand in Rhino/Grasshopper before a release, and after any change to
the script.

## Setup

1. New Grasshopper definition with one C# Script component containing
   `StackDetector.cs`; inputs `sticks` (List of Brep) and `cubes` (List of Brep),
   output `A`.
2. Model units: metres (the script's `contactTol = 0.001` is commented "+-1 mm").
   **Rhino Z is up** in this script (it compares `Min.Z` / `Max.Z`).
3. Feed boxes via `Box` -> `Brep` components; names are generated from list order:
   `stick1, stick2, ...` and `cube1, cube2, ...` (capitalised in the output).

## Cases

Unit boxes are 0.05 x 0.05 x 0.05 m unless stated. "Expected" is what the script's
logic prescribes; a mismatch means the script (or this checklist) is wrong.

| # | Scene | Expected `A` |
|---|-------|--------------|
| 1 | one stick, no cubes | `null` / nothing (script returns early when fewer than 2 objects; output is **unset**, not an empty list) |
| 2 | cube1 exactly on top of stick1 (cube bottom Z = stick top Z), same footprint | `["OnTop(Cube1 Stick1)"]` |
| 3 | same as 2 with the cube lifted 0.0005 m (0.5 mm gap) | `["OnTop(Cube1 Stick1)"]` (within +-1 mm) |
| 4 | same as 2 with the cube lifted 0.002 m (2 mm gap) | `[]` |
| 5 | cube1 and stick1 side by side on the floor, touching on a face | `[]` |
| 6 | cube1 on stick1 with footprints overlapping by half | `["OnTop(Cube1 Stick1)"]` |
| 7 | three-high tower: stick1, cube1 on it, cube2 on cube1 | `OnTop(Cube1 Stick1)`, `OnTop(Cube2 Cube1)`; **no** `OnTop(Cube2 Stick1)` |
| 8 | cube1 resting on stick1 and stick2 (bridge) | one `OnTop` line per supporting stick |
| 9 | cube1 on stick1, plus an unrelated cube2 far away | only `OnTop(Cube1 Stick1)` |
| 10 | two boxes whose footprints only touch along a **line/corner** (edge contact), with cube bottom = stick top | see "Known differences" below - record what you see |

## Known differences from the C++ implementation (found by code review)

Record actual behavior for these; they are the likeliest source of surprises
when both tools are used on the same scene:

- **Tolerance**: script `contactTol = 0.001` (metres); C++ `IsStackedOn` uses a
  hard-coded `EPSILON = 1.0` in scene units. The same pair 0.5 m apart is
  "on top" in C++ if the OBJ is in metres, never in the script.
- **Footprint overlap at contact**: the script counts footprints that merely
  touch (`<= ... + contactTol`) as overlapping; C++ requires strict overlap.
  Case 10 is decided differently by the two.
- **Up axis**: script = world Z; C++ default and `main.cpp` = Y ("Rhino uses Y as
  vertical" after OBJ export). Exporting the OBJ with the wrong axis flips
  results (see `ShippedScene.TheSameFileReadAsZUpFindsNoStackAtAll`).
- **Betweenness**: the script has no "nothing between" check (none is needed
  for exact contact); C++ has one that is effectively vacuous for touching boxes.

## Recording results

Date, Rhino version, Grasshopper version, and a pass/fail per case (with the
actual output for any failure or for case 10).

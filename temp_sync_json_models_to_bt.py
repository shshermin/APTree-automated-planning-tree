#!/usr/bin/env python3
"""Temporary strict sync from engine JSON models back to CRFConcrete DSL."""

import argparse
import json
import re
from collections import Counter, defaultdict
from pathlib import Path


ROOT = Path(__file__).resolve().parent
MODEL_DIR = ROOT / "APTreeExecutionEngine" / "src" / "ModelLoader"
CRF_DIR = ROOT / "APTreeDSL" / "src" / "test" / "resources" / "valid" / "CRFConcrete"

SETUP_JSON = MODEL_DIR / "LiveMatSetupObjects.json"
PROPERTY_JSON = MODEL_DIR / "PropertyInstances.json"
STATE_JSON = MODEL_DIR / "InitialStatePredicates.json"
SETUP_BT = CRF_DIR / "LiveMatSetupObjects.bt"
STATE_BT = CRF_DIR / "LiveMatInitialState.bt"

OBJECT_PROPERTIES = {
    "FirstPos": (),
    "PositionOnRail": (),
    "EquipPosition": (),
    "StackPosition": (),
    "Robot": ("loc", "mType"),
    "Beam": ("loc",),
    "Plate": ("loc",),
    "VacGripper": ("loc", "isActive"),
    "NailGripper": ("loc", "isActive"),
    "GlueGun": ("loc", "isActive"),
    "Cassette": ("layers",),
    "Stack": ("level", "belongsToModule"),
}

OBJECT_GROUPS = (
    ("Location - FirstPos instances", "FirstPos"),
    ("Location - PositionOnRail instances", "PositionOnRail"),
    ("Location - EquipPosition instances", "EquipPosition"),
    ("Location - StackPosition instances", "StackPosition"),
    ("Agent - Robot instances", "Robot"),
    ("Element - Beam instances", "Beam"),
    ("Element - Plate instances", "Plate"),
    ("Tool - VacGripper instances", "VacGripper"),
    ("Tool - NailGripper instances", "NailGripper"),
    ("Tool - GlueGun instances", "GlueGun"),
    ("Module - Cassette instances", "Cassette"),
    ("Layer - Stack instances", "Stack"),
)

PREDICATE_PROPERTIES = {
    "Holding": ("item", "agent"),
    "AtPlace": ("item", "loc"),
    "IsAt": ("myObject", "location"),
    "AtAgent": ("agent", "location"),
    "HasTool": ("agent", "tool"),
    "Clear": ("myObject",),
    "OnTop": ("myObject1", "myObject2"),
    "AllSet": ("lay", "mod"),
    "BelongsToLayer": ("myObject", "lay"),
    "BelongsToModule": ("myObject", "mod"),
    "PositionFree": ("pos",),
    "Stacked": ("myObject",),
    "Glued": ("myObject",),
    "Nailed": ("myObject",),
    "VgEmpty": ("client",),
    "AtTool": ("tool", "loc"),
}


def load_records(path: Path, key: str) -> list[dict]:
    with path.open(encoding="utf-8") as stream:
        document = json.load(stream)
    records = document.get(key)
    if not isinstance(records, list):
        raise ValueError(f"{path}: expected a '{key}' array")
    declared_count = document.get("count")
    if declared_count is not None and declared_count != len(records):
        raise ValueError(f"{path}: count={declared_count}, actual={len(records)}")
    return records


def require_exact_keys(record: dict, required: set[str], optional: set[str], context: str) -> None:
    keys = set(record)
    missing = required - keys
    unknown = keys - required - optional
    if missing or unknown:
        raise ValueError(f"{context}: missing keys={sorted(missing)}, unknown keys={sorted(unknown)}")


def validate_objects(records: list[dict]) -> None:
    names = set()
    for index, record in enumerate(records):
        context = f"setup instance #{index + 1}"
        require_exact_keys(record, {"name", "type", "extends"}, {"properties"}, context)
        object_type = record["type"]
        if object_type not in OBJECT_PROPERTIES:
            raise ValueError(f"{context}: unsupported type '{object_type}'")
        if record["name"] in names:
            raise ValueError(f"{context}: duplicate name '{record['name']}'")
        names.add(record["name"])
        properties = record.get("properties", {})
        if not isinstance(properties, dict):
            raise ValueError(f"{context}: properties must be an object")
        allowed = set(OBJECT_PROPERTIES[object_type])
        derived = {"isActive", "layers"}
        unknown = set(properties) - allowed
        missing = allowed - set(properties) - derived
        if unknown or missing:
            raise ValueError(f"{context}: missing properties={sorted(missing)}, unknown properties={sorted(unknown)}")


def validate_predicates(records: list[dict]) -> None:
    for index, record in enumerate(records):
        context = f"predicate #{index + 1}"
        require_exact_keys(record, {"type", "properties"}, set(), context)
        predicate_type = record["type"]
        if predicate_type not in PREDICATE_PROPERTIES:
            raise ValueError(f"{context}: unsupported type '{predicate_type}'")
        properties = record["properties"]
        expected = set(PREDICATE_PROPERTIES[predicate_type]) | {"not"}
        if set(properties) != expected:
            raise ValueError(
                f"{context} {predicate_type}: expected properties={sorted(expected)}, "
                f"actual={sorted(properties)}"
            )


def inventory(label: str, records: list[dict]) -> None:
    print(f"\n=== {label}: {len(records)} records ===")
    for index, record in enumerate(records, 1):
        print(f"{index:03}: {json.dumps(record, sort_keys=True, ensure_ascii=True)}")
    counts = Counter(record["type"] for record in records)
    print("Counts: " + ", ".join(f"{key}={counts[key]}" for key in sorted(counts)))


def old_boolean_values(text: str) -> dict[str, str]:
    values = {}
    pattern = re.compile(r"^(VacGripper|NailGripper|GlueGun)\s+(\w+)\s*\(\S+\s+(True|False)\)\s*$", re.M)
    for match in pattern.finditer(text):
        values[match.group(2)] = match.group(3)
    return values


def render_setup(records: list[dict], old_text: str) -> str:
    by_type = defaultdict(list)
    by_name = {}
    for record in records:
        by_type[record["type"]].append(record)
        by_name[record["name"]] = record

    module_layers = defaultdict(list)
    for stack in by_type["Stack"]:
        properties = stack.get("properties", {})
        module_layers[properties["belongsToModule"]].append(stack["name"])

    old_active = old_boolean_values(old_text)
    lines = [
        "// Generated from APTreeExecutionEngine/src/ModelLoader/LiveMatSetupObjects.json",
        "// Temporary reverse sync; JSON is the source of truth.",
        "// Format: TypeName instanceName (properties)",
        "",
    ]
    emitted = 0
    for heading, object_type in OBJECT_GROUPS:
        group = by_type[object_type]
        if not group:
            continue
        lines.append(f"// {heading} ({len(group)})")
        for record in group:
            properties = dict(record.get("properties", {}))
            if object_type in {"VacGripper", "NailGripper", "GlueGun"}:
                properties["isActive"] = old_active.get(record["name"], "False")
            elif object_type == "Cassette":
                layers = module_layers.get(record["name"], [])
                if not layers:
                    raise ValueError(f"Cassette {record['name']}: no Stack belongs to this module")
                properties["layers"] = " ".join(layers)
            values = [str(properties[key]) for key in OBJECT_PROPERTIES[object_type]]
            lines.append(f"{object_type} {record['name']} ({' '.join(values)})")
            emitted += 1
        lines.append("")
    if emitted != len(records):
        raise ValueError(f"Rendered {emitted} of {len(records)} setup instances")
    return "\n".join(lines).rstrip() + "\n"


def render_state(records: list[dict]) -> str:
    by_type = defaultdict(list)
    for record in records:
        by_type[record["type"]].append(record)

    lines = [
        "// Generated from APTreeExecutionEngine/src/ModelLoader/InitialStatePredicates.json",
        "// Temporary reverse sync; JSON is the source of truth.",
        "// Format: PredicateName(arg1 arg2)",
        "",
    ]
    emitted = 0
    for predicate_type in PREDICATE_PROPERTIES:
        group = by_type[predicate_type]
        if not group:
            continue
        lines.append(f"// {predicate_type} predicates ({len(group)})")
        for record in group:
            properties = record["properties"]
            prefix = "!" if properties["not"] else ""
            arguments = [str(properties[key]) for key in PREDICATE_PROPERTIES[predicate_type]]
            lines.append(f"{prefix}{predicate_type}({' '.join(arguments)})")
            emitted += 1
        lines.append("")
    if emitted != len(records):
        raise ValueError(f"Rendered {emitted} of {len(records)} predicates")
    return "\n".join(lines).rstrip() + "\n"


def compare_property_subset(setup: list[dict], property_instances: list[dict]) -> None:
    setup_by_name = {record["name"]: record for record in setup}
    mismatches = []
    for record in property_instances:
        current = setup_by_name.get(record["name"])
        if current != record:
            mismatches.append(record["name"])
    missing = sorted(setup_by_name.keys() - {record["name"] for record in property_instances})
    print("\n=== PropertyInstances comparison ===")
    print(f"Matching subset records: {len(property_instances) - len(mismatches)}")
    print(f"Mismatched records: {len(mismatches)}")
    print(f"Records absent from stale PropertyInstances.json: {len(missing)}")
    if mismatches:
        print("Mismatches: " + ", ".join(mismatches))
    if missing:
        print("Missing names: " + ", ".join(missing))


def declaration_count(text: str, kind: str) -> int:
    if kind == "object":
        return len(re.findall(r"^\s*\w+\s+\w+\s*\([^)]*\)\s*$", text, re.M))
    return len(re.findall(r"^\s*!?\w+\s*\([^)]*\)\s*$", text, re.M))


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--write", action="store_true", help="replace stale .bt models")
    parser.add_argument("--no-list", action="store_true", help="only print summaries")
    args = parser.parse_args()

    setup = load_records(SETUP_JSON, "instances")
    property_instances = load_records(PROPERTY_JSON, "instances")
    state = load_records(STATE_JSON, "predicates")
    validate_objects(setup)
    validate_objects(property_instances)
    validate_predicates(state)

    if not args.no_list:
        inventory("LiveMatSetupObjects.json", setup)
        inventory("PropertyInstances.json", property_instances)
        inventory("InitialStatePredicates.json", state)
    compare_property_subset(setup, property_instances)

    old_setup = SETUP_BT.read_text(encoding="utf-8")
    old_state = STATE_BT.read_text(encoding="utf-8")
    new_setup = render_setup(setup, old_setup)
    new_state = render_state(state)

    print("\n=== DSL comparison ===")
    print(f"{SETUP_BT.name}: {declaration_count(old_setup, 'object')} -> {len(setup)} instances")
    print(f"{STATE_BT.name}: {declaration_count(old_state, 'predicate')} -> {len(state)} predicates")
    print(f"{SETUP_BT.name} changed: {old_setup != new_setup}")
    print(f"{STATE_BT.name} changed: {old_state != new_state}")

    if args.write:
        SETUP_BT.write_text(new_setup, encoding="utf-8", newline="\n")
        STATE_BT.write_text(new_state, encoding="utf-8", newline="\n")
        print("Updated both stale CRFConcrete models.")
    else:
        print("Dry run only. Re-run with --write to update the models.")


if __name__ == "__main__":
    main()
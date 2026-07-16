from __future__ import annotations

import re
from pathlib import Path


SExpr = str | list["SExpr"]
TOKEN_PATTERN = re.compile(r";[^\n]*|[()]|[^\s()]+")


def parse_sexpr(text: str) -> list[SExpr]:
    tokens = [
        token
        for token in TOKEN_PATTERN.findall(text)
        if not token.startswith(";")
    ]
    position = 0

    def parse_one() -> SExpr:
        nonlocal position
        if position >= len(tokens):
            raise ValueError("Unexpected end of input")

        token = tokens[position]
        position += 1
        if token != "(":
            if token == ")":
                raise ValueError("Unexpected closing parenthesis")
            return token

        expression: list[SExpr] = []
        while position < len(tokens) and tokens[position] != ")":
            expression.append(parse_one())
        if position >= len(tokens):
            raise ValueError("Missing closing parenthesis")
        position += 1
        return expression

    expressions: list[SExpr] = []
    while position < len(tokens):
        expressions.append(parse_one())
    return expressions


def render(expression: SExpr) -> str:
    if isinstance(expression, str):
        return expression
    return "(" + " ".join(render(item) for item in expression) + ")"


def find_section(document: list[SExpr], name: str) -> list[SExpr]:
    for item in document:
        if isinstance(item, list) and item and item[0] == name:
            return item
    raise ValueError(f"Missing {name} section")


def find_field(section: list[SExpr], name: str) -> SExpr:
    for index, item in enumerate(section[:-1]):
        if item == name:
            return section[index + 1]
    raise ValueError(f"Missing {name} field")


def typed_object_lines(objects: list[SExpr]) -> list[str]:
    tokens = objects[1:]
    lines: list[str] = []
    names: list[str] = []
    index = 0
    while index < len(tokens):
        token = tokens[index]
        if not isinstance(token, str):
            raise ValueError("Object declaration contains a nested expression")
        if token == "-":
            if not names or index + 1 >= len(tokens):
                raise ValueError("Malformed typed object declaration")
            object_type = tokens[index + 1]
            if not isinstance(object_type, str):
                raise ValueError("Object type must be a symbol")
            lines.append(f"    {' '.join(names)} - {object_type}")
            names = []
            index += 2
            continue
        names.append(token)
        index += 1

    if names:
        raise ValueError("Untyped trailing objects are not supported")
    return lines


def extract_deliveries(tasks: SExpr) -> list[list[SExpr]]:
    if not isinstance(tasks, list):
        raise ValueError("HTN tasks must be an expression")
    task_items = tasks[1:] if tasks and tasks[0] == "and" else [tasks]
    deliveries: list[list[SExpr]] = []
    for task in task_items:
        if not isinstance(task, list) or len(task) != 3 or task[0] != "deliver":
            raise ValueError(f"Unsupported HTN task: {render(task)}")
        deliveries.append(task)
    return deliveries


def validate_output(
    destination_path: Path,
    source_objects: list[SExpr],
    source_init: list[SExpr],
    deliveries: list[list[SExpr]],
) -> None:
    expressions = parse_sexpr(destination_path.read_text(encoding="ascii"))
    if len(expressions) != 1 or not isinstance(expressions[0], list):
        raise ValueError(f"Invalid generated document: {destination_path.name}")

    define = expressions[0]
    generated_objects = find_section(define, ":objects")
    generated_init = find_section(define, ":init")
    generated_goal = find_section(define, ":goal")
    expected_goals: list[SExpr] = [
        ["at", task[1], task[2]] for task in deliveries
    ]

    if generated_objects != source_objects:
        raise ValueError(f"Object mismatch in {destination_path.name}")
    if generated_init != source_init:
        raise ValueError(f"Initial-state mismatch in {destination_path.name}")
    if generated_goal != [":goal", ["and", *expected_goals]]:
        raise ValueError(f"Delivery-goal mismatch in {destination_path.name}")


def convert_problem(source_path: Path, destination_path: Path) -> int:
    expressions = parse_sexpr(source_path.read_text(encoding="utf-8"))
    if len(expressions) != 1 or not isinstance(expressions[0], list):
        raise ValueError(f"Expected one top-level expression in {source_path.name}")

    define = expressions[0]
    if not define or define[0] != "define":
        raise ValueError(f"Expected a define expression in {source_path.name}")

    objects = find_section(define, ":objects")
    htn = find_section(define, ":htn")
    init = find_section(define, ":init")
    deliveries = extract_deliveries(find_field(htn, ":tasks"))

    ordering = find_field(htn, ":ordering")
    constraints = find_field(htn, ":constraints")
    if ordering != [] or constraints != []:
        raise ValueError(
            f"{source_path.name} has ordering or constraints that cannot be "
            "represented by an unordered classical goal conjunction"
        )

    problem_name = f"transport-{source_path.stem}"
    lines = [
        f"(define (problem {problem_name})",
        "  (:domain transport)",
        "  (:objects",
        *typed_object_lines(objects),
        "  )",
        "  (:init",
        *(f"    {render(fact)}" for fact in init[1:]),
        "  )",
        "  (:goal (and",
        *(f"    (at {task[1]} {task[2]})" for task in deliveries),
        "  ))",
        ")",
        "",
    ]
    destination_path.write_text("\n".join(lines), encoding="ascii")
    validate_output(destination_path, objects, init, deliveries)
    return len(deliveries)


def main() -> None:
    output_dir = Path(__file__).resolve().parent
    source_dir = output_dir.parent / "HTN problems and domain"
    source_files = sorted(source_dir.glob("pfile*.hddl"))
    if len(source_files) != 40:
        raise RuntimeError(f"Expected 40 HDDL problems, found {len(source_files)}")

    total_deliveries = 0
    for source_path in source_files:
        destination_path = output_dir / f"{source_path.stem}.pddl"
        total_deliveries += convert_problem(source_path, destination_path)

    print(
        f"Converted {len(source_files)} problems with "
        f"{total_deliveries} delivery goals"
    )


if __name__ == "__main__":
    main()

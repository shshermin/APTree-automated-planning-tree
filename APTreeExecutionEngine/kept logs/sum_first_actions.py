import csv
import sys

def sum_first_actions(csv_path):
    seen = set()
    total = 0
    details = []

    exclude_suffixes = ('C1.pddl', 'C2.pddl', 'C3.pddl', 'C4.pddl')

    with open(csv_path, newline='') as f:
        reader = csv.DictReader(f)
        for row in reader:
            problem = row['ProblemFile']
            if problem.endswith(exclude_suffixes):
                continue
            if problem not in seen:
                seen.add(problem)
                actions = int(row['ActionsGenerated'])
                total += actions
                details.append((problem, actions))

    print(f"{'ProblemFile':<70} ActionsGenerated")
    print("-" * 90)
    for problem, actions in details:
        print(f"{problem:<70} {actions}")
    print("-" * 90)
    print(f"{'Total':<70} {total}")
    print(f"\nUnique problem files: {len(details)}")

if __name__ == '__main__':
    path = sys.argv[1] if len(sys.argv) > 1 else 'PlannerCalls_2026-02-19_19-41-24.csv'
    sum_first_actions(path)

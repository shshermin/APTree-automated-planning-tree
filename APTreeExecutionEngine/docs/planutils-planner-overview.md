# Planutils Planner Overview

All planners available through [Planutils](https://github.com/AI-Planning/planutils).
The table covers the planners most relevant for integration; the full package list contains ~116 entries including IPC competition variants.

## Planner Table

| Package Name | Full Name | PDDL Support | Planning Type | Configurations / Modes | Optimal? | Install Command |
|---|---|---|---|---|---|---|
| `ff` | Fast-Forward | STRIPS, ADL (PDDL 1.x / 2.1 L1) | Classical | EHC+H (default), BFS fallback | Satisficing | `planutils install ff` |
| `metric-ff` | Metric-FF | PDDL 2.1 Level 2 (numerical fluents) | Classical + Numerical | 6 search modes via `searchflag`: EHC+H (0), BFS (1), BFS+helpful actions (2), Weighted A\* (3), A\*ε (4), EHC+H then A\*ε (5) | Modes 0–2: Satisficing; Modes 3–5: Near-optimal (WA\*/A\*ε) | `planutils install metric-ff` |
| `metric-ff-2.0` | Metric-FF 2.0 | PDDL 2.1 Level 2 + axioms | Classical + Numerical | Same 6 search modes as metric-ff; adds axiom support | Modes 0–2: Satisficing; Modes 3–5: Near-optimal | `planutils install metric-ff-2.0` |
| `lama` | LAMA (anytime) | STRIPS, ADL, action costs (PDDL 2.2) | Classical (anytime) | Phase 1: greedy BFS → Phase 2+: iterative improvement | Anytime / Satisficing (improves over time) | `planutils install lama` |
| `lama-first` | LAMA-First | STRIPS, ADL, action costs | Classical | Greedy BFS, stops at first solution | Satisficing | `planutils install lama-first` |
| `downward` | Fast Downward | STRIPS, ADL, action costs (PDDL 2.2) | Classical (highly configurable) | `--search "astar(lmcut())"` (optimal), `--search "lazy_greedy([ff()])"` (satisficing), LAMA config, many more | Both — config-dependent | `planutils install downward` |
| `scorpion` | Scorpion | STRIPS, ADL, action costs | Classical | Saturated cost partitioning over abstractions (default); multiple heuristic combinations | **Optimal** | `planutils install scorpion` |
| `symk` | SymK (Sym-k) | STRIPS, ADL, action costs | Classical top-k / optimal | Top-k: generates up to k cheapest plans (default k=100); Optimal single: symbolic bidirectional search | **Optimal** (cost-wise) | `planutils install symk` |
| `kstar` | K\* | STRIPS, ADL, action costs | Classical top-k quality | `number_of_plans` (default 10), `quality` bound (default "1.0"), `--unordered` toggle | **Optimal** (within quality bound) | `planutils install kstar` |
| `optic` | OPTIC | PDDL 2.1 (`:durative-actions`, `:numeric-fluents`, `:preferences`, `:timed-initial-literals`) | Temporal + Numerical + Preferences | Single mode; optimises preferences and time-dependent costs | Satisficing / Preference-optimal | `planutils install optic` |
| `popf` | POPF2 | PDDL 2.1 (`:durative-actions`, `:numeric-fluents`, continuous effects) | Temporal + Numerical | Anytime (returns improving solutions) | Satisficing (anytime) | `planutils install popf` |
| `tfd` | Temporal Fast Downward | PDDL 2.1 (`:durative-actions`) | Temporal | Single mode; context-enhanced additive heuristic | Satisficing | `planutils install tfd` |
| `lpg-td` | LPG-TD | PDDL 2.2 (`:durative-actions`, `:numeric-fluents`, timed initial literals) | Temporal + Numerical | Local search on planning graphs; speed/quality tradeoff flags | Satisficing | `planutils install lpg-td` |
| `pyperplan` | Pyperplan | STRIPS (PDDL 1.2, no numeric fluents) | Classical | Multiple search algorithms (BFS, DFS, A\*, WA\*, GBFS, enforced HC) selectable via flag | Satisficing (A\* variant near-optimal) | `planutils install pyperplan` |
| `powerlifted` | Powerlifted | STRIPS + `:typing`, inequalities, object creation (lifted, no grounding) | Classical | Single mode; lifted state-space search | Satisficing | `planutils install powerlifted` |
| `cerberus` | Cerberus | STRIPS, ADL, action costs | Classical | Base: red-black heuristic + novelty guidance; `cerberus-agl` (agile GBFS), `cerberus-sat` (satisficing) | Satisficing | `planutils install cerberus` |
| `cerberus-agl` | Cerberus Agile | STRIPS, ADL, action costs | Classical | GBFS with deferred heuristic evaluation | Satisficing | `planutils install cerberus-agl` |
| `cerberus-sat` | Cerberus Satisficing | STRIPS, ADL, action costs | Classical | Satisficing configuration | Satisficing | `planutils install cerberus-sat` |
| `dual-bfws-ffparser` | BFWS (FF-parser) | STRIPS, ADL (classical) | Classical | Best-First Width Search; width-based novelty + FF heuristic | Satisficing | `planutils install dual-bfws-ffparser` |
| `dual-bfws-fdparser` | BFWS (FD-parser) | STRIPS, ADL (classical) | Classical | Best-First Width Search; FD-parser variant | Satisficing | `planutils install dual-bfws-fdparser` |
| `prp` | PRP (Planning for Relevant Policies) | FOND PDDL (`:non-deterministic` effects, `:conditional-effects`) | Fully Observable Non-Deterministic (FOND) | Policy-based search | Satisficing (strong cyclic policy) | `planutils install prp` |
| `forbiditerative` | Forbid-Iterative | STRIPS, ADL, action costs | Classical (diverse / top-k/q) | `forbiditerative-topk`, `forbiditerative-topq`, `forbiditerative-diverse-agl`, `forbiditerative-diverse-sat` | Satisficing (diverse solutions) | `planutils install forbiditerative` |
| `forbiditerative-topk` | FI Top-k | STRIPS, ADL, action costs | Classical top-k | Finds k cheapest plans | Optimal per plan found | `planutils install forbiditerative-topk` |
| `forbiditerative-diverse-sat` | FI Diverse Satisficing | STRIPS, ADL, action costs | Classical diverse | Diverse satisficing plans | Satisficing | `planutils install forbiditerative-diverse-sat` |
| `cpddl` | cpddl | STRIPS, ADL (lifted + ground search) | Classical | Lifted pruning, FDR translation, symbolic search | Both (config-dependent) | `planutils install cpddl` |
| `smtplan` | SMTPlan+ | PDDL 2.1 (`:numeric-fluents`, `:durative-actions`, nonlinear) | Temporal + Nonlinear Numerical | SMT-based encoding | Satisficing | `planutils install smtplan` |
| `lapkt` | LAPKT toolkit | STRIPS, ADL | Classical | Framework: BFWS, SIW, SIW-then-BFS variants | Satisficing | `planutils install lapkt` |
| `val` | VAL (Plan Validator) | PDDL 2.1+ | Validation (not a planner) | Validates plan correctness | N/A | `planutils install val` |
| `macq` | MACQ | — | Action model acquisition | Learns PDDL models from observations | N/A | `planutils install macq` |

---

## Notes

- **Optimal** means the planner guarantees a cost-minimal plan.
- **Satisficing** means the planner finds *a* valid plan without cost guarantee.
- **Anytime** means the planner keeps improving the solution if given more time.
- **Top-k** means the planner finds the k best (cheapest) distinct plans.
- Planners marked *config-dependent* can do both depending on the search algorithm configured.
- ENHSP (used in this project) is installed separately as a JAR — it is **not** a Planutils package.
- All Planutils planners are invoked inside the `planutils` Docker container via `planutils run <package> domain.pddl problem.pddl`.

---

## Currently Integrated Planners

| C# Class | Planutils Package | Type | Optimal |
|---|---|---|---|
| `PlannerMetricFF` | `ff`, `metric-ff` | Classical / Numerical | Satisficing |
| `PlannerENHSP` | — (JAR) | Numerical | Both (config-dependent) |
| `PlannerLAMAFirst` | `lama-first` | Classical | Satisficing |
| `PlannerScorpion` | `scorpion` | Classical | **Optimal** |
| `PlannerFastDownward` | `downward` | Classical | Both (config-dependent) |
| `PlannerOPTIC` | `optic` | Temporal + Numerical | Satisficing |
| `PlannerPOPF` | `popf` | Temporal + Numerical | Satisficing |
| `PlannerPyperplan` | `pyperplan` | Classical STRIPS | Satisficing |

# APTree Lean Formalization

Machine-checked Lean 4 proofs for the paper's formal correctness analysis. The
artifact proves the three main theorems and checks finite counterexamples that
show why their key hypotheses are necessary.

## Reproduce the proofs

Install [Lean 4](https://lean-lang.org/install/). The pinned toolchain
(`leanprover/lean4:v4.32.0`) is selected automatically. From this directory, run:

```bash
lake build
```

The project uses only Lean core and contains no `sorry` or `native_decide`.
For individual theorem and axiom checks, run:

```
lake env lean CheckTheorem1.lean  # standalone check: refinement soundness
lake env lean CheckTheorem2.lean  # standalone check: local replanning
lake env lean CheckTheorem3.lean  # standalone check: partial-order execution
lake env lean Audit.lean          # axiom footprint of every headline theorem
```

Axiom footprint: the positive theorems use only `propext`; the countermodels additionally use `Quot.sound` (and `Classical.choice` in one place), all standard Lean axioms introduced by `decide`/`simp` on `List`. Nothing is assumed beyond Lean's kernel.

## Layout

| File | Contents |
|---|---|
| `APTree/Planning.lean` | STRIPS substrate: literals, states as total valuations, actions with `pre`/`add`/`del`, plans, applicability, planners, **Assumption (Planner soundness)** |
| `APTree/Interference.lean` | **Definition (Goal interference)**, clobbering, and the goal-persistence lemma |
| `APTree/Refinement.lean` | **Theorem (Refinement soundness)**: blocks (ML subtrees as executed), lazy ML planning, sequential composition, **Invariants: initial non-interference, goal inheritance, ML non-interference** |
| `APTree/Replanning.lean` | **Theorem (Local replanning)**: `BTNodeResult`, quiescent execution, DFN with archived/active split, the `Replan` operation, **Invariants: goal immutability, status exclusion** |
| `APTree/PartialOrder.lean` | **Theorem (Partial-order execution)**: partial-order plans, linearizations, **Invariants: precondition-guarded execution, predecessor completion** (`canExecute`) |
| `APTree/SuccessCriteria.lean` | `All`/`Any`/`Count`/`Percentage` and their relation to goal achievement (why the analysis is restricted to `All`) |
| `APTree/Counterexamples.lean` | Finite countermodels showing the necessity of the hypotheses, fluents named after the LivMatS case study |

## Paper ↔ Lean map

| Appendix | Lean | Note |
|---|---|---|
| `D = ⟨P, A⟩`, `Π = ⟨D, s₀, g⟩` | `Action F`, `State F`, `Goal F` | domain implicit in the action type |
| `S(Π)` | `Planner.run` | |
| Assumption (Planner soundness) | `Planner.Sound` | hypothesis, as in the paper |
| Definition (Goal interference), goal/goal | `Interferes` | |
| Definition (Goal interference), plan/goal | `PlanInterferes` | both clauses (deleted positive / added negative literals) |
| Invariant (Initial non-interference) | `InitialNonInterference` | over HL goal/plan pairs |
| Invariant (Goal inheritance), `goal(Fᵢ) = eff(aᵢ)` | `Block.Inherits`, `Action.effGoal` | effects read as a goal; definitional in `Block` |
| Invariant (ML non-interference), inherited-goal clause | `MLNonInterference` (= `TailPreserves`) | necessity: countermodel 2 |
| Invariant (ML non-interference), subgoal clause | hypothesis of `hl_goal_achieved` | literal-wise preservation |
| **Theorem (Refinement soundness)** | `theorem1_refinement_sound`, `hl_goal_achieved` | bridge to `g₁ ∧ … ∧ g_k` |
| Definition (Quiescent execution) | `ActionQuiescent` | action nodes only; see `quiescent_excludes_replan_trigger` for why |
| Definition (Replan operation) | `Dfn.replan` | |
| Invariant (Goal immutability) | `Dfn.replan` passes `d.goal` | definitional |
| Invariant (Status excludes archived) | `Dfn.status`, `Dfn.status_indep_archived` | definitional |
| **Theorem (Local replanning)** | `theorem2_replan_sound`, `replan_preserves_sibling` | locality via ML non-interference |
| Invariant (HL plan immutability) | block goals fixed; `replan` only on `Dfn` | definitional |
| Invariant (ML non-preemption) | `runBlocks` (blocks run contiguously) | definitional; necessity: countermodel 3 |
| Invariant (Precondition-guarded execution) | `Pop.PrecondGuarded` | |
| Invariant (Predecessor completion) / `canExecute` | `Pop.PrecedenceGuarded` | |
| **Theorem (Partial-order execution)** | `theorem3_safety`, `respects_of_precedenceGuarded` | |
| `SuccessCriteria` | `SuccessCriteria.succeeded` | |

## Results proved

**Positive.**

- `Models.run` — goal persistence: a satisfied goal survives any plan that clobbers none of its literals.
- `composition_sound` — sequential composition: if every block reaches its own goal and no later block clobbers an earlier goal, the final state satisfies the conjunction of all goals. The order of the blocks does not appear in the proof.
- `lazyPlanned_achieves`, `lazyPlanned_applicable` — planner soundness plus lazy ML planning gives both goal achievement and applicability for every block.
- `theorem1_refinement_sound` — Theorem (Refinement soundness): the schedule is applicable and the final state satisfies every inherited goal `eff(aᵢ)`.
- `hl_goal_achieved` — the bridge to the overall high-level goal: a subgoal literal established by some high-level action lies in an inherited goal and holds finally; one that holds initially and is clobbered by no ML subtree persists.
- `theorem2_replan_sound`, `theorem2_replan_idempotent` — Theorem (Local replanning) and its iteration over sequences of replanning events.
- `replan_preserves_sibling` — the locality clause: with ML non-interference, a sibling's achieved goal survives the replanned subtree.
- `respects_of_precedenceGuarded` — the linearization step: predecessor completion (the behavior of `canExecute`) yields a schedule respecting `≺`.
- `theorem3_reduces_to_theorem1`, `theorem3_safety` — Theorem (Partial-order execution), safety form.
- `quiescent_excludes_replan_trigger` — quiescence over *all* nodes would be incompatible with the replan trigger; this is why quiescence is defined over action nodes (`actionQuiescent_allows_replan_trigger`).

**Hypothesis necessity (finite countermodels, all `decide`-checked).**

1. `negative_clause_necessary` — the plan/goal interference clause must cover negative goal literals: a plan can destroy a goal literal `¬p` while deleting nothing.
2. `initial_noninterference_insufficient` — initial (HL) non-interference, goal inheritance and planner soundness all hold, every ML subtree reaches its inherited goal, and the composite execution still misses `g₁ ∧ g₂`; `mlNonInterference_sufficient` shows the same schedule succeeds once ML non-interference holds.
3. `interleaving_breaks_applicability` — atomic execution of two branches works, action-level interleaving is not even applicable: ML non-preemption cannot be dropped.
4. `precondGuarded_not_respects` — a precondition-guarded schedule that violates `≺`: the linearization step rests on predecessor completion, not on precondition-guardedness.
5. `successCriteria_breaks_goal_achievement` — under `Any`, `Count 1` or `Percentage 50` a DFN reports `Success` while its goal fails: the restriction to the `All` criterion cannot be dropped.

## Scope of the model

What is modelled: planning domains, plans, world states, goal achievement and interference, the DFN archived/active split, the `Replan` operation, partial orders and linearizations, the four success criteria.

What is **not** modelled: the tick semantics (`onEnter`/`onTickNodeLogic`/`onExit`, decorators, services, the blackboard), the DSL metamodels and their well-formedness rules, Allen's interval algebra beyond a bare precedence relation, and timing. The formalization targets the abstract execution model of the appendix; connecting it to an operational semantics of the tick loop is future work.

/-
# Logical composability under partial-order execution (Theorem: Partial-order execution)

This file formalizes partial-order plans, linearizations, precondition-guarded
execution (Invariant *Precondition-guarded execution*) and predecessor-completion
(Invariant *Predecessor completion*, the behavior implemented by `canExecute`), and
proves:

* the linearization step: predecessor-completion yields a schedule respecting `≺`
  (`respects_of_precedenceGuarded`). Precondition-guardedness alone does not — see
  `Counterexamples.precondGuarded_not_respects` — which is why the theorem's proof
  cites predecessor completion for this step;
* the theorem's conclusion: once the ML blocks are atomic and non-interfering, any
  completed interleaved execution achieves all goals
  (`theorem3_reduces_to_theorem1`, `theorem3_safety`).
-/

import APTree.Replanning

namespace APTree

universe u

variable {F : Type u} [DecidableEq F]

/-! ## Partial-order plans -/

/-- A partial-order plan `⟨A′, ≺⟩`: actions indexed by their position in `acts`, with
`prec` listing the pairs `(i, j)` such that `acts[i] ≺ acts[j]`.

In APTree these constraints are the `Relation` edges of a `NodeGraph`, annotated with
Allen temporal types; the absence of an edge "indicates no ordering constraint". -/
structure Pop (F : Type u) where
  /-- `A′`. -/
  acts : List (Action F)
  /-- `≺`, as a list of index pairs. -/
  prec : List (Nat × Nat)

/-- A harmless no-op, used as the out-of-range default. -/
def noop : Action F := { name := "noop", pre := [], add := [], del := [] }

/-- The action at a given index. -/
def Pop.actAt (p : Pop F) (i : Nat) : Action F := (p.acts[i]?).getD noop

/-- A schedule is the sequence of action indices in the order the executor releases
them. -/
abbrev Schedule : Type := List Nat

/-! ## "Occurs before" -/

/-- `beforeB i j order = true` iff `i` occurs in `order` strictly before the first
occurrence of `j`. -/
def beforeB (i j : Nat) : Schedule → Bool
  | [] => false
  | k :: rest => if k = i then true else if k = j then false else beforeB i j rest

/-- If `i` has already been executed and `j` has not, then `i` occurs before `j`. -/
theorem beforeB_append_of_mem {i j : Nat} {done rest : Schedule}
    (hi : i ∈ done) (hj : j ∉ done) : beforeB i j (done ++ rest) = true := by
  induction done with
  | nil => exact absurd hi (by simp)
  | cons k d ih =>
    rw [List.cons_append, beforeB]
    by_cases hki : k = i
    · simp [hki]
    · have hkj : k ≠ j := fun h => hj (by simp [h])
      rw [if_neg hki, if_neg hkj]
      exact ih (by
        rcases List.mem_cons.1 hi with h | h
        · exact absurd h.symm hki
        · exact h)
        (fun h => hj (List.mem_cons_of_mem _ h))

/-- A schedule *respects* `≺` when, whenever a constrained successor is released, its
predecessor was released strictly earlier. This is the linearization property used in
the proof of the partial-order theorem. -/
def Pop.Respects (p : Pop F) (order : Schedule) : Prop :=
  ∀ ij ∈ p.prec, ij.2 ∈ order → beforeB ij.1 ij.2 order = true

/-! ## The two guard conditions -/

/-- **Invariant (Precondition-guarded execution).** "A high-level action in a
partial-order plan becomes eligible for execution only when all of its preconditions are
satisfied in the current world state, regardless of whether it is unordered with respect
to sibling actions."

Note that this condition does not mention `≺`; the linearization step therefore rests
on predecessor completion (`PrecedenceGuarded`), not on this invariant. -/
def Pop.PrecondGuarded (p : Pop F) (s : State F) : Schedule → Prop
  | [] => True
  | i :: rest => (p.actAt i).enabled s ∧ p.PrecondGuarded ((p.actAt i).apply s) rest

/-- Boolean version, for `decide`. -/
def Pop.precondGuardedB (p : Pop F) (s : State F) : Schedule → Bool
  | [] => true
  | i :: rest => modelsB s (p.actAt i).pre && p.precondGuardedB ((p.actAt i).apply s) rest

theorem Pop.precondGuarded_iff {p : Pop F} {s : State F} {order : Schedule} :
    p.PrecondGuarded s order ↔ p.precondGuardedB s order = true := by
  induction order generalizing s with
  | nil => simp [Pop.PrecondGuarded, Pop.precondGuardedB]
  | cons i rest ih =>
    simp only [Pop.PrecondGuarded, Pop.precondGuardedB, Bool.and_eq_true, Action.enabled]
    rw [models_iff_modelsB, ih]

/-- **Invariant (Predecessor completion)**: the behavior implemented by `canExecute` —
it releases a node only if it has not already completed and all temporal relations to
its predecessors are satisfied.

`PrecedenceGuarded done order` says the executor releases `order` after having already
completed `done`, never re-releasing a completed node and never releasing a node before
all of its `≺`-predecessors are complete. -/
def Pop.PrecedenceGuarded (p : Pop F) : Schedule → Schedule → Prop
  | _done, [] => True
  | done, i :: rest =>
      i ∉ done ∧
      (∀ ij ∈ p.prec, ij.2 = i → ij.1 ∈ done) ∧
      p.PrecedenceGuarded (done ++ [i]) rest

omit [DecidableEq F] in
/-- **The linearization step.**

If the executor releases an action only after all of its `≺`-predecessors have completed
— which is what `canExecute` does, via the `NodeGraph` relations — then the resulting
execution order is a linearization respecting `≺`. -/
theorem respects_of_precedenceGuarded (p : Pop F) (order : Schedule)
    (h : p.PrecedenceGuarded [] order) : p.Respects order := by
  suffices H : ∀ (done rest : Schedule), p.PrecedenceGuarded done rest →
      ∀ ij ∈ p.prec, ij.2 ∈ rest → beforeB ij.1 ij.2 (done ++ rest) = true by
    intro ij hij hmem
    simpa using H [] order h ij hij hmem
  intro done rest
  induction rest generalizing done with
  | nil => intro _ ij _ hmem; exact absurd hmem (by simp)
  | cons i rest ih =>
    intro hpg ij hij hmem
    obtain ⟨hfresh, hpred, hpg'⟩ := hpg
    rcases List.mem_cons.1 hmem with heq | hmem'
    · -- The constrained successor is released now: its predecessor is already in `done`.
      have h1 : ij.1 ∈ done := hpred ij hij heq
      have h2 : ij.2 ∉ done := by rw [heq]; exact hfresh
      exact beforeB_append_of_mem h1 h2
    · have := ih (done ++ [i]) hpg' ij hij hmem'
      rwa [List.append_assoc, List.singleton_append] at this

/-! ## The theorem -/

/-- **Theorem (Partial-order execution), machine-checked form.**

**Invariant (HL plan immutability)** is definitional in this model: the list of block
goals comes from the high-level plan at initialization and no operation rewrites it;
`Dfn.replan` applies to a `Dfn`, i.e. to a mid-level subtree only.

Under planner soundness, lazy mid-level planning and ML non-interference, the
interleaved execution of sibling DFNs reaches the conjunction of all inherited goals.
Once the ML blocks are atomic (ML non-preemption, built into `runBlocks`) and
non-interfering, the conclusion holds for *any* interleaving at block granularity,
so the theorem reduces to refinement soundness. -/
theorem theorem3_reduces_to_theorem1 {S : Planner F} (hS : S.Sound) (s₀ : State F)
    (bs : List (Block F)) (hlazy : LazyPlanned S s₀ bs) (hni : TailPreserves bs) :
    runBlocks s₀ bs ⊨ allGoals bs :=
  (theorem1_refinement_sound hS s₀ bs hlazy hni).2

/-- The safety form of the theorem — "any execution that completes achieves the goal" —
with applicability of every block made explicit. Nothing here claims that a completing
execution exists; that is the progress property left as future work. -/
theorem theorem3_safety {S : Planner F} (hS : S.Sound) (s₀ : State F)
    (bs : List (Block F)) (hlazy : LazyPlanned S s₀ bs) (hni : TailPreserves bs) :
    AllApplicable s₀ bs ∧ runBlocks s₀ bs ⊨ allGoals bs :=
  theorem1_refinement_sound hS s₀ bs hlazy hni

end APTree

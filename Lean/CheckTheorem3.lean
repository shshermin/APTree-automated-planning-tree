/-
# Standalone check for the theorem "Logical correctness under partial-order execution"

Run with:  lake env lean CheckTheorem3.lean

If this file compiles, Lean's kernel has verified:
  (a) the linearization step — predecessor completion (the behavior of `canExecute`)
      yields a schedule respecting `≺`;
  (b) hypothesis necessity: precondition-guardedness alone does NOT yield that step
      (finite countermodel), so predecessor completion cannot be dropped;
  (c) the theorem's conclusion — any completed interleaved execution of atomic,
      non-interfering, lazily planned ML subtrees achieves all goals (safety);
  (d) hypothesis necessity: ML non-preemption cannot be dropped — action-level
      interleaving can break applicability even when sequential execution succeeds.
-/
import APTree.Counterexamples

open APTree

/-! ## (a) Predecessor completion gives a valid linearization

If the executor releases an action only after all its `≺`-predecessors completed and
never re-releases completed nodes (`PrecedenceGuarded`, i.e. `canExecute`), the executed
schedule respects `≺`. -/
theorem check_linearization {F : Type} [DecidableEq F]
    (p : Pop F) (order : Schedule)
    (h : p.PrecedenceGuarded [] order) :
    p.Respects order :=
  respects_of_precedenceGuarded p order h

/-! ## (b) Precondition-guardedness alone does not give it

A precondition-guarded schedule that violates `≺`: with `travel ≺ place` and both
preconditions holding initially, `[place, travel]` is precondition-guarded and breaks
the ordering. The linearization step therefore rests on predecessor completion. -/
theorem check_invariant_precondition_insufficient :
    ∃ (p : Pop Counterexamples.Fl) (s : State Counterexamples.Fl) (order : Schedule),
      p.PrecondGuarded s order ∧ ¬ p.Respects order :=
  ⟨Counterexamples.popMeets, Counterexamples.s₀, [1, 0],
   Counterexamples.precondGuarded_not_respects⟩

/-! ## (c) The theorem, safety form

Given planner soundness, lazy ML planning, atomic blocks (ML non-preemption, built into
`runBlocks`), and ML non-interference, any completed execution achieves the conjunction
of all inherited goals — for ANY interleaving of the branches at block granularity. -/
theorem check_theorem3 {F : Type} [DecidableEq F]
    (S : Planner F) (hS : S.Sound)              -- planner soundness
    (s₀ : State F)
    (bs : List (Block F))                        -- the interleaved schedule of subtrees
    (hlazy : LazyPlanned S s₀ bs)                -- lazy ML planning
    (hni : MLNonInterference bs) :               -- ML non-interference
    AllApplicable s₀ bs ∧ runBlocks s₀ bs ⊨ allGoals bs :=
  theorem3_safety hS s₀ bs hlazy hni

/-! ## (d) ML non-preemption (atomicity) is necessary

Sequential execution of the two branches works; interleaving them at action granularity
is not even applicable. -/
theorem check_atomicity_needed :
    Plan.applicable Counterexamples.s₀ (Counterexamples.τ₁ ++ Counterexamples.τ₂) ∧
    ¬ Plan.applicable Counterexamples.s₀
        [Counterexamples.pickPlate, Counterexamples.pickBeam,
         Counterexamples.placePlate, Counterexamples.placeBeam] :=
  ⟨Counterexamples.interleaving_breaks_applicability.1,
   Counterexamples.interleaving_breaks_applicability.2.2⟩

/-! ## Axiom audit — `sorryAx` here would mean something was assumed, not proved -/

#print axioms check_linearization
#print axioms check_invariant_precondition_insufficient
#print axioms check_theorem3
#print axioms check_atomicity_needed

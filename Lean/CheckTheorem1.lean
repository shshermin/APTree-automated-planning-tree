/-
# Standalone check for the theorem "Refinement soundness"

Run with:  lake env lean CheckTheorem1.lean

If this file compiles, Lean's kernel has verified:
  (a) the theorem: under planner soundness, goal inheritance, lazy ML planning,
      ML non-preemption and ML non-interference, any execution of the ML subtrees
      is applicable and achieves the overall goal;
  (b) hypothesis necessity: initial (HL) non-interference alone does not suffice —
      ML non-interference is required (finite countermodel);
  (c) neither result relies on `sorry` or nonstandard axioms (see `#print axioms`).
-/
import APTree.Counterexamples

open APTree

/-! ## (a) The theorem

Given
  * Assumption (Planner soundness)  — a sound planner `S`,
  * Invariant (Goal inheritance)    — definitional in `Block`: each ML subtree carries
                                      the effects `eff(aᵢ)` of the HL action it
                                      refines, read as its goal (`Action.effGoal`),
  * lazy ML planning                — each `Tᵢ` planned from the world state current at
                                      refinement,
  * Invariant (ML non-preemption)   — ML subtrees execute atomically (built into
                                      `runBlocks`),
  * Invariant (ML non-interference) — no ML subtree clobbers a sibling's goal,
the whole schedule is applicable and the final state satisfies `g₁ ∧ … ∧ g_k`. -/
theorem check_theorem1 {F : Type} [DecidableEq F]
    (S : Planner F) (hS : S.Sound)                -- planner soundness
    (s₀ : State F)                                 -- initial world state
    (bs : List (Block F))                          -- the ML subtrees, goal-inheriting
    (hlazy : LazyPlanned S s₀ bs)                  -- lazy ML planning
    (hni : MLNonInterference bs) :                 -- ML non-interference
    AllApplicable s₀ bs ∧ runBlocks s₀ bs ⊨ allGoals bs :=
  theorem1_refinement_sound hS s₀ bs hlazy hni

/-! ## (a′) Bridge to the overall high-level goal

The conclusion above is the conjunction of the inherited goals `eff(aᵢ)`. A high-level
subgoal `gⱼ` follows: each of its literals is either established by some high-level
action — hence contained in an inherited goal — or holds initially and is clobbered by
no ML subtree (the subgoal clause of Invariant (ML non-interference)). -/
theorem check_overall_goal {F : Type} [DecidableEq F]
    (S : Planner F) (hS : S.Sound) (s₀ : State F) (bs : List (Block F))
    (hlazy : LazyPlanned S s₀ bs) (hni : MLNonInterference bs)
    (g : Goal F)
    (hlit : ∀ l ∈ g, l ∈ allGoals bs ∨
      (Lit.eval s₀ l = true ∧ ∀ b ∈ bs, b.plan.Preserves [l])) :
    runBlocks s₀ bs ⊨ g :=
  hl_goal_achieved (theorem1_refinement_sound hS s₀ bs hlazy hni).2 hlit

/-! ## (b) ML non-interference is necessary

Witness: the plate/beam countermodel. Initial non-interference holds for the HL
subproblems (`initialNonInterference_holds`), the planners behave soundly
(`schedule_achieves`), goal inheritance holds by construction — yet the execution
misses `g₁ ∧ g₂`, because a mid-level action clobbers a sibling's goal. -/
theorem check_ml_noninterference_necessary :
    InitialNonInterference
      [(Counterexamples.g₁, Counterexamples.πHL₁),
       (Counterexamples.g₂, Counterexamples.πHL₂)] ∧      -- HL non-interference holds
    (AllApplicable Counterexamples.s₀ Counterexamples.schedule ∧
     Achieves Counterexamples.s₀ Counterexamples.schedule) ∧ -- soundness + inheritance
    ¬ (runBlocks Counterexamples.s₀ Counterexamples.schedule
        ⊨ allGoals Counterexamples.schedule) :=            -- yet the goal fails
  ⟨Counterexamples.initialNonInterference_holds,
   Counterexamples.schedule_achieves,
   Counterexamples.initial_noninterference_insufficient.1⟩

/-! ## (c) Axiom audit — `sorryAx` here would mean something was assumed, not proved -/

#print axioms check_theorem1
#print axioms check_overall_goal
#print axioms check_ml_noninterference_necessary

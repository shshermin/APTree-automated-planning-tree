/-
# Correctness under dynamic node insertion (Theorem: Refinement soundness)

The execution of the refined ML subtrees is modelled as a sequence of *blocks* — one
per refined high-level action — each executed atomically (Invariant *ML
non-preemption*). The theorem needs two hypotheses:

* `Achieves` — each block reaches its own inherited goal. This is *proved* from
  planner soundness plus lazy planning (`lazyPlanned_achieves`).
* `TailPreserves` — no later block clobbers an earlier block's goal. This is the
  Invariant *ML non-interference* of the paper; `Counterexamples.lean` shows it is
  not implied by the initial (high-level) non-interference invariant and hence must
  be assumed separately.

Given both, the theorem holds (`theorem1_refinement_sound`), and notably the *order*
of the blocks is irrelevant — the ordering matters only for establishing the
hypotheses.
-/

import APTree.Interference

namespace APTree

universe u

variable {F : Type u} [DecidableEq F]

/-! ## Blocks: mid-level subtrees as executed -/

/-- One mid-level subtree `Tᵢ` as it is actually executed: the goal it inherited from
the high-level action it refines — the effects of that action, read as a goal —
together with the ML plan generated for it.

**Invariant (Goal inheritance)** — `goal(Fᵢ) = eff(aᵢ)` — is definitional here: a
block *is* a goal together with the plan generated for that goal, so no ML subtree can
be executed under a goal other than the inherited one. `Block.Inherits` names the
invariant explicitly. -/
structure Block (F : Type u) where
  /-- `goal(Fᵢ) = eff(aᵢ)` (goal inheritance). -/
  goal : Goal F
  /-- The plan of the ML subtree `Tᵢ`. -/
  plan : Plan F

/-- **Invariant (Goal inheritance)**: `goal(Fᵢ) = eff(aᵢ)` — the block refining
high-level action `aᵢ` carries the effects of `aᵢ`, read as a goal
(`Action.effGoal`). -/
def Block.Inherits (b : Block F) (a : Action F) : Prop := b.goal = a.effGoal

/-- The state reached by executing a schedule of blocks, each atomically. -/
def runBlocks (s : State F) : List (Block F) → State F
  | [] => s
  | b :: bs => runBlocks (Plan.run s b.plan) bs

/-- The overall goal: the conjunction of all block goals (`g₁ ∧ … ∧ g_k`). -/
def allGoals : List (Block F) → Goal F
  | [] => []
  | b :: bs => b.goal ++ allGoals bs

/-- Each block reaches its own inherited goal from the state at which it starts. -/
def Achieves (s : State F) : List (Block F) → Prop
  | [] => True
  | b :: bs => (Plan.run s b.plan ⊨ b.goal) ∧ Achieves (Plan.run s b.plan) bs

/-- Every block is applicable at the point where it is scheduled. -/
def AllApplicable (s : State F) : List (Block F) → Prop
  | [] => True
  | b :: bs => Plan.applicable s b.plan ∧ AllApplicable (Plan.run s b.plan) bs

/-- **Invariant (ML non-interference).** Every block preserves the goals of all blocks
scheduled before it: no action of a later ML subtree falsifies a literal of an earlier
subtree's goal. -/
def TailPreserves : List (Block F) → Prop
  | [] => True
  | b :: bs => (∀ b' ∈ bs, b'.plan.Preserves b.goal) ∧ TailPreserves bs

/-! ## Goal persistence across a schedule -/

theorem Models.runBlocks {s : State F} {g : Goal F} {bs : List (Block F)}
    (h : s ⊨ g) (hp : ∀ b ∈ bs, b.plan.Preserves g) : runBlocks s bs ⊨ g := by
  induction bs generalizing s with
  | nil => simpa [APTree.runBlocks] using h
  | cons b bs ih =>
    refine ih (Models.run h (hp b (List.mem_cons_self ..))) ?_
    exact fun b' hb' => hp b' (List.mem_cons_of_mem _ hb')

/-! ## The composition theorem -/

/-- **Sequential composition.** If every block reaches its own goal and no later block
clobbers an earlier goal, then the final state satisfies the conjunction of all goals.

This is the mathematical core of the refinement-soundness theorem. Note what it does
*not* mention: the order of the blocks, the tree structure, tick semantics, or the
high-level plans. Once `Achieves` and `TailPreserves` hold, the conclusion is
order-independent. -/
theorem composition_sound (s : State F) (bs : List (Block F))
    (ha : Achieves s bs) (hp : TailPreserves bs) :
    runBlocks s bs ⊨ allGoals bs := by
  induction bs generalizing s with
  | nil => simpa [APTree.runBlocks, allGoals] using models_nil
  | cons b bs ih =>
    obtain ⟨hgoal, harest⟩ := ha
    obtain ⟨hpre, hprest⟩ := hp
    have hlater : runBlocks (Plan.run s b.plan) bs ⊨ b.goal :=
      Models.runBlocks hgoal hpre
    have hrest : runBlocks (Plan.run s b.plan) bs ⊨ allGoals bs :=
      ih (Plan.run s b.plan) harest hprest
    simpa [APTree.runBlocks, allGoals, models_append] using ⟨hlater, hrest⟩

/-! ## Lazy mid-level planning discharges `Achieves` -/

/-- Mid-level planning problems "are instantiated lazily, taking as their initial state
the world state at the time of refinement or replanning": every block's plan is
produced by the planner from the state current when the block is reached. -/
inductive LazyPlanned (S : Planner F) : State F → List (Block F) → Prop
  | nil {s : State F} : LazyPlanned S s []
  | cons {s : State F} {g : Goal F} {π : Plan F} {bs : List (Block F)} :
      S.run s g = some π → LazyPlanned S (Plan.run s π) bs →
      LazyPlanned S s ({ goal := g, plan := π } :: bs)

/-- Planner soundness + lazy planning gives `Achieves`. -/
theorem lazyPlanned_achieves {S : Planner F} (hS : S.Sound) {s : State F}
    {bs : List (Block F)} (h : LazyPlanned S s bs) : Achieves s bs := by
  induction h with
  | nil => exact trivial
  | cons hplan _ ih => exact ⟨(hS _ _ _ hplan).2, ih⟩

/-- Planner soundness + lazy planning also gives applicability of every block. -/
theorem lazyPlanned_applicable {S : Planner F} (hS : S.Sound) {s : State F}
    {bs : List (Block F)} (h : LazyPlanned S s bs) : AllApplicable s bs := by
  induction h with
  | nil => exact trivial
  | cons hplan _ ih => exact ⟨(hS _ _ _ hplan).1, ih⟩

/-! ## The theorem -/

/-- **Theorem (Refinement soundness), machine-checked form.**

Hypotheses: a sound planner (Assumption *Planner soundness*), goal inheritance
(definitional in `Block`), lazy mid-level planning from the current world state,
atomic execution of each mid-level subtree (Invariant *ML non-preemption*, built into
`runBlocks`), and ML non-interference (`TailPreserves`).

Conclusion: the schedule is executable and its final state satisfies the conjunction of
all inherited goals. -/
theorem theorem1_refinement_sound {S : Planner F} (hS : S.Sound) (s₀ : State F)
    (bs : List (Block F)) (hlazy : LazyPlanned S s₀ bs) (hni : TailPreserves bs) :
    AllApplicable s₀ bs ∧ runBlocks s₀ bs ⊨ allGoals bs :=
  ⟨lazyPlanned_applicable hS hlazy,
   composition_sound s₀ bs (lazyPlanned_achieves hS hlazy) hni⟩

/-! ## From inherited goals to the high-level subgoals -/

/-- Bridge to the overall high-level goal `g₁ ∧ … ∧ g_k`. A literal `l` of a subgoal
`gⱼ` is either established by some high-level action of `πⱼ` — then `l` lies in that
action's inherited goal (Invariant *Goal inheritance*) and holds finally by
`theorem1_refinement_sound` — or it holds initially and no ML subtree clobbers it
(the subgoal clause of Invariant *ML non-interference*), so it persists. -/
theorem hl_goal_achieved {s : State F} {bs : List (Block F)} {g : Goal F}
    (hall : runBlocks s bs ⊨ allGoals bs)
    (hlit : ∀ l ∈ g, l ∈ allGoals bs ∨
      (Lit.eval s l = true ∧ ∀ b ∈ bs, b.plan.Preserves [l])) :
    runBlocks s bs ⊨ g := by
  intro l hl
  rcases hlit l hl with hmem | ⟨hinit, hpres⟩
  · exact hall l hmem
  · exact Models.runBlocks (g := [l])
      (fun l' hl' => by rw [List.mem_singleton] at hl'; subst hl'; exact hinit)
      hpres l (List.mem_cons_self ..)

/-! ## Invariant (Initial non-interference) -/

/-- **Invariant (Initial non-interference)**: over the initial high-level subproblems,
given as goal/plan pairs `(gⱼ, πⱼ)`, (1) goals are pairwise non-interfering and (2) no
subproblem's plan interferes with another subproblem's goal.

Both clauses speak only about the *high-level* plans `πⱼ`;
`Counterexamples.initial_noninterference_insufficient` shows why the mid-level
analogue (`MLNonInterference`) must be assumed in addition. -/
def InitialNonInterference (subs : List (Goal F × Plan F)) : Prop :=
  subs.Pairwise fun x y =>
    ¬ (x.1 ▷◁ y.1) ∧ ¬ PlanInterferes y.2 x.1 ∧ ¬ PlanInterferes x.2 y.1

/-! ## Invariant (ML non-interference), by its paper name -/

/-- **Invariant (ML non-interference)**: unlike `InitialNonInterference`, this
quantifies over the *mid-level* plans actually generated during execution (the
`Block`s), not just the initial high-level plans. It is the inherited-goal clause
(clause 2) of the paper's invariant — exactly `TailPreserves`, restated under the
paper's invariant name; the subgoal clause (clause 1) enters as the literal-wise
hypothesis of `hl_goal_achieved`. -/
abbrev MLNonInterference (bs : List (Block F)) : Prop := TailPreserves bs

end APTree

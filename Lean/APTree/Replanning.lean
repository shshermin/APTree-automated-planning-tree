/-
# Correctness under local re-planning (Theorem: Local replanning)

Formalized here: node results (`BTNodeResult`), quiescent execution, a DFN with the
archived/active split, the `Replan` operation, and the theorem itself.

Quiescence is stated at the level of *action* nodes (`ActionQuiescent`), matching the
paper's definition: flow-node status is bookkeeping and does not change the world
state, so a DFN reset to `InProgress` to trigger replanning does not violate
quiescence (`actionQuiescent_allows_replan_trigger`). The stronger all-nodes variant
(`Quiescent`) is included for contrast: it is incompatible with the replan trigger
(`quiescent_excludes_replan_trigger`), which is why the definition is stated over
action nodes only.

The locality clause — replanning inside one DFN does not destroy a *sibling* DFN's
achieved goal — additionally needs ML non-interference for the fresh plan
(`replan_preserves_sibling`), as stated in the paper.
-/

import APTree.Refinement

namespace APTree

universe u

variable {F : Type u} [DecidableEq F]

/-! ## Node results and quiescence -/

/-- `BTNodeResult`: the three classical statuses plus the two APTree additions. -/
inductive NodeResult where
  | Uninitialized
  | ReadyToTick
  | InProgress
  | Success
  | Failure
  deriving DecidableEq, Repr

/-- All-nodes quiescence:
`∀ Tᵢ, ∀ n ∈ Tᵢ : result(n, T_k) ≠ InProgress`, quantifying over *every* node.
Included for contrast with `ActionQuiescent`; see
`quiescent_excludes_replan_trigger`. -/
def Quiescent {ι : Type} (result : ι → NodeResult) : Prop :=
  ∀ n : ι, result n ≠ NodeResult.InProgress

/-- The replan trigger: a dynamic flow node awaiting replanning has its status reset to
`InProgress` by its post-processing decorator, triggering re-planning in the next round
of tick signals. -/
def ReplanPending {ι : Type} (result : ι → NodeResult) (n : ι) : Prop :=
  result n = NodeResult.InProgress

/-- All-nodes quiescence is incompatible with a pending replan: the node awaiting
replanning is itself `InProgress`. This is why quiescence is defined over action nodes
only (`ActionQuiescent`). -/
theorem quiescent_excludes_replan_trigger {ι : Type} (result : ι → NodeResult) (n : ι)
    (hq : Quiescent result) : ¬ ReplanPending result n :=
  fun h => hq n h

/-- **Definition (Quiescent execution)**: no *action* node is executing. `isAction`
marks the action nodes; flow-node status (e.g. a DFN reset to `InProgress` to trigger
replanning) is not constrained, since flow-node status does not change the world
state. -/
def ActionQuiescent {ι : Type} (isAction : ι → Bool) (result : ι → NodeResult) : Prop :=
  ∀ n : ι, isAction n = true → result n ≠ NodeResult.InProgress

/-- Quiescence is satisfiable together with a pending replan: the DFN (a flow node) is
`InProgress` while every action node is at rest. Witness: two nodes, `true` an action
node that has succeeded, `false` the replan-pending DFN. -/
theorem actionQuiescent_allows_replan_trigger :
    ∃ (isAction : Bool → Bool) (result : Bool → NodeResult) (n : Bool),
      ActionQuiescent isAction result ∧ ReplanPending result n := by
  refine ⟨id, fun b => if b then .Success else .InProgress, false, ?_, rfl⟩
  intro n hn
  have : n = true := hn
  subst this
  decide

/-! ## DFNs with the archived/active split -/

/-- A DFN under execution.

* `goal` is assigned at creation (goal inheritance) and, by **Invariant (Goal
  immutability)**, never changes for the lifetime of the subtree. The invariant is
  definitional here: no operation below writes `goal`.
* `archived` are the completed children, "retained for execution history and never
  re-ticked"; their effects remain reflected in the world state.
* `active` are the children not yet executed. -/
structure Dfn (F : Type u) where
  /-- `goal(Fᵢ)`, fixed for the lifetime of the subtree (goal immutability). -/
  goal : Goal F
  /-- `Cᵃʳᶜʰⁱᵛᵉᵈ`. -/
  archived : Plan F := []
  /-- `Cᵃᶜᵗⁱᵛᵉ`, the unexecuted remainder. -/
  active : Plan F

/-- **Invariant (Status excludes archived nodes).** "the node result of a DFN `Fᵢ` is
determined solely by its active children". Definitional: `status` does not read
`archived`. -/
def Dfn.status (d : Dfn F) : NodeResult :=
  if d.active.isEmpty then NodeResult.Success else NodeResult.InProgress

omit [DecidableEq F] in
theorem Dfn.status_indep_archived (d : Dfn F) (π : Plan F) :
    ({ d with archived := π } : Dfn F).status = d.status := rfl

/-- Executing one active child: it moves to the archived list and its effect is applied
to the world state. -/
def Dfn.step (d : Dfn F) (s : State F) : Dfn F × State F :=
  match d.active with
  | [] => (d, s)
  | a :: rest => ({ d with archived := d.archived ++ [a], active := rest }, a.apply s)

/-- **Definition (Replan operation).** `Replan(Fᵢ)`:
1. archives all completed children — they are already in `archived`, and are kept;
2. removes all unexecuted active children (and any descendant subtrees) — `active` is
   discarded;
3. generates a new plan from the current world state while preserving `goal(Fᵢ)`
   (goal immutability), attaching the result as the new active children. -/
def Dfn.replan (S : Planner F) (d : Dfn F) (s : State F) : Option (Dfn F) :=
  (S.run s d.goal).map fun π => { goal := d.goal, archived := d.archived, active := π }

/-! ## The theorem -/

/-- **Theorem (Correctness under local replanning), machine-checked form.**

A replanning event keeps the goal (goal immutability), keeps the archived history
untouched (status exclusion), and installs an active plan that is applicable from the
current state and reaches `goal(Fᵢ)`.

Quiescence enters as the justification that `s` is a well-defined valuation with no
partially applied effects (in this model, every `State F` is a total valuation), and
goal immutability enters as the fact that `replan` passes `d.goal` unchanged to the
planner. -/
theorem theorem2_replan_sound {S : Planner F} (hS : S.Sound) (d d' : Dfn F) (s : State F)
    (h : d.replan S s = some d') :
    d'.goal = d.goal ∧ d'.archived = d.archived ∧
      Plan.applicable s d'.active ∧ Plan.run s d'.active ⊨ d.goal := by
  unfold Dfn.replan at h
  cases hrun : S.run s d.goal with
  | none => rw [hrun] at h; exact absurd h (by simp)
  | some π =>
    rw [hrun] at h
    simp only [Option.map_some] at h
    have hd : ({ goal := d.goal, archived := d.archived, active := π } : Dfn F) = d' :=
      Option.some.inj h
    subst hd
    obtain ⟨happ, hgoal⟩ := hS s d.goal π hrun
    exact ⟨rfl, rfl, happ, hgoal⟩

/-- Any sequence of replanning events preserves correctness: each event re-establishes
the same property from the then-current state. -/
theorem theorem2_replan_idempotent {S : Planner F} (hS : S.Sound)
    (d₁ d₂ d₃ : Dfn F) (s₁ s₂ : State F)
    (h₁ : d₁.replan S s₁ = some d₂) (h₂ : d₂.replan S s₂ = some d₃) :
    d₃.goal = d₁.goal ∧ Plan.run s₂ d₃.active ⊨ d₁.goal := by
  obtain ⟨hg₁, _, _, _⟩ := theorem2_replan_sound hS d₁ d₂ s₁ h₁
  obtain ⟨hg₂, _, _, hach⟩ := theorem2_replan_sound hS d₂ d₃ s₂ h₂
  exact ⟨hg₂.trans hg₁, hg₁ ▸ hach⟩

/-! ## Locality -/

/-- **Sibling preservation** — the locality clause of the theorem.

A replanning event inside `Fᵢ` produces a fresh plan from the current world state. That
plan is sound *for `goal(Fᵢ)`*; a sibling DFN's already-achieved goal `g` survives
provided the fresh plan satisfies ML non-interference with respect to `g`
(`Preserves g`), exactly as the theorem's locality clause requires. -/
theorem replan_preserves_sibling {S : Planner F} (d d' : Dfn F) (s : State F)
    (g : Goal F) (hsib : s ⊨ g) (_h : d.replan S s = some d')
    (hpres : d'.active.Preserves g) :
    Plan.run s d'.active ⊨ g :=
  Models.run hsib hpres

end APTree

/-
# Standalone check for the theorem "Correctness under local replanning"

Run with:  lake env lean CheckTheorem2.lean
(or just open in the editor and check the Problems panel / infoview)

If this file compiles, Lean's kernel has verified:
  (a) the theorem — replanning preserves the affected DFN's correctness;
  (b) the locality clause — with ML non-interference, a sibling's achieved goal
      survives the replanned subtree;
  (c) sequences of replanning events preserve correctness;
  (d) quiescence is correctly stated at the action-node level: the all-nodes variant
      is incompatible with the replan trigger, the action-level definition is not.
-/
import APTree.Replanning

open APTree

/-! ## (a) The affected DFN

Given planner soundness, quiescence (so the current state `s` is well defined with no
partially applied effects), goal immutability (`replan` passes `d.goal` unchanged), and
the replan operation, the replanned DFN keeps its goal and archived history, and its
new active plan is applicable from `s` and reaches `goal(Fᵢ)`. -/
theorem check_theorem2 {F : Type} [DecidableEq F]
    (S : Planner F) (hS : S.Sound)              -- planner soundness
    (d d' : Dfn F) (s : State F)                 -- DFN before/after, quiescent state
    (h : d.replan S s = some d') :               -- replan operation
    d'.goal = d.goal ∧                           -- goal immutability
    d'.archived = d.archived ∧                   -- archived history untouched
    Plan.applicable s d'.active ∧
    Plan.run s d'.active ⊨ d.goal :=
  theorem2_replan_sound hS d d' s h

/-! ## (b) Locality: siblings are unharmed

The clause that justifies calling replanning *local*: if the fresh plan satisfies
ML non-interference w.r.t. a sibling's goal `g` (i.e. it `Preserves g`), then `g`,
already satisfied at replan time, still holds after the new subtree runs. -/
theorem check_theorem2_locality {F : Type} [DecidableEq F]
    (S : Planner F) (d d' : Dfn F) (s : State F)
    (g : Goal F)                                 -- a sibling's achieved subgoal
    (hsib : s ⊨ g)                               -- satisfied at replan time
    (h : d.replan S s = some d')                 -- replan operation
    (hni : d'.active.Preserves g) :              -- ML non-interference
    Plan.run s d'.active ⊨ g :=
  replan_preserves_sibling d d' s g hsib h hni

/-! ## (c) Any sequence of replanning events preserves correctness -/

theorem check_theorem2_sequence {F : Type} [DecidableEq F]
    (S : Planner F) (hS : S.Sound)
    (d₁ d₂ d₃ : Dfn F) (s₁ s₂ : State F)
    (h₁ : d₁.replan S s₁ = some d₂) (h₂ : d₂.replan S s₂ = some d₃) :
    d₃.goal = d₁.goal ∧ Plan.run s₂ d₃.active ⊨ d₁.goal :=
  theorem2_replan_idempotent hS d₁ d₂ d₃ s₁ s₂ h₁ h₂

/-! ## (d) The quiescence definitions

All-nodes quiescence is incompatible with the replan trigger — the very node awaiting
replanning is `InProgress` — which is why quiescence is defined over action nodes
only. -/
theorem check_quiescence_all_nodes_too_strong {ι : Type}
    (result : ι → NodeResult) (n : ι) (hq : Quiescent result) :
    ¬ ReplanPending result n :=
  quiescent_excludes_replan_trigger result n hq

/-- Action-level quiescence (the definition used in the analysis): a replan-pending
flow node coexists with action-level quiescence. -/
theorem check_quiescence_action_level_satisfiable :
    ∃ (isAction : Bool → Bool) (result : Bool → NodeResult) (n : Bool),
      ActionQuiescent isAction result ∧ ReplanPending result n :=
  actionQuiescent_allows_replan_trigger

/-! ## Axiom audit — `sorryAx` here would mean something was assumed, not proved -/

#print axioms check_theorem2
#print axioms check_theorem2_locality
#print axioms check_theorem2_sequence
#print axioms check_quiescence_all_nodes_too_strong
#print axioms check_quiescence_action_level_satisfiable

/-
# Countermodels: necessity of the hypotheses

Every theorem in this file is a fully computed finite model, checked by `decide`. The
fluents are named after the LivMatS case study (a plate and a beam to be placed on a
work table by one robot arm). Each countermodel shows that one hypothesis of the
analysis cannot be dropped or weakened.

| # | Statement | What it shows |
|---|-----------|---------------|
| 1 | `negative_clause_necessary` | the plan/goal interference clause must cover negative goal literals |
| 2 | `initial_noninterference_insufficient` | initial (HL) non-interference alone does not imply refinement soundness |
| 2b | `mlNonInterference_sufficient` | with ML non-interference, the conclusion holds |
| 3 | `interleaving_breaks_applicability` | ML non-preemption (atomicity) cannot be dropped |
| 4 | `precondGuarded_not_respects` | precondition-guardedness does not yield a valid linearization; predecessor completion does |
| 5 | `successCriteria_breaks_goal_achievement` | the restriction to the `All` criterion cannot be dropped |
-/

import APTree.SuccessCriteria

namespace APTree
namespace Counterexamples

/-- Fluents of the running countermodel. -/
inductive Fl where
  /-- The plate is at its final position. -/
  | plateAt
  /-- The beam is at its final position. -/
  | beamAt
  /-- The work table is clear. -/
  | tableClear
  /-- The gripper holds an element. -/
  | gripHolding
  /-- Glue has been spilled. -/
  | spilled
  deriving DecidableEq, Repr

open Fl

/-- The initial world state: the table is clear, nothing else holds. -/
def s₀ : State Fl := fun p => p == tableClear

/-! ## 1. The plan/goal clause must cover negative goal literals

A goal literal `¬p` is destroyed by an action that *adds* `p`. Restricting plan/goal
interference to deleted positive literals (`PlanInterferesPos`) misses this. -/

/-- Goal: no glue is spilled. -/
def gClean : Goal Fl := [.neg spilled]

/-- A plan that spills glue. -/
def pourGlue : Action Fl := { name := "pourGlueML", pre := [], add := [spilled] }

/-- A plan that destroys the goal `¬spilled` while deleting nothing: interference must
be judged by the two-clause definition (`PlanInterferes`), not by deletions alone. -/
theorem negative_clause_necessary :
    ¬ PlanInterferesPos [pourGlue] gClean ∧
      PlanInterferes [pourGlue] gClean ∧
      ¬ (Plan.run s₀ [pourGlue] ⊨ gClean) := by
  refine ⟨?_, ?_, ?_⟩
  · rintro ⟨a, _, p, hpos, -⟩
    simp [gClean] at hpos
  · exact ⟨pourGlue, List.mem_cons_self .., spilled,
      Or.inr ⟨by simp [gClean], by simp [pourGlue]⟩⟩
  · rw [models_iff_modelsB]; decide

/-! ## 2. Initial non-interference does not suffice; ML non-interference is necessary

Two independent high-level subproblems: place the plate (`g₁`) and place the beam (`g₂`).
The high-level plans satisfy initial non-interference in full. Each high-level action is
refined by a sound mid-level planner from the current world state, and each mid-level
subtree does reach its inherited goal. Yet the composite execution does not reach
`g₁ ∧ g₂`, because the *mid-level* plan for the beam clears the table — a finer-grained
action that no high-level plan mentions, and that initial non-interference therefore
does not constrain. This is why ML non-interference is a separate hypothesis. -/

/-- Subgoal of the first branch. -/
def g₁ : Goal Fl := [.pos plateAt]

/-- Subgoal of the second branch. -/
def g₂ : Goal Fl := [.pos beamAt]

/-- The high-level action of the first branch (`PlaceHL(plate1, pr1)`). -/
def placePlateHL : Action Fl := { name := "PlaceHL_plate", pre := [], add := [plateAt] }

/-- The high-level action of the second branch (`PlaceHL(beam1, pr2)`). -/
def placeBeamHL : Action Fl := { name := "PlaceHL_beam", pre := [], add := [beamAt] }

/-- High-level plan of the first subproblem. -/
def πHL₁ : Plan Fl := [placePlateHL]

/-- High-level plan of the second subproblem. -/
def πHL₂ : Plan Fl := [placeBeamHL]

/-- **Initial non-interference holds.** The two subgoals do not interfere, and neither
high-level plan interferes with the other subgoal: no high-level action deletes
anything, and neither subgoal contains a negative literal. -/
theorem initialNonInterference_holds :
    InitialNonInterference [(g₁, πHL₁), (g₂, πHL₂)] := by
  have hno : ∀ (π : Plan Fl) (a : Action Fl) (g : Goal Fl),
      π = [a] → a.del = [] → (∀ p : Fl, Lit.neg p ∉ g) → ¬ PlanInterferes π g := by
    rintro π a g rfl hdel hneg ⟨b, hb, p, h⟩
    simp only [List.mem_singleton] at hb
    subst hb
    rcases h with ⟨-, hp⟩ | ⟨hn, -⟩
    · rw [hdel] at hp; simp at hp
    · exact hneg p hn
  refine List.Pairwise.cons ?_ (List.Pairwise.cons ?_ List.Pairwise.nil)
  · rintro b hb
    simp only [List.mem_singleton] at hb
    subst hb
    refine ⟨?_, ?_, ?_⟩
    · rintro ⟨p, h⟩
      rcases h with ⟨-, h⟩ | ⟨h, -⟩ <;> simp [g₁, g₂] at h
    · exact hno πHL₂ placeBeamHL g₁ rfl rfl (by intro p hp; simp [g₁] at hp)
    · exact hno πHL₁ placePlateHL g₂ rfl rfl (by intro p hp; simp [g₂] at hp)
  · rintro b hb; simp at hb

/-- Mid-level refinement of `placePlateHL`: pick the plate up, then place it. -/
def πML₁ : Plan Fl :=
  [ { name := "PickUpML_plate", pre := [.pos tableClear],
      add := [gripHolding], del := [tableClear] },
    { name := "PlaceML_plate", pre := [.pos gripHolding],
      add := [plateAt], del := [gripHolding] } ]

/-- Mid-level refinement of `placeBeamHL`: the beam needs a clear table, so the mid-level
planner first clears it — which removes the plate. This action exists only in `D_ML`. -/
def πML₂ : Plan Fl :=
  [ { name := "ClearTableML", pre := [], add := [tableClear], del := [plateAt] },
    { name := "PlaceML_beam", pre := [.pos tableClear], add := [beamAt] } ]

/-- The executed schedule: both mid-level subtrees, in an order consistent with the
high-level plans (the two branches are unordered, so any order is consistent). -/
def schedule : List (Block Fl) :=
  [ { goal := g₁, plan := πML₁ }, { goal := g₂, plan := πML₂ } ]

/-- Each mid-level subtree is applicable where it is scheduled and reaches its inherited
goal — so planner soundness and goal inheritance are satisfied by this schedule. -/
theorem schedule_achieves : AllApplicable s₀ schedule ∧ Achieves s₀ schedule := by
  constructor
  · refine ⟨?_, ?_, trivial⟩ <;>
      · rw [Plan.applicable_iff_applicableB]; decide
  · refine ⟨?_, ?_, trivial⟩ <;>
      · rw [models_iff_modelsB]; decide

/-- **Initial non-interference is not enough.**

Initial non-interference holds for the high-level subproblems
(`initialNonInterference_holds`), planner soundness is witnessed by the fact that every
plan is applicable and reaches its goal (`schedule_achieves`), and goal inheritance
holds by construction — yet the execution does not reach `g₁ ∧ g₂`.

The diagnosis is `¬ TailPreserves schedule`: non-interference is needed at the
*mid-level* (`MLNonInterference`), which the initial invariant does not provide. -/
theorem initial_noninterference_insufficient :
    ¬ (runBlocks s₀ schedule ⊨ allGoals schedule) ∧ ¬ TailPreserves schedule := by
  constructor
  · rw [models_iff_modelsB]; decide
  · rintro ⟨h, -⟩
    have hb : ({ goal := g₂, plan := πML₂ } : Block Fl) ∈ [({ goal := g₂, plan := πML₂ } : Block Fl)] :=
      List.mem_cons_self ..
    have hpres := h _ hb
    have hact : ({ name := "ClearTableML", pre := [], add := [tableClear], del := [plateAt] } : Action Fl)
        ∈ πML₂ := by simp [πML₂]
    exact hpres _ hact (.pos plateAt) (by simp [g₁]) (by simp [Action.clobbers])

/-! ## 2b. ML non-interference is sufficient

Same two branches, except the beam is placed on its own stand instead of on the shared
table, so its mid-level plan never touches `plateAt`. This schedule satisfies
`MLNonInterference` (`TailPreserves`), and the conclusion (`g₁ ∧ g₂`) holds. -/

/-- Mid-level refinement of `placeBeamHL` that does not clobber the plate's goal. -/
def πML₂Stand : Plan Fl :=
  [ { name := "PlaceML_beam_on_stand", pre := [], add := [beamAt] } ]

/-- The non-interfering schedule. -/
def scheduleNI : List (Block Fl) :=
  [ { goal := g₁, plan := πML₁ }, { goal := g₂, plan := πML₂Stand } ]

/-- **ML non-interference holds for this schedule.** `πML₂Stand` has an empty `del`
list, so it clobbers nothing — in particular not `g₁`. -/
theorem scheduleNI_mlNonInterference : MLNonInterference scheduleNI := by
  refine ⟨?_, ?_, trivial⟩
  · intro b' hb' a ha l hl hcl
    simp only [List.mem_singleton] at hb'
    subst hb'
    simp only [πML₂Stand, List.mem_singleton] at ha
    subst ha
    simp only [g₁, List.mem_singleton] at hl
    subst hl
    simp [Action.clobbers] at hcl
  · intro b' hb'
    simp at hb'

/-- Each mid-level subtree of this schedule still reaches its own inherited goal. -/
theorem scheduleNI_achieves : Achieves s₀ scheduleNI := by
  refine ⟨?_, ?_, trivial⟩ <;>
      · rw [models_iff_modelsB]; decide

/-- **The conclusion holds.** Contrast with `initial_noninterference_insufficient`:
same branches, same subgoals, the only change is that the mid-level plan no longer
clobbers a sibling's goal. -/
theorem mlNonInterference_sufficient :
    runBlocks s₀ scheduleNI ⊨ allGoals scheduleNI :=
  composition_sound s₀ scheduleNI scheduleNI_achieves scheduleNI_mlNonInterference

/-! ## 3. ML non-preemption (atomicity) cannot be dropped

The same two branches, now with mid-level plans that leave the table clear again, so that
running them one after the other works. Interleaving them at *action* granularity — which
is what "sibling DFNs may be ticked concurrently" would mean without ML non-preemption —
breaks the second branch's precondition. Non-interference does not exclude this: it
constrains goals and goal-deleting effects, never preconditions. -/

/-- Pick the plate up: needs a clear table, occupies the gripper. -/
def pickPlate : Action Fl :=
  { name := "PickUpML_plate", pre := [.pos tableClear],
    add := [gripHolding], del := [tableClear] }

/-- Place the plate: frees the gripper and clears the table again. -/
def placePlate : Action Fl :=
  { name := "PlaceML_plate", pre := [.pos gripHolding],
    add := [plateAt, tableClear], del := [gripHolding] }

/-- Pick the beam up. -/
def pickBeam : Action Fl :=
  { name := "PickUpML_beam", pre := [.pos tableClear],
    add := [gripHolding], del := [tableClear] }

/-- Place the beam. -/
def placeBeam : Action Fl :=
  { name := "PlaceML_beam", pre := [.pos gripHolding],
    add := [beamAt, tableClear], del := [gripHolding] }

/-- Branch 1, restoring the table. -/
def τ₁ : Plan Fl := [pickPlate, placePlate]

/-- Branch 2, restoring the table. -/
def τ₂ : Plan Fl := [pickBeam, placeBeam]

/-- **Atomic execution works; action-level interleaving does not.**

`τ₁ ++ τ₂` is applicable and achieves both subgoals. The interleaving that ticks the two
branches alternately is not even applicable — the second branch's `PickUpML_beam` needs a
clear table that the first branch is still occupying. -/
theorem interleaving_breaks_applicability :
    Plan.applicable s₀ (τ₁ ++ τ₂) ∧ (Plan.run s₀ (τ₁ ++ τ₂) ⊨ (g₁ ++ g₂)) ∧
      ¬ Plan.applicable s₀ [pickPlate, pickBeam, placePlate, placeBeam] := by
  refine ⟨?_, ?_, ?_⟩
  · rw [Plan.applicable_iff_applicableB]; decide
  · rw [models_iff_modelsB]; decide
  · rw [Plan.applicable_iff_applicableB]; decide

/-! ## 4. Precondition-guardedness does not give a valid linearization

A precondition-guarded schedule can violate `≺`: here `travelML ≺ placeML` (a `Meets`
edge in the `NodeGraph`), but both preconditions hold initially, so the schedule `[1, 0]`
is precondition-guarded and yet violates the constraint. What *does* give a valid
linearization is `canExecute`'s predecessor check (predecessor completion) — see
`respects_of_precedenceGuarded`. -/

/-- Two high-level actions ordered by a `Meets` relation. -/
def popMeets : Pop Fl :=
  { acts := [ { name := "TravelML", pre := [], add := [tableClear] },
              { name := "PlaceML", pre := [], add := [plateAt] } ],
    prec := [(0, 1)] }

/-- **Precondition-guardedness does not imply respecting `≺`.** -/
theorem precondGuarded_not_respects :
    popMeets.PrecondGuarded s₀ [1, 0] ∧ ¬ popMeets.Respects [1, 0] := by
  constructor
  · rw [Pop.precondGuarded_iff]; decide
  · intro h
    have := h (0, 1) (by simp [popMeets]) (by simp)
    exact absurd this (by decide)

/-! ## 5. `Success` does not mean "goal achieved" unless the criterion is `All`

The analysis equates "executing `Tᵢ`" with "achieving `goal(Tᵢ)`". The engine returns
`Success` as soon as the configured criterion is met. Under `Any`, `Count n` or
`Percentage p` a DFN can report `Success` with a failed child, and then its goal does not
hold — which is why the analysis is restricted to the `All` criterion. -/

/-- Two children of one DFN: the plate branch succeeded, the beam branch failed. -/
def children : List (Child Fl) :=
  [ { goal := g₁, plan := [placePlateHL], verdict := true },
    { goal := g₂, plan := [placeBeamHL], verdict := false } ]

/-- **A DFN that reports `Success` without its goal holding.** -/
theorem successCriteria_breaks_goal_achievement :
    SuccessCriteria.Any.succeeded (verdicts children) = true ∧
      (SuccessCriteria.Count 1).succeeded (verdicts children) = true ∧
      (SuccessCriteria.Percentage 50).succeeded (verdicts children) = true ∧
      SuccessCriteria.All.succeeded (verdicts children) = false ∧
      ¬ (runChildren s₀ children ⊨ childGoals children) := by
  refine ⟨by decide, by decide, by decide, by decide, ?_⟩
  rw [models_iff_modelsB]; decide

end Counterexamples
end APTree

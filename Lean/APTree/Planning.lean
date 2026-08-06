/-
# Planning substrate

Formalization of the *Preliminaries* of Appendix B (Formal Correctness Analysis) of

  S. Sherkat, T. Wortmann, A. Wortmann, "APTree: Extended Behavior Trees for Adaptive
  and Scalable Automated Robotic Task Planning in Construction".

Appendix B fixes a planning domain `D = ⟨P, A⟩` with fluents `P` and actions carrying
`pre(a)` and `eff(a)`, and a planning problem `Π = ⟨D, s₀, g⟩`. We model this in the
standard STRIPS style: fluents are an arbitrary type with decidable equality, a world
state is a total valuation of the fluents, and goals and preconditions are finite
conjunctions of literals.

Design decisions:

* **Total valuations.** `State F := F → Bool`. This makes the frame problem explicit
  (an action changes exactly its add/del fluents) and avoids the closed-world
  subtleties of representing states as sets of positive fluents.
* **`eff(a)` split into `add`/`del`.** Appendix B writes `eff(a)` as a pair of an
  add-list and a delete-list; we adopt this directly.
* **One fluent space for HL and ML.** Appendix B posits a high-level domain `D_HL`
  and mid-level domains `D_ML`, where `D_ML` "refines its corresponding `D_HL` into
  finer-grained actions and fluents". We work in a single fluent space in which ML
  actions may touch fluents that no HL action mentions; this is the weakest reading
  of the refinement relation, and it matches the fact that ML plans are executed
  against the shared world state.
-/

namespace APTree

universe u

variable {F : Type u}

/-! ## Literals, states, goals -/

/-- A literal over a fluent type `F`. -/
inductive Lit (F : Type u) where
  | pos : F → Lit F
  | neg : F → Lit F
  deriving DecidableEq, Repr

/-- A world state: a total valuation of the fluents. -/
def State (F : Type u) : Type u := F → Bool

/-- A goal (also used for preconditions): a finite conjunction of literals. -/
abbrev Goal (F : Type u) : Type u := List (Lit F)

/-- Truth value of a literal in a state. -/
def Lit.eval (s : State F) : Lit F → Bool
  | .pos p => s p
  | .neg p => !(s p)

/-- `s ⊨ g`: the state `s` satisfies every literal of the goal `g`. -/
def Models (s : State F) (g : Goal F) : Prop := ∀ l ∈ g, Lit.eval s l = true

@[inherit_doc] scoped infix:50 " ⊨ " => Models

/-- Boolean version of `Models`, for computation and for `decide`. -/
def modelsB (s : State F) (g : Goal F) : Bool := g.all (Lit.eval s)

theorem models_iff_modelsB {s : State F} {g : Goal F} : s ⊨ g ↔ modelsB s g = true := by
  simp [Models, modelsB, List.all_eq_true]

theorem models_nil {s : State F} : s ⊨ ([] : Goal F) := by
  intro l hl; cases hl

theorem models_append {s : State F} {g₁ g₂ : Goal F} :
    s ⊨ (g₁ ++ g₂) ↔ (s ⊨ g₁ ∧ s ⊨ g₂) := by
  constructor
  · intro h
    exact ⟨fun l hl => h l (List.mem_append_left _ hl),
           fun l hl => h l (List.mem_append_right _ hl)⟩
  · intro ⟨h₁, h₂⟩ l hl
    rcases List.mem_append.1 hl with h | h
    · exact h₁ l h
    · exact h₂ l h

/-! ## Actions -/

/-- An action of a planning domain: a precondition and an effect split into an
add-list and a delete-list. -/
structure Action (F : Type u) where
  /-- A label, used only for readability of the examples. -/
  name : String := ""
  /-- `pre(a)`. -/
  pre : Goal F
  /-- The positive part of `eff(a)`. -/
  add : List F := []
  /-- The negative part of `eff(a)`. -/
  del : List F := []

/-- Applying an action to a state. Add-effects take precedence over delete-effects
(the choice is immaterial for well-formed actions with `add ∩ del = ∅`, but it must be
fixed to make the semantics total). -/
def Action.apply [DecidableEq F] (a : Action F) (s : State F) : State F :=
  fun p => if p ∈ a.add then true else if p ∈ a.del then false else s p

/-- An action is enabled in a state when its precondition holds. -/
def Action.enabled (a : Action F) (s : State F) : Prop := s ⊨ a.pre

/-- `eff(a)` read as a goal: the add-list literals hold positively, the delete-list
literals negatively. **Invariant (Goal inheritance)** sets `goal(Fᵢ) = eff(aᵢ)` in
this reading. -/
def Action.effGoal (a : Action F) : Goal F := a.add.map Lit.pos ++ a.del.map Lit.neg

/-- Applying an action reaches its own effects, read as a goal (for well-formed
actions, whose add- and delete-lists are disjoint). -/
theorem Action.apply_models_effGoal [DecidableEq F] {a : Action F}
    (hwf : ∀ p ∈ a.del, p ∉ a.add) (s : State F) : a.apply s ⊨ a.effGoal := by
  intro l hl
  rcases List.mem_append.1 hl with hp | hn
  · obtain ⟨p, hmem, rfl⟩ := List.mem_map.1 hp
    simp [Lit.eval, Action.apply, hmem]
  · obtain ⟨p, hmem, rfl⟩ := List.mem_map.1 hn
    simp [Lit.eval, Action.apply, hmem, hwf p hmem]

/-! ## Plans -/

/-- A plan is a finite sequence of actions, as in Appendix B's `S(Π) = π`. -/
abbrev Plan (F : Type u) : Type u := List (Action F)

/-- The state reached by running a plan. -/
def Plan.run [DecidableEq F] (s : State F) : Plan F → State F
  | [] => s
  | a :: π => Plan.run (a.apply s) π

/-- Sequential applicability of a plan from a state. -/
def Plan.applicable [DecidableEq F] (s : State F) : Plan F → Prop
  | [] => True
  | a :: π => a.enabled s ∧ Plan.applicable (a.apply s) π

/-- Boolean version of `Plan.applicable`, for `decide`. -/
def Plan.applicableB [DecidableEq F] (s : State F) : Plan F → Bool
  | [] => true
  | a :: π => modelsB s a.pre && Plan.applicableB (a.apply s) π

theorem Plan.applicable_iff_applicableB [DecidableEq F] {s : State F} {π : Plan F} :
    Plan.applicable s π ↔ Plan.applicableB s π = true := by
  induction π generalizing s with
  | nil => simp [Plan.applicable, Plan.applicableB]
  | cons a π ih =>
    simp only [Plan.applicable, Plan.applicableB, Bool.and_eq_true, Action.enabled]
    rw [models_iff_modelsB, ih]

@[simp] theorem Plan.run_nil [DecidableEq F] (s : State F) :
    Plan.run s ([] : Plan F) = s := rfl

@[simp] theorem Plan.run_cons [DecidableEq F] (s : State F) (a : Action F) (π : Plan F) :
    Plan.run s (a :: π) = Plan.run (a.apply s) π := rfl

theorem Plan.run_append [DecidableEq F] (s : State F) (π₁ π₂ : Plan F) :
    Plan.run s (π₁ ++ π₂) = Plan.run (Plan.run s π₁) π₂ := by
  induction π₁ generalizing s with
  | nil => rfl
  | cons a π ih => simpa using ih (a.apply s)

theorem Plan.applicable_append [DecidableEq F] {s : State F} {π₁ π₂ : Plan F} :
    Plan.applicable s (π₁ ++ π₂) ↔
      (Plan.applicable s π₁ ∧ Plan.applicable (Plan.run s π₁) π₂) := by
  induction π₁ generalizing s with
  | nil => simp [Plan.applicable]
  | cons a π ih => simp only [List.cons_append, Plan.applicable, Plan.run_cons, ih, and_assoc]

/-! ## Planners and Assumption 1 -/

/-- A symbolic planner `S`: given a problem (here: an initial state and a goal — the
domain is implicit in the action type) it either returns a plan or fails. -/
structure Planner (F : Type u) where
  /-- `S(Π)`. -/
  run : State F → Goal F → Option (Plan F)

/-- **Assumption 1 (Planner soundness).** "for every planning problem `Π = ⟨D, s₀, g⟩`,
if `S(Π)` returns a plan `π`, then `π` is applicable from `s₀` and reaches a state
satisfying `g`." -/
def Planner.Sound [DecidableEq F] (S : Planner F) : Prop :=
  ∀ (s : State F) (g : Goal F) (π : Plan F),
    S.run s g = some π → Plan.applicable s π ∧ Plan.run s π ⊨ g

end APTree

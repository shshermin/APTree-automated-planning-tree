/-
# Interference and goal persistence

Formalization of **Definition (Goal interference)** of Appendix B, together with the
goal-persistence lemma underlying the proofs of Theorems 1–3: a goal that holds keeps
holding as long as no action clobbers one of its literals.

`Interferes` and `PlanInterferes` formalize the two clauses of the definition;
`Action.clobbers` and `Plan.Preserves` are the corresponding semantic notions used by
the proofs. `PlanInterferesPos` is the strictly weaker variant covering only positive
goal literals; `Counterexamples.negative_clause_necessary` shows why the definition
must also cover negative ones.
-/

import APTree.Planning

namespace APTree

universe u

variable {F : Type u}

/-! ## Goal/goal interference -/

/-- **Definition (Goal interference), goal/goal clause.** `gᵢ ▷◁ gⱼ`. -/
def Interferes (g₁ g₂ : Goal F) : Prop :=
  ∃ p : F, (Lit.pos p ∈ g₁ ∧ Lit.neg p ∈ g₂) ∨ (Lit.neg p ∈ g₁ ∧ Lit.pos p ∈ g₂)

@[inherit_doc] scoped infix:50 " ▷◁ " => Interferes

/-- Goal/goal interference is symmetric, as the "or vice versa" intends. -/
theorem interferes_symm {g₁ g₂ : Goal F} : g₁ ▷◁ g₂ → g₂ ▷◁ g₁ := by
  intro ⟨p, h⟩
  rcases h with ⟨h₁, h₂⟩ | ⟨h₁, h₂⟩
  · exact ⟨p, Or.inr ⟨h₂, h₁⟩⟩
  · exact ⟨p, Or.inl ⟨h₂, h₁⟩⟩

/-! ## Plan/goal interference -/

/-- The plan/goal clause restricted to *positive* goal literals: some action deletes a
`p` with `p ∈ gᵢ`. Strictly weaker than `PlanInterferes`;
`Counterexamples.negative_clause_necessary` shows the difference matters. -/
def PlanInterferesPos [DecidableEq F] (π : Plan F) (g : Goal F) : Prop :=
  ∃ a ∈ π, ∃ p : F, Lit.pos p ∈ g ∧ p ∈ a.del

/-- **Definition (Goal interference), plan/goal clause.** A plan `πⱼ` interferes with a
goal `gᵢ` iff some action `a ∈ πⱼ` has an effect `¬p` with `p ∈ gᵢ` (here `p ∈ a.del`),
or an effect `p` with `¬p ∈ gᵢ` (here `p ∈ a.add`). -/
def PlanInterferes [DecidableEq F] (π : Plan F) (g : Goal F) : Prop :=
  ∃ a ∈ π, ∃ p : F, (Lit.pos p ∈ g ∧ p ∈ a.del) ∨ (Lit.neg p ∈ g ∧ p ∈ a.add)

/-! ## Clobbering: the semantic notion used by the proofs -/

/-- An action *clobbers* a literal when applying it falsifies that literal. This is the
semantic content of "has an effect `¬p`". -/
def Action.clobbers [DecidableEq F] (a : Action F) : Lit F → Prop
  | .pos p => p ∈ a.del ∧ p ∉ a.add
  | .neg p => p ∈ a.add

/-- An action preserves a goal when it clobbers none of its literals. -/
def Action.Preserves [DecidableEq F] (a : Action F) (g : Goal F) : Prop :=
  ∀ l ∈ g, ¬ a.clobbers l

/-- A plan preserves a goal when none of its actions clobbers any of its literals. -/
def Plan.Preserves [DecidableEq F] (π : Plan F) (g : Goal F) : Prop :=
  ∀ a ∈ π, a.Preserves g

/-- Plan/goal non-interference implies preservation. -/
theorem Plan.Preserves.of_not_interferes [DecidableEq F] {π : Plan F} {g : Goal F}
    (h : ¬ PlanInterferes π g) : π.Preserves g := by
  intro a ha l hl hcl
  apply h
  cases l with
  | pos p => exact ⟨a, ha, p, Or.inl ⟨hl, hcl.1⟩⟩
  | neg p => exact ⟨a, ha, p, Or.inr ⟨hl, hcl⟩⟩

/-! ## Goal persistence -/

/-- A literal that holds and is not clobbered still holds after the action.

This one-step frame lemma is the reasoning behind goal persistence ("each subgoal `gⱼ`,
once achieved, remains satisfied"). -/
theorem Lit.eval_apply_of_not_clobbers [DecidableEq F]
    {a : Action F} {s : State F} {l : Lit F}
    (h : Lit.eval s l = true) (hc : ¬ a.clobbers l) :
    Lit.eval (a.apply s) l = true := by
  cases l with
  | pos p =>
    simp only [Action.clobbers, not_and, Decidable.not_not] at hc
    simp only [Lit.eval] at h ⊢
    simp only [Action.apply]
    by_cases hadd : p ∈ a.add
    · rw [if_pos hadd]
    · by_cases hdel : p ∈ a.del
      · exact absurd (hc hdel) hadd
      · rw [if_neg hadd, if_neg hdel]; exact h
  | neg p =>
    simp only [Action.clobbers] at hc
    simp only [Lit.eval] at h ⊢
    simp only [Action.apply]
    rw [if_neg hc]
    by_cases hdel : p ∈ a.del
    · rw [if_pos hdel]; rfl
    · rw [if_neg hdel]; exact h

/-- One-step goal persistence. -/
theorem Models.apply [DecidableEq F] {a : Action F} {s : State F} {g : Goal F}
    (h : s ⊨ g) (hp : a.Preserves g) : a.apply s ⊨ g :=
  fun l hl => Lit.eval_apply_of_not_clobbers (h l hl) (hp l hl)

/-- **Goal persistence.** A goal that holds and is preserved by every action of a plan
still holds after the plan has run. -/
theorem Models.run [DecidableEq F] {s : State F} {g : Goal F} {π : Plan F}
    (h : s ⊨ g) (hp : π.Preserves g) : Plan.run s π ⊨ g := by
  induction π generalizing s with
  | nil => simpa using h
  | cons a π ih =>
    refine ih (Models.apply h (hp a (List.mem_cons_self ..))) ?_
    exact fun b hb => hp b (List.mem_cons_of_mem _ hb)

end APTree

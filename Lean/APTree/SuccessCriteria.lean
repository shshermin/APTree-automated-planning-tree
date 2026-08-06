/-
# Success criteria

The analysis reasons about goals: "executing `Tᵢ` achieves its inherited goal". The
execution engine reasons about *node results*: a DFN returns `Success` when its
configured `SuccessCriteria` is met by its children's results (`checkSuccessCriteria`).

The DSL offers four criteria:

> * `All`: All child nodes must succeed.
> * `Any`: At least one child node must succeed.
> * `Count`: A specified number of child nodes must succeed.
> * `Percentage`: A specified percentage of child nodes must succeed, enabling partial
>   success thresholds.

The identification of "the DFN succeeded" with "`goal(Fᵢ)` holds" is licensed only by
`All` — which is why the analysis is restricted to DFNs using the `All` criterion. Under
the other three, a DFN can report `Success` with a failed child, and the inherited goal
need not hold — see `Counterexamples.successCriteria_breaks_goal_achievement`.
-/

import APTree.PartialOrder

namespace APTree

universe u

variable {F : Type u} [DecidableEq F]

/-- The `SuccessCriteria` enumeration. -/
inductive SuccessCriteria where
  | All
  | Any
  | Count (n : Nat)
  | Percentage (pct : Nat)
  deriving DecidableEq, Repr

/-- `checkSuccessCriteria`: the verdict a DFN returns given its children's verdicts. -/
def SuccessCriteria.succeeded : SuccessCriteria → List Bool → Bool
  | .All, rs => rs.all id
  | .Any, rs => rs.any id
  | .Count n, rs => n ≤ (rs.filter id).length
  | .Percentage pct, rs => pct * rs.length ≤ 100 * (rs.filter id).length

/-- With criterion `All`, a `Success` verdict does imply that every child succeeded —
the case the analysis is restricted to. -/
theorem SuccessCriteria.all_succeeded {rs : List Bool}
    (h : SuccessCriteria.All.succeeded rs = true) : ∀ r ∈ rs, r = true := by
  simpa [SuccessCriteria.succeeded, List.all_eq_true] using h

/-- With `Any`, a DFN returns `Success` although a child failed. -/
theorem SuccessCriteria.any_succeeded_with_failure :
    SuccessCriteria.Any.succeeded [true, false] = true := by decide

/-- With `Count 1` over two children, likewise. -/
theorem SuccessCriteria.count_succeeded_with_failure :
    (SuccessCriteria.Count 1).succeeded [true, false] = true := by decide

/-- With `Percentage 50` over two children, likewise. -/
theorem SuccessCriteria.percentage_succeeded_with_failure :
    (SuccessCriteria.Percentage 50).succeeded [true, false] = true := by decide

/-! ## Children and their verdicts -/

/-- A child of a DFN together with the verdict it returned. A child that returned
`Failure` has not established its goal. -/
structure Child (F : Type u) where
  /-- The goal this child was planned for. -/
  goal : Goal F
  /-- The plan of this child. -/
  plan : Plan F
  /-- The `BTNodeResult` this child reported, as a success flag. -/
  verdict : Bool

/-- Executing the children of a DFN: only children that returned `Success` have run their
plan to completion; a failed child contributes nothing to the world state. (Taking a
failed child's contribution to be the identity is the *most favourable* modelling choice
— a real failed child may have applied a prefix of its effects, which can only make
matters worse.) -/
def runChildren (s : State F) : List (Child F) → State F
  | [] => s
  | c :: cs => runChildren (if c.verdict then Plan.run s c.plan else s) cs

/-- The conjunction of the children's goals, i.e. `goal(Fᵢ)` decomposed. -/
def childGoals : List (Child F) → Goal F
  | [] => []
  | c :: cs => c.goal ++ childGoals cs

/-- The verdicts a DFN sees. -/
def verdicts (cs : List (Child F)) : List Bool := cs.map Child.verdict

end APTree

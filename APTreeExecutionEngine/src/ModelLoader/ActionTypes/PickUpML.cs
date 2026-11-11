using System;
using System.Collections.Generic;
using System.Linq;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;
using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject
{
    public class PickUpML : PActionNode
    {
        // Parameter: obj of type Element
        public Element obj { get; private set; }

        // Parameter: p of type Location
        public Location p { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: vg of type Gripper
        public Gripper vg { get; private set; }

        // Parameter: rp of type RobotPosition
        public RobotPosition rp { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public PickUpML(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj, Location p, Robot client, Gripper vg, RobotPosition rp)
            : base(actionType, instanceName, blackboard)
        {
            this.obj = obj;
            this.p = p;
            this.client = client;
            this.vg = vg;
            this.rp = rp;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("pickUpML_preconditions"));
            preconditions.AddPredicate(new FastName("pickUpML_pre_0"), new HasTool(client, vg, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_1"), new AtPlace(obj, p, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_2"), new AtAgent(client, rp, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_3"), new GripperEmpty(client, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_4"), new PositionFree(p, true));
            preconditions.AddPredicate(new FastName("pickUpML_pre_5"), new Clear(obj, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_6"), new Fixed(obj, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("pickUpML_effects"));
            effects.AddPredicate(new FastName("pickUpML_eff_0"), new Holding(client, obj, false));
            effects.AddPredicate(new FastName("pickUpML_eff_1"), new AtPlace(obj, p, true));
            effects.AddPredicate(new FastName("pickUpML_eff_2"), new GripperEmpty(client, true));
            effects.AddPredicate(new FastName("pickUpML_eff_3"), new Clear(obj, true));
            effects.AddPredicate(new FastName("pickUpML_eff_4"), new PositionFree(p, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;

        /// <summary>
        /// Extends the base effect application with a runtime conditional effect
        /// that mirrors the PDDL domain's forall/when clause:
        /// if the picked-up object was stacked on any other element, clear that
        /// stacked relationship and mark the element underneath as clear +
        /// accessible. This lets rearrangement plans (e.g. pick blocker → put
        /// down → pick target) work at runtime, not just in the planner.
        /// </summary>
        public override void applyEffects()
        {
            base.applyEffects();

            if (blackboard == null) return;
            RevealWhatWasUnderneath(blackboard.GetAllPredicates(), log: true);
        }

        /// <summary>The same conditional effect, applied to a plan simulation.</summary>
        internal override void SimulateEffects(AIPlanning.PlanSimulationState state)
        {
            base.SimulateEffects(state);
            RevealWhatWasUnderneath(state.All, log: false);
        }

        private void RevealWhatWasUnderneath(IEnumerable<Predicate> current, bool log)
        {
            if (obj == null) return;
            string pickedName = obj.NameKey?.ToString();
            if (string.IsNullOrEmpty(pickedName)) return;

            var predicates = current.ToList();

            // Find stacked(pickedName, ?x) predicates that are currently true
            var stackedToClear = new List<Stacked>();
            foreach (var p in predicates)
            {
                if (p is Stacked s
                    && !s.not
                    && s.obj1?.NameKey?.ToString()?.Equals(pickedName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    stackedToClear.Add(s);
                }
            }

            foreach (var s in stackedToClear)
            {
                string belowName = s.obj2?.NameKey?.ToString() ?? "";
                // Flip stacked(picked, below) → false
                s.not = true;
                if (log) LoggingService.LogSuccess(
                    $"🔧 PickUpML: Cleared stacked({pickedName}, {belowName}) — picked object revealed what was underneath");

                // Set clear(below) = true and accessible(below) = true
                foreach (var p in predicates)
                {
                    if (p is Clear c
                        && c.obj?.NameKey?.ToString()?.Equals(belowName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        c.not = false;
                    }
                    else if (p is Accessible a
                        && a.obj?.NameKey?.ToString()?.Equals(belowName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        a.not = false;
                    }
                }
                if (log) LoggingService.LogSuccess(
                    $"🔧 PickUpML: Set clear({belowName}) = true and accessible({belowName}) = true");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class StackML : PActionNode
    {
        // Parameter: obj1 of type Element
        public Element obj1 { get; private set; }

        // Parameter: obj2 of type Element
        public Element obj2 { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: vg of type VacGripper
        public VacGripper vg { get; private set; }

        // Parameter: pr of type PositionOnRail
        public PositionOnRail pr { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public StackML(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj1, Element obj2, Robot client, VacGripper vg, PositionOnRail pr)
            : base(actionType, instanceName, blackboard)
        {
            this.obj1 = obj1;
            this.obj2 = obj2;
            this.client = client;
            this.vg = vg;
            this.pr = pr;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("stackML_preconditions"));
            preconditions.AddPredicate(new FastName("stackML_pre_0"), new vgempty(client, true));
            preconditions.AddPredicate(new FastName("stackML_pre_1"), new holding(client, obj1, false));
            preconditions.AddPredicate(new FastName("stackML_pre_2"), new atAgent(client, pr, false));
            preconditions.AddPredicate(new FastName("stackML_pre_3"), new activeTool(vg, false));
            preconditions.AddPredicate(new FastName("stackML_pre_4"), new atplace(obj2, pr, false));
            preconditions.AddPredicate(new FastName("stackML_pre_5"), new positionfree(pr, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("stackML_effects"));
            effects.AddPredicate(new FastName("stackML_eff_0"), new ontop(obj1, obj2, false));
            effects.AddPredicate(new FastName("stackML_eff_1"), new holding(client, obj1, true));
            effects.AddPredicate(new FastName("stackML_eff_2"), new atplace(obj1, pr, false));
            effects.AddPredicate(new FastName("stackML_eff_3"), new vgempty(client, false));
            effects.AddPredicate(new FastName("stackML_eff_4"), new clear(obj2, true));
            effects.AddPredicate(new FastName("stackML_eff_5"), new clear(obj1, false));
            effects.AddPredicate(new FastName("stackML_eff_6"), new stacked(obj1, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class NailingHL : PActionNode
    {
        // Parameter: obj1 of type Element
        public Element obj1 { get; private set; }

        // Parameter: obj2 of type Element
        public Element obj2 { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: s of type StaplerGun
        public StaplerGun s { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public NailingHL(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj1, Element obj2, Robot client, StaplerGun s)
            : base(actionType, instanceName, blackboard)
        {
            this.obj1 = obj1;
            this.obj2 = obj2;
            this.client = client;
            this.s = s;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("nailingHL_preconditions"));
            preconditions.AddPredicate(new FastName("nailingHL_pre_0"), new Vgempty(client, false));
            preconditions.AddPredicate(new FastName("nailingHL_pre_1"), new Clear(obj1, false));
            preconditions.AddPredicate(new FastName("nailingHL_pre_2"), new Nailed(obj1, obj2, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("nailingHL_effects"));
            effects.AddPredicate(new FastName("nailingHL_eff_0"), new Nailed(obj1, obj2, false));
            effects.AddPredicate(new FastName("nailingHL_eff_1"), new Fixed(obj1, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

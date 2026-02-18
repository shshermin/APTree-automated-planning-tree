using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class StackHL : PActionNode
    {
        // Parameter: obj1 of type element
        public Element obj1 { get; private set; }

        // Parameter: obj2 of type element
        public Element obj2 { get; private set; }

        // Parameter: client of type robot
        public Robot client { get; private set; }

        // Parameter: pr of type location
        public Location pr { get; private set; }

        // Parameter: lay of type stack
        public Stack lay { get; private set; }

        // Parameter: mod of type cassette
        public Cassette mod { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public StackHL(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj1, Element obj2, Robot client, Location pr, Stack lay, Cassette mod)
            : base(actionType, instanceName, blackboard)
        {
            this.obj1 = obj1;
            this.obj2 = obj2;
            this.client = client;
            this.pr = pr;
            this.lay = lay;
            this.mod = mod;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("stackHL_preconditions"));
            preconditions.AddPredicate(new FastName("stackHL_pre_0"), new Vgempty(client, true));
            preconditions.AddPredicate(new FastName("stackHL_pre_1"), new Holding(client, obj1, false));
            preconditions.AddPredicate(new FastName("stackHL_pre_2"), new Atplace(obj2, pr, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("stackHL_effects"));
            effects.AddPredicate(new FastName("stackHL_eff_0"), new Ontop(obj1, obj2, false));
            effects.AddPredicate(new FastName("stackHL_eff_1"), new Stacked(obj1, false));
            effects.AddPredicate(new FastName("stackHL_eff_2"), new Holding(client, obj1, true));
            effects.AddPredicate(new FastName("stackHL_eff_3"), new Atplace(obj1, pr, false));
            effects.AddPredicate(new FastName("stackHL_eff_4"), new Clear(obj2, true));
            effects.AddPredicate(new FastName("stackHL_eff_5"), new Clear(obj1, false));
            effects.AddPredicate(new FastName("stackHL_eff_6"), new Vgempty(client, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;

    }
}

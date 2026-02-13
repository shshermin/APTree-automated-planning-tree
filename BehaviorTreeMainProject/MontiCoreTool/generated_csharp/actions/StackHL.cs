using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTree.Actions
{
    public class StackHL : PActionNode
    {
        // Parameter: obj1 of type Element
        public Element obj1 { get; private set; }

        // Parameter: obj2 of type Element
        public Element obj2 { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: pr of type Location
        public Location pr { get; private set; }

        // Parameter: lay of type Stack
        public Stack lay { get; private set; }

        // Parameter: mod of type Cassette
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
            preconditions = new State(StateType.Precondition, new FastName("stackhl_preconditions"));
            // TODO: Add preconditions as needed
            // Example: preconditions.AddPredicate(new FastName("pre_0"), new PredicateName(param1, param2, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("stackhl_effects"));
            // TODO: Add effects as needed
            // Example: effects.AddPredicate(new FastName("eff_0"), new PredicateName(param1, param2, true));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

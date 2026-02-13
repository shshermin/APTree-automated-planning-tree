using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTree.Actions
{
    public class StackonmultipleHL : PActionNode
    {
        // Parameter: plate of type Element
        public Element plate { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: pos of type Location
        public Location pos { get; private set; }

        // Parameter: mod of type Cassette
        public Cassette mod { get; private set; }

        // Parameter: lay of type Stack
        public Stack lay { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public StackonmultipleHL(string actionType, string instanceName, Blackboard<FastName> blackboard, Element plate, Robot client, Location pos, Cassette mod, Stack lay)
            : base(actionType, instanceName, blackboard)
        {
            this.plate = plate;
            this.client = client;
            this.pos = pos;
            this.mod = mod;
            this.lay = lay;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("stackonmultiplehl_preconditions"));
            // TODO: Add preconditions as needed
            // Example: preconditions.AddPredicate(new FastName("pre_0"), new PredicateName(param1, param2, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("stackonmultiplehl_effects"));
            // TODO: Add effects as needed
            // Example: effects.AddPredicate(new FastName("eff_0"), new PredicateName(param1, param2, true));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

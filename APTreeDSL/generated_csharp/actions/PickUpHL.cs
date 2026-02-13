using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTree.Actions
{
    public class PickUpHL : PActionNode
    {
        // Parameter: obj of type Element
        public Element obj { get; private set; }

        // Parameter: grabPos of type Location
        public Location grabPos { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public PickUpHL(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj, Location grabPos, Robot client)
            : base(actionType, instanceName, blackboard)
        {
            this.obj = obj;
            this.grabPos = grabPos;
            this.client = client;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("pickuphl_preconditions"));
            // TODO: Add preconditions as needed
            // Example: preconditions.AddPredicate(new FastName("pre_0"), new PredicateName(param1, param2, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("pickuphl_effects"));
            // TODO: Add effects as needed
            // Example: effects.AddPredicate(new FastName("eff_0"), new PredicateName(param1, param2, true));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

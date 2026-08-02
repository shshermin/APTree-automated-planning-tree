using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class TravelML : PActionNode
    {
        public override ActionLevel Level => ActionLevel.MidLevel;

        // Parameter: client of type robot
        public Robot client { get; private set; }

        // Parameter: from of type location
        public Location from { get; private set; }

        // Parameter: to of type location
        public Location to { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public TravelML(string actionType, string instanceName, Blackboard<FastName> blackboard, Robot client, Location from, Location to)
            : base(actionType, instanceName, blackboard)
        {
            this.client = client;
            this.from = from;
            this.to = to;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("travelML_preconditions"));
            preconditions.AddPredicate(new FastName("travelML_pre_0"), new AtAgent(client, from, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("travelML_effects"));
            effects.AddPredicate(new FastName("travelML_eff_0"), new AtAgent(client, from, true));
            effects.AddPredicate(new FastName("travelML_eff_1"), new AtAgent(client, to, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;

    }
}

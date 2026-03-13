using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class InitializeML : PActionNode
    {
        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: too of type Tool
        public Tool too { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public InitializeML(string actionType, string instanceName, Blackboard<FastName> blackboard, Robot client, Tool too)
            : base(actionType, instanceName, blackboard)
        {
            this.client = client;
            this.too = too;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("initializeML_preconditions"));
            preconditions.AddPredicate(new FastName("initializeML_pre_0"), new RobotEquipped(client, false));
            preconditions.AddPredicate(new FastName("initializeML_pre_1"), new HasTool(client, too, false));
            preconditions.AddPredicate(new FastName("initializeML_pre_2"), new ActiveTool(too, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("initializeML_effects"));
            effects.AddPredicate(new FastName("initializeML_eff_0"), new ActiveTool(too, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

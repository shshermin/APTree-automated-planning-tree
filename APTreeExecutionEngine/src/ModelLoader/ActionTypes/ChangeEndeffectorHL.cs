using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class ChangeEndeffectorHL : PActionNode
    {
        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: oldtool of type Tool
        public Tool oldtool { get; private set; }

        // Parameter: newtool of type Tool
        public Tool newtool { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public ChangeEndeffectorHL(string actionType, string instanceName, Blackboard<FastName> blackboard, Robot client, Tool oldtool, Tool newtool)
            : base(actionType, instanceName, blackboard)
        {
            this.client = client;
            this.oldtool = oldtool;
            this.newtool = newtool;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("changeEndeffectorHL_preconditions"));
            preconditions.AddPredicate(new FastName("changeEndeffectorHL_pre_0"), new HasTool(client, oldtool, false));
            preconditions.AddPredicate(new FastName("changeEndeffectorHL_pre_1"), new GripperEmpty(client, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("changeEndeffectorHL_effects"));
            effects.AddPredicate(new FastName("changeEndeffectorHL_eff_0"), new HasTool(client, oldtool, true));
            effects.AddPredicate(new FastName("changeEndeffectorHL_eff_1"), new HasTool(client, newtool, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

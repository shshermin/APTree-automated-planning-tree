using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class NailingHL : PActionNode
    {
        // Parameter: obj of type Element
        public Element obj { get; private set; }

        // Parameter: pos of type PositionOnRail
        public PositionOnRail pos { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public NailingHL(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj, PositionOnRail pos, Robot client)
            : base(actionType, instanceName, blackboard)
        {
            this.obj = obj;
            this.pos = pos;
            this.client = client;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("nailingHL_preconditions"));
            preconditions.AddPredicate(new FastName("nailingHL_pre_0"), new robotequipped(client, false));
            preconditions.AddPredicate(new FastName("nailingHL_pre_1"), new atplace(obj, pos, false));
            preconditions.AddPredicate(new FastName("nailingHL_pre_2"), new clear(obj, false));
            preconditions.AddPredicate(new FastName("nailingHL_pre_3"), new nailed(obj, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("nailingHL_effects"));
            effects.AddPredicate(new FastName("nailingHL_eff_0"), new nailed(obj, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

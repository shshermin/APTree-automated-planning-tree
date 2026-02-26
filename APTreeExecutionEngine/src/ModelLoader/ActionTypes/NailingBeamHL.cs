using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class NailingBeamHL : PActionNode
    {
        // Parameter: obj of type element
        public Element obj { get; private set; }

        // Parameter: pos of type positionOnRail
        public PositionOnRail pos { get; private set; }

        // Parameter: client of type robot
        public Robot client { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public NailingBeamHL(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj, PositionOnRail pos, Robot client)
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
            preconditions.AddPredicate(new FastName("nailingHL_pre_0"), new Robotequipped(client, false));
            preconditions.AddPredicate(new FastName("nailingHL_pre_1"), new Atplace(obj, pos, false));
            preconditions.AddPredicate(new FastName("nailingHL_pre_2"), new Clear(obj, false));
            preconditions.AddPredicate(new FastName("nailingHL_pre_3"), new Nailed(obj, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("nailingHL_effects"));
            effects.AddPredicate(new FastName("nailingHL_eff_0"), new Nailed(obj, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;

    }
}

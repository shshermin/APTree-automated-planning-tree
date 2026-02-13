using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class NailingML : PActionNode
    {
        // Parameter: obj of type element
        public Element obj { get; private set; }

        // Parameter: pos of type positionOnRail
        public PositionOnRail pos { get; private set; }

        // Parameter: client of type robot
        public Robot client { get; private set; }

        // Parameter: ng of type nailGripper
        public NailGripper ng { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public NailingML(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj, PositionOnRail pos, Robot client, NailGripper ng)
            : base(actionType, instanceName, blackboard)
        {
            this.obj = obj;
            this.pos = pos;
            this.client = client;
            this.ng = ng;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("nailingML_preconditions"));
            preconditions.AddPredicate(new FastName("nailingML_pre_0"), new AtAgent(client, pos, false));
            preconditions.AddPredicate(new FastName("nailingML_pre_1"), new Atplace(obj, pos, false));
            preconditions.AddPredicate(new FastName("nailingML_pre_2"), new ActiveTool(ng, false));
            preconditions.AddPredicate(new FastName("nailingML_pre_3"), new Nailed(obj, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("nailingML_effects"));
            effects.AddPredicate(new FastName("nailingML_eff_0"), new Nailed(obj, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;

    }
}

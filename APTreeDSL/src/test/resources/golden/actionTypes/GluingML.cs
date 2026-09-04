using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class GluingML : PActionNode
    {
        // Parameter: obj of type Element
        public Element obj { get; private set; }

        // Parameter: pos of type PositionOnRail
        public PositionOnRail pos { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: gg of type GlueGun
        public GlueGun gg { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public GluingML(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj, PositionOnRail pos, Robot client, GlueGun gg)
            : base(actionType, instanceName, blackboard)
        {
            this.obj = obj;
            this.pos = pos;
            this.client = client;
            this.gg = gg;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("gluingML_preconditions"));
            preconditions.AddPredicate(new FastName("gluingML_pre_0"), new atAgent(client, pos, false));
            preconditions.AddPredicate(new FastName("gluingML_pre_1"), new atplace(obj, pos, false));
            preconditions.AddPredicate(new FastName("gluingML_pre_2"), new clear(obj, false));
            preconditions.AddPredicate(new FastName("gluingML_pre_3"), new activeTool(gg, false));
            preconditions.AddPredicate(new FastName("gluingML_pre_4"), new glued(obj, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("gluingML_effects"));
            effects.AddPredicate(new FastName("gluingML_eff_0"), new glued(obj, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

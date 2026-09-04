using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class GluingBeamHL : PActionNode
    {
        // Parameter: obj of type Beam
        public Beam obj { get; private set; }

        // Parameter: pos of type PositionOnRail
        public PositionOnRail pos { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: mod of type Cassette
        public Cassette mod { get; private set; }

        // Parameter: lay of type Stack
        public Stack lay { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public GluingBeamHL(string actionType, string instanceName, Blackboard<FastName> blackboard, Beam obj, PositionOnRail pos, Robot client, Cassette mod, Stack lay)
            : base(actionType, instanceName, blackboard)
        {
            this.obj = obj;
            this.pos = pos;
            this.client = client;
            this.mod = mod;
            this.lay = lay;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("gluingBeamHL_preconditions"));
            preconditions.AddPredicate(new FastName("gluingBeamHL_pre_0"), new robotequipped(client, true));
            preconditions.AddPredicate(new FastName("gluingBeamHL_pre_1"), new atplace(obj, pos, false));
            preconditions.AddPredicate(new FastName("gluingBeamHL_pre_2"), new clear(obj, false));
            preconditions.AddPredicate(new FastName("gluingBeamHL_pre_3"), new glued(obj, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("gluingBeamHL_effects"));
            effects.AddPredicate(new FastName("gluingBeamHL_eff_0"), new glued(obj, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

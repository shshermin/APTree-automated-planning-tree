using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTree.Actions
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
            preconditions = new State(StateType.Precondition, new FastName("gluingbeamhl_preconditions"));
            // TODO: Add preconditions as needed
            // Example: preconditions.AddPredicate(new FastName("pre_0"), new PredicateName(param1, param2, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("gluingbeamhl_effects"));
            // TODO: Add effects as needed
            // Example: effects.AddPredicate(new FastName("eff_0"), new PredicateName(param1, param2, true));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

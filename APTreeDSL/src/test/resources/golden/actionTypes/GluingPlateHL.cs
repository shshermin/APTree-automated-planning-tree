using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class GluingPlateHL : PActionNode
    {
        // Parameter: obj of type Plate
        public Plate obj { get; private set; }

        // Parameter: pos of type PositionOnRail
        public PositionOnRail pos { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public GluingPlateHL(string actionType, string instanceName, Blackboard<FastName> blackboard, Plate obj, PositionOnRail pos, Robot client)
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
            preconditions = new State(StateType.Precondition, new FastName("gluingPlateHL_preconditions"));
            preconditions.AddPredicate(new FastName("gluingPlateHL_pre_0"), new robotequipped(client, true));
            preconditions.AddPredicate(new FastName("gluingPlateHL_pre_1"), new atplace(obj, pos, false));
            preconditions.AddPredicate(new FastName("gluingPlateHL_pre_2"), new clear(obj, false));
            preconditions.AddPredicate(new FastName("gluingPlateHL_pre_3"), new glued(obj, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("gluingPlateHL_effects"));
            effects.AddPredicate(new FastName("gluingPlateHL_eff_0"), new glued(obj, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

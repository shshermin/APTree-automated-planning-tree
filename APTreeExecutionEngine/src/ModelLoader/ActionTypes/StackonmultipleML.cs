using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class StackOnMultipleML : PActionNode
    {
        // Parameter: plate of type plate
        public Plate plate { get; private set; }

        // Parameter: client of type robot
        public Robot client { get; private set; }

        // Parameter: pos of type positionOnRail
        public PositionOnRail pos { get; private set; }

        // Parameter: vg of type vacuumGripper
        public VacGripper vg { get; private set; }

        // Parameter: mod of type cassette
        public Cassette mod { get; private set; }

        // Parameter: lay of type stack
        public Stack lay { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public StackOnMultipleML(string actionType, string instanceName, Blackboard<FastName> blackboard, Plate plate, Robot client, PositionOnRail pos, VacGripper vg, Cassette mod, Stack lay)
            : base(actionType, instanceName, blackboard)
        {
            this.plate = plate;
            this.client = client;
            this.pos = pos;
            this.vg = vg;
            this.mod = mod;
            this.lay = lay;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("stackonmultipleML_preconditions"));
            preconditions.AddPredicate(new FastName("stackonmultipleML_pre_0"), new Holding(client, plate, false));
            preconditions.AddPredicate(new FastName("stackonmultipleML_pre_1"), new AtAgent(client, pos, false));
            preconditions.AddPredicate(new FastName("stackonmultipleML_pre_2"), new ActiveTool(vg, false));
            preconditions.AddPredicate(new FastName("stackonmultipleML_pre_3"), new AtPlace(plate, pos, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("stackonmultipleML_effects"));
            effects.AddPredicate(new FastName("stackonmultipleML_eff_0"), new AtPlace(plate, pos, false));
            effects.AddPredicate(new FastName("stackonmultipleML_eff_1"), new Vgempty(client, false));
            effects.AddPredicate(new FastName("stackonmultipleML_eff_2"), new Clear(plate, true));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;

    }
}

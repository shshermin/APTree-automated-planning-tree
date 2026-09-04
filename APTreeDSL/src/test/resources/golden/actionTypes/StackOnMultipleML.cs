using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class StackOnMultipleML : PActionNode
    {
        // Parameter: plate of type Plate
        public Plate plate { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: pos of type PositionOnRail
        public PositionOnRail pos { get; private set; }

        // Parameter: vg of type VacGripper
        public VacGripper vg { get; private set; }

        // Parameter: mod of type Cassette
        public Cassette mod { get; private set; }

        // Parameter: lay of type Stack
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
            preconditions = new State(StateType.Precondition, new FastName("stackOnMultipleML_preconditions"));
            preconditions.AddPredicate(new FastName("stackOnMultipleML_pre_0"), new holding(client, plate, false));
            preconditions.AddPredicate(new FastName("stackOnMultipleML_pre_1"), new atAgent(client, pos, false));
            preconditions.AddPredicate(new FastName("stackOnMultipleML_pre_2"), new activeTool(vg, false));
            preconditions.AddPredicate(new FastName("stackOnMultipleML_pre_3"), new atplace(plate, pos, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("stackOnMultipleML_effects"));
            effects.AddPredicate(new FastName("stackOnMultipleML_eff_0"), new atplace(plate, pos, false));
            effects.AddPredicate(new FastName("stackOnMultipleML_eff_1"), new vgempty(client, false));
            effects.AddPredicate(new FastName("stackOnMultipleML_eff_2"), new clear(plate, true));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class StackOnMultipleHL : PActionNode
    {
        // Parameter: plate of type Element
        public Element plate { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: pos of type Location
        public Location pos { get; private set; }

        // Parameter: mod of type Cassette
        public Cassette mod { get; private set; }

        // Parameter: lay of type Stack
        public Stack lay { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public StackOnMultipleHL(string actionType, string instanceName, Blackboard<FastName> blackboard, Element plate, Robot client, Location pos, Cassette mod, Stack lay)
            : base(actionType, instanceName, blackboard)
        {
            this.plate = plate;
            this.client = client;
            this.pos = pos;
            this.mod = mod;
            this.lay = lay;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("stackOnMultipleHL_preconditions"));
            preconditions.AddPredicate(new FastName("stackOnMultipleHL_pre_0"), new holding(client, plate, false));
            preconditions.AddPredicate(new FastName("stackOnMultipleHL_pre_1"), new atplace(plate, pos, true));
            preconditions.AddPredicate(new FastName("stackOnMultipleHL_pre_2"), new robotequipped(client, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("stackOnMultipleHL_effects"));
            effects.AddPredicate(new FastName("stackOnMultipleHL_eff_0"), new atplace(plate, pos, false));
            effects.AddPredicate(new FastName("stackOnMultipleHL_eff_1"), new clear(plate, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

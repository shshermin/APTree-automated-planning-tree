using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class StackOnMultipleHL : PActionNode
    {
        // Parameter: plate of type element
        public Element plate { get; private set; }

        // Parameter: client of type robot
        public Robot client { get; private set; }

        // Parameter: pos of type location
        public Location pos { get; private set; }

        // Parameter: mod of type cassette
        public Cassette mod { get; private set; }

        // Parameter: lay of type stack
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
            preconditions = new State(StateType.Precondition, new FastName("stackonmultipleHL_preconditions"));
            preconditions.AddPredicate(new FastName("stackonmultipleHL_pre_0"), new Holding(client, plate, false));
            preconditions.AddPredicate(new FastName("stackonmultipleHL_pre_1"), new AtPlace(plate, pos, true));
            preconditions.AddPredicate(new FastName("stackonmultipleHL_pre_2"), new Vgempty(client, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("stackonmultipleHL_effects"));
            effects.AddPredicate(new FastName("stackonmultipleHL_eff_0"), new AtPlace(plate, pos, false));
            effects.AddPredicate(new FastName("stackonmultipleHL_eff_1"), new Vgempty(client, false));
            effects.AddPredicate(new FastName("stackonmultipleHL_eff_2"), new Clear(plate, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;

    }
}

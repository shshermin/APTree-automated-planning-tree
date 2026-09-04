using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class PlaceHL : PActionNode
    {
        // Parameter: obj of type Element
        public Element obj { get; private set; }

        // Parameter: placePos of type Location
        public Location placePos { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public PlaceHL(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj, Location placePos, Robot client)
            : base(actionType, instanceName, blackboard)
        {
            this.obj = obj;
            this.placePos = placePos;
            this.client = client;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("placeHL_preconditions"));
            preconditions.AddPredicate(new FastName("placeHL_pre_0"), new holding(client, obj, false));
            preconditions.AddPredicate(new FastName("placeHL_pre_1"), new clear(obj, true));
            preconditions.AddPredicate(new FastName("placeHL_pre_2"), new positionfree(placePos, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("placeHL_effects"));
            effects.AddPredicate(new FastName("placeHL_eff_0"), new atplace(obj, placePos, false));
            effects.AddPredicate(new FastName("placeHL_eff_1"), new holding(client, obj, true));
            effects.AddPredicate(new FastName("placeHL_eff_2"), new clear(obj, false));
            effects.AddPredicate(new FastName("placeHL_eff_3"), new positionfree(placePos, true));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

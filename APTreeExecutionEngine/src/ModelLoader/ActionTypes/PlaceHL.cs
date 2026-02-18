using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class PlaceHL : PActionNode
    {
        // Parameter: obj of type element
        public Element obj { get; private set; }

        // Parameter: placePos of type location
        public Location placePos { get; private set; }

        // Parameter: client of type robot
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
            preconditions.AddPredicate(new FastName("placeHL_pre_0"), new Vgempty(client, true));
            preconditions.AddPredicate(new FastName("placeHL_pre_1"), new Holding(client, obj, false));
            preconditions.AddPredicate(new FastName("placeHL_pre_2"), new Clear(obj, true));
            preconditions.AddPredicate(new FastName("placeHL_pre_3"), new Positionfree(placePos, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("placeHL_effects"));
            effects.AddPredicate(new FastName("placeHL_eff_0"), new AtPlace(obj, placePos, false));
            effects.AddPredicate(new FastName("placeHL_eff_1"), new Holding(client, obj, true));
            effects.AddPredicate(new FastName("placeHL_eff_2"), new Clear(obj, false));
<<<<<<< HEAD
            effects.AddPredicate(new FastName("placeHL_eff_3"), new PositionFree(placePos, true));
=======
            effects.AddPredicate(new FastName("placeHL_eff_3"), new Positionfree(placePos, true));
            effects.AddPredicate(new FastName("placeHL_eff_4"), new Vgempty(client, false));
>>>>>>> 3505327 (optimizing the speed with new decorators)
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;

    }
}

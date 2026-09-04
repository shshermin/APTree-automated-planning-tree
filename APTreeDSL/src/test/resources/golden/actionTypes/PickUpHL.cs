using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class PickUpHL : PActionNode
    {
        // Parameter: obj of type Element
        public Element obj { get; private set; }

        // Parameter: grabPos of type Location
        public Location grabPos { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public PickUpHL(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj, Location grabPos, Robot client)
            : base(actionType, instanceName, blackboard)
        {
            this.obj = obj;
            this.grabPos = grabPos;
            this.client = client;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("pickUpHL_preconditions"));
            preconditions.AddPredicate(new FastName("pickUpHL_pre_0"), new robotequipped(client, true));
            preconditions.AddPredicate(new FastName("pickUpHL_pre_1"), new atplace(obj, grabPos, false));
            preconditions.AddPredicate(new FastName("pickUpHL_pre_2"), new holding(client, obj, true));
            preconditions.AddPredicate(new FastName("pickUpHL_pre_3"), new positionfree(grabPos, true));
            preconditions.AddPredicate(new FastName("pickUpHL_pre_4"), new clear(obj, false));
            preconditions.AddPredicate(new FastName("pickUpHL_pre_5"), new stacked(obj, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("pickUpHL_effects"));
            effects.AddPredicate(new FastName("pickUpHL_eff_0"), new holding(client, obj, false));
            effects.AddPredicate(new FastName("pickUpHL_eff_1"), new atplace(obj, grabPos, true));
            effects.AddPredicate(new FastName("pickUpHL_eff_2"), new clear(obj, true));
            effects.AddPredicate(new FastName("pickUpHL_eff_3"), new positionfree(grabPos, false));
            effects.AddPredicate(new FastName("pickUpHL_eff_4"), new robotequipped(client, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

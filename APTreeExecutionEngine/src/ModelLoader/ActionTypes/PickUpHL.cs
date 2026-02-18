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

        // Parameter: p of type Location
        public Location p { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: g of type Gripper
        public Gripper g { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public PickUpHL(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj, Location p, Robot client, Gripper g)
            : base(actionType, instanceName, blackboard)
        {
            this.obj = obj;
            this.p = p;
            this.client = client;
            this.g = g;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("pickUpHL_preconditions"));
            preconditions.AddPredicate(new FastName("pickUpHL_pre_0"), new Vgempty(client, false));
            preconditions.AddPredicate(new FastName("pickUpHL_pre_1"), new Atplace(obj, grabPos, false));
            preconditions.AddPredicate(new FastName("pickUpHL_pre_2"), new Holding(client, obj, true));
            preconditions.AddPredicate(new FastName("pickUpHL_pre_3"), new Positionfree(grabPos, true));
            preconditions.AddPredicate(new FastName("pickUpHL_pre_4"), new Clear(obj, false));
            preconditions.AddPredicate(new FastName("pickUpHL_pre_5"), new Stacked(obj, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("pickUpHL_effects"));
            effects.AddPredicate(new FastName("pickUpHL_eff_0"), new Holding(client, obj, false));
<<<<<<< HEAD
            effects.AddPredicate(new FastName("pickUpHL_eff_1"), new AtPlace(obj, p, true));
            effects.AddPredicate(new FastName("pickUpHL_eff_2"), new GripperEmpty(client, true));
            effects.AddPredicate(new FastName("pickUpHL_eff_3"), new Clear(obj, true));
            effects.AddPredicate(new FastName("pickUpHL_eff_4"), new PositionFree(p, false));
=======
            effects.AddPredicate(new FastName("pickUpHL_eff_1"), new Atplace(obj, grabPos, true));
            effects.AddPredicate(new FastName("pickUpHL_eff_2"), new Clear(obj, true));
            effects.AddPredicate(new FastName("pickUpHL_eff_3"), new Positionfree(grabPos, false));
            effects.AddPredicate(new FastName("pickUpHL_eff_4"), new Vgempty(client, true));
>>>>>>> 3505327 (optimizing the speed with new decorators)
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

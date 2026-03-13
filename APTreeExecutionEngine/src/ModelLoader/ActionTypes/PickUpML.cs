using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class PickUpML : PActionNode
    {
        // Parameter: obj of type Element
        public Element obj { get; private set; }

        // Parameter: p of type Location
        public Location p { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: vg of type Gripper
        public Gripper vg { get; private set; }

        // Parameter: rp of type RobotPosition
        public RobotPosition rp { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public PickUpML(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj, Location p, Robot client, Gripper vg, RobotPosition rp)
            : base(actionType, instanceName, blackboard)
        {
            this.obj = obj;
            this.p = p;
            this.client = client;
            this.vg = vg;
            this.rp = rp;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("pickUpML_preconditions"));
            preconditions.AddPredicate(new FastName("pickUpML_pre_0"), new HasTool(client, vg, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_1"), new AtPlace(obj, p, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_2"), new AtAgent(client, rp, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_3"), new GripperEmpty(client, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_4"), new PositionFree(p, true));
            preconditions.AddPredicate(new FastName("pickUpML_pre_5"), new Clear(obj, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_6"), new Fixed(obj, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("pickUpML_effects"));
            effects.AddPredicate(new FastName("pickUpML_eff_0"), new Holding(client, obj, false));
            effects.AddPredicate(new FastName("pickUpML_eff_1"), new AtPlace(obj, p, true));
            effects.AddPredicate(new FastName("pickUpML_eff_2"), new GripperEmpty(client, true));
            effects.AddPredicate(new FastName("pickUpML_eff_3"), new Clear(obj, true));
            effects.AddPredicate(new FastName("pickUpML_eff_4"), new PositionFree(p, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

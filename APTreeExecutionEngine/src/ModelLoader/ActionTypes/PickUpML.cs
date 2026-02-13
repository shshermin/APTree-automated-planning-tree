using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class PickUpML : PActionNode
    {
        // Parameter: obj of type element
        public Element obj { get; private set; }

        // Parameter: pos of type firstposition
        public Firstposition pos { get; private set; }

        // Parameter: client of type robot
        public Robot client { get; private set; }

        // Parameter: vg of type vacuumGripper
        public VacuumGripper vg { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public PickUpML(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj, Firstposition pos, Robot client, VacuumGripper vg)
            : base(actionType, instanceName, blackboard)
        {
            this.obj = obj;
            this.pos = pos;
            this.client = client;
            this.vg = vg;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("pickUpML_preconditions"));
            preconditions.AddPredicate(new FastName("pickUpML_pre_0"), new HasTool(client, vg, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_1"), new ActiveTool(vg, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_2"), new Atplace(obj, pos, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_3"), new AtAgent(client, pos, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_4"), new Vgempty(client, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_5"), new Holding(client, obj, true));
            preconditions.AddPredicate(new FastName("pickUpML_pre_6"), new Clear(obj, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("pickUpML_effects"));
            effects.AddPredicate(new FastName("pickUpML_eff_0"), new Holding(client, obj, false));
            effects.AddPredicate(new FastName("pickUpML_eff_1"), new Atplace(obj, pos, true));
            effects.AddPredicate(new FastName("pickUpML_eff_2"), new Vgempty(client, true));
            effects.AddPredicate(new FastName("pickUpML_eff_3"), new Clear(obj, true));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;

    }
}

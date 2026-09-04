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

        // Parameter: pos of type FirstPos
        public FirstPos pos { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: vg of type VacGripper
        public VacGripper vg { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public PickUpML(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj, FirstPos pos, Robot client, VacGripper vg)
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
            preconditions.AddPredicate(new FastName("pickUpML_pre_0"), new hasTool(client, vg, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_1"), new activeTool(vg, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_2"), new atplace(obj, pos, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_3"), new atAgent(client, pos, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_4"), new vgempty(client, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_5"), new holding(client, obj, true));
            preconditions.AddPredicate(new FastName("pickUpML_pre_6"), new clear(obj, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("pickUpML_effects"));
            effects.AddPredicate(new FastName("pickUpML_eff_0"), new holding(client, obj, false));
            effects.AddPredicate(new FastName("pickUpML_eff_1"), new atplace(obj, pos, true));
            effects.AddPredicate(new FastName("pickUpML_eff_2"), new vgempty(client, true));
            effects.AddPredicate(new FastName("pickUpML_eff_3"), new clear(obj, true));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

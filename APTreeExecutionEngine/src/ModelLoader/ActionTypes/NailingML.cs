using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class NailingML : PActionNode
    {
        // Parameter: obj1 of type Element
        public Element obj1 { get; private set; }

        // Parameter: obj2 of type Element
        public Element obj2 { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: ng of type StaplerGun
        public StaplerGun ng { get; private set; }

        // Parameter: rp of type RobotPosition
        public RobotPosition rp { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public NailingML(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj1, Element obj2, Robot client, StaplerGun ng, RobotPosition rp)
            : base(actionType, instanceName, blackboard)
        {
            this.obj1 = obj1;
            this.obj2 = obj2;
            this.client = client;
            this.ng = ng;
            this.rp = rp;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("nailingML_preconditions"));
            preconditions.AddPredicate(new FastName("nailingML_pre_0"), new AtAgent(client, rp, false));
            preconditions.AddPredicate(new FastName("nailingML_pre_1"), new AtFinalPosition(obj2, false));
            preconditions.AddPredicate(new FastName("nailingML_pre_2"), new AtFinalPosition(obj1, false));
            preconditions.AddPredicate(new FastName("nailingML_pre_3"), new Accessible(obj1, false));
            preconditions.AddPredicate(new FastName("nailingML_pre_4"), new ActiveTool(ng, false));
            preconditions.AddPredicate(new FastName("nailingML_pre_5"), new HasTool(client, ng, false));
            preconditions.AddPredicate(new FastName("nailingML_pre_6"), new Nailed(obj1, obj2, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("nailingML_effects"));
            effects.AddPredicate(new FastName("nailingML_eff_0"), new Nailed(obj1, obj2, false));
            effects.AddPredicate(new FastName("nailingML_eff_1"), new Fixed(obj1, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

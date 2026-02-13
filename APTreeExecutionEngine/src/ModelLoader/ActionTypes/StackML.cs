using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class StackML : PActionNode
    {
        // Parameter: obj1 of type element
        public Element obj1 { get; private set; }

        // Parameter: obj2 of type element
        public Element obj2 { get; private set; }

        // Parameter: client of type robot
        public Robot client { get; private set; }

        // Parameter: vg of type vacuumGripper
        public VacuumGripper vg { get; private set; }

        // Parameter: pr of type positionOnRail
        public PositionOnRail pr { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public StackML(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj1, Element obj2, Robot client, VacuumGripper vg, PositionOnRail pr)
            : base(actionType, instanceName, blackboard)
        {
            this.obj1 = obj1;
            this.obj2 = obj2;
            this.client = client;
            this.vg = vg;
            this.pr = pr;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("stackML_preconditions"));
            preconditions.AddPredicate(new FastName("stackML_pre_0"), new Vgempty(client, true));
            preconditions.AddPredicate(new FastName("stackML_pre_1"), new Holding(client, obj1, false));
            preconditions.AddPredicate(new FastName("stackML_pre_2"), new AtAgent(client, pr, false));
            preconditions.AddPredicate(new FastName("stackML_pre_3"), new ActiveTool(vg, false));
            preconditions.AddPredicate(new FastName("stackML_pre_4"), new Atplace(obj2, pr, false));
            preconditions.AddPredicate(new FastName("stackML_pre_5"), new Positionfree(pr, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("stackML_effects"));
            effects.AddPredicate(new FastName("stackML_eff_0"), new Ontop(obj1, obj2, false));
            effects.AddPredicate(new FastName("stackML_eff_1"), new Holding(client, obj1, true));
            effects.AddPredicate(new FastName("stackML_eff_2"), new Atplace(obj1, pr, false));
            effects.AddPredicate(new FastName("stackML_eff_3"), new Vgempty(client, false));
            effects.AddPredicate(new FastName("stackML_eff_4"), new Clear(obj2, true));
            effects.AddPredicate(new FastName("stackML_eff_5"), new Clear(obj1, false));
            effects.AddPredicate(new FastName("stackML_eff_6"), new Stacked(obj1, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;

    }
}

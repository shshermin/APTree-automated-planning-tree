using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class EquipeML : PActionNode
    {
        // Parameter: client of type robot
        public Robot client { get; private set; }

        // Parameter: too of type tool
        public Tool too { get; private set; }

        // Parameter: ep of type equipposition
        public EquipPosition ep { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;
        public int cost = 50;

        public EquipeML(string actionType, string instanceName, Blackboard<FastName> blackboard, Robot client, Tool too, EquipPosition ep)
            : base(actionType, instanceName, blackboard)
        {
            this.client = client;
            this.too = too;
            this.ep = ep;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("equipeML_preconditions"));
            preconditions.AddPredicate(new FastName("equipeML_pre_0"), new AtTool(too, ep, false));
            preconditions.AddPredicate(new FastName("equipeML_pre_1"), new Robotequipped(client, true));
            preconditions.AddPredicate(new FastName("equipeML_pre_2"), new Positionfree(ep, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("equipeML_effects"));
            effects.AddPredicate(new FastName("equipeML_eff_0"), new HasTool(client, too, false));
            effects.AddPredicate(new FastName("equipeML_eff_1"), new Robotequipped(client, false));
            effects.AddPredicate(new FastName("equipeML_eff_2"), new Positionfree(ep, false));
            effects.AddPredicate(new FastName("equipeML_eff_3"), new AtTool(too, ep, true));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;

    }
}

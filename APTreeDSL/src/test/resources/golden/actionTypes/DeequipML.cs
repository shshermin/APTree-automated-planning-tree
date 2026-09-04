using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class DeequipML : PActionNode
    {
        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: too of type Tool
        public Tool too { get; private set; }

        // Parameter: ep of type EquipPosition
        public EquipPosition ep { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public DeequipML(string actionType, string instanceName, Blackboard<FastName> blackboard, Robot client, Tool too, EquipPosition ep)
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
            preconditions = new State(StateType.Precondition, new FastName("deequipML_preconditions"));
            preconditions.AddPredicate(new FastName("deequipML_pre_0"), new hasTool(client, too, false));
            preconditions.AddPredicate(new FastName("deequipML_pre_1"), new robotequipped(client, false));
            preconditions.AddPredicate(new FastName("deequipML_pre_2"), new positionfree(ep, false));
            preconditions.AddPredicate(new FastName("deequipML_pre_3"), new atTool(too, ep, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("deequipML_effects"));
            effects.AddPredicate(new FastName("deequipML_eff_0"), new robotequipped(client, true));
            effects.AddPredicate(new FastName("deequipML_eff_1"), new hasTool(client, too, true));
            effects.AddPredicate(new FastName("deequipML_eff_2"), new positionfree(ep, true));
            effects.AddPredicate(new FastName("deequipML_eff_3"), new atTool(too, ep, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

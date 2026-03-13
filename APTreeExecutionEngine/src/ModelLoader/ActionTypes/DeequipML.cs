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

        // Parameter: ep of type EquipLocation
        public EquipLocation ep { get; private set; }

        // Parameter: rp of type RobotPosition
        public RobotPosition rp { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public DeequipML(string actionType, string instanceName, Blackboard<FastName> blackboard, Robot client, Tool too, EquipLocation ep, RobotPosition rp)
            : base(actionType, instanceName, blackboard)
        {
            this.client = client;
            this.too = too;
            this.ep = ep;
            this.rp = rp;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("deequipML_preconditions"));
            preconditions.AddPredicate(new FastName("deequipML_pre_0"), new AtAgent(client, rp, false));
            preconditions.AddPredicate(new FastName("deequipML_pre_1"), new HasTool(client, too, false));
            preconditions.AddPredicate(new FastName("deequipML_pre_2"), new ActiveTool(too, true));
            preconditions.AddPredicate(new FastName("deequipML_pre_3"), new AtTool(too, ep, true));
            preconditions.AddPredicate(new FastName("deequipML_pre_4"), new RobotEquipped(client, false));
            preconditions.AddPredicate(new FastName("deequipML_pre_5"), new PositionFree(ep, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("deequipML_effects"));
            effects.AddPredicate(new FastName("deequipML_eff_0"), new AtTool(too, ep, false));
            effects.AddPredicate(new FastName("deequipML_eff_1"), new RobotEquipped(client, true));
            effects.AddPredicate(new FastName("deequipML_eff_2"), new HasTool(client, too, true));
            effects.AddPredicate(new FastName("deequipML_eff_3"), new PositionFree(ep, true));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

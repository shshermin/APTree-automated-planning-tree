using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class EquipeML : PActionNode
    {
        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: too of type Tool
        public Tool too { get; private set; }

        // Parameter: rp of type RobotPosition
        public RobotPosition rp { get; private set; }

        // Parameter: ep of type EquipLocation
        public EquipLocation ep { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public EquipeML(string actionType, string instanceName, Blackboard<FastName> blackboard, Robot client, Tool too, RobotPosition rp, EquipLocation ep)
            : base(actionType, instanceName, blackboard)
        {
            this.client = client;
            this.too = too;
            this.rp = rp;
            this.ep = ep;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("equipeML_preconditions"));
            preconditions.AddPredicate(new FastName("equipeML_pre_0"), new AtTool(too, ep, false));
            preconditions.AddPredicate(new FastName("equipeML_pre_1"), new RobotEquipped(client, true));
            preconditions.AddPredicate(new FastName("equipeML_pre_2"), new AtAgent(client, rp, false));
            preconditions.AddPredicate(new FastName("equipeML_pre_3"), new PositionFree(ep, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("equipeML_effects"));
            effects.AddPredicate(new FastName("equipeML_eff_0"), new HasTool(client, too, false));
            effects.AddPredicate(new FastName("equipeML_eff_1"), new RobotEquipped(client, false));
            effects.AddPredicate(new FastName("equipeML_eff_2"), new AtTool(too, ep, true));
            effects.AddPredicate(new FastName("equipeML_eff_3"), new PositionFree(ep, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

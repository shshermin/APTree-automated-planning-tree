using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class PickUpCassetteML : PActionNode
    {
        // Parameter: lp of type plate
        public Plate lp { get; private set; }

        // Parameter: mod of type cassette
        public Cassette mod { get; private set; }

        // Parameter: lay of type stack
        public Stack lay { get; private set; }

        // Parameter: p of type location
        public Location p { get; private set; }

        // Parameter: client of type robot
        public Robot client { get; private set; }

        // Parameter: vg of type vacgripper
        public VacGripper vg { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public PickUpCassetteML(string actionType, string instanceName, Blackboard<FastName> blackboard, Plate lp, Cassette mod, Stack lay, Location p, Robot client, VacGripper vg)
            : base(actionType, instanceName, blackboard)
        {
            this.lp = lp;
            this.mod = mod;
            this.lay = lay;
            this.p = p;
            this.client = client;
            this.vg = vg;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("pickUpCassetteML_preconditions"));
            preconditions.AddPredicate(new FastName("pickUpCassetteML_pre_0"), new HasTool(client, vg, false));
            preconditions.AddPredicate(new FastName("pickUpCassetteML_pre_1"), new ActiveTool(vg, false));
            preconditions.AddPredicate(new FastName("pickUpCassetteML_pre_2"), new Vgempty(client, false));
            preconditions.AddPredicate(new FastName("pickUpCassetteML_pre_3"), new AtAgent(client, p, false));
            preconditions.AddPredicate(new FastName("pickUpCassetteML_pre_4"), new Atplace(lp, p, false));
            preconditions.AddPredicate(new FastName("pickUpCassetteML_pre_5"), new Belongstomodule(lp, mod, false));
            preconditions.AddPredicate(new FastName("pickUpCassetteML_pre_6"), new Holding(client, lp, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("pickUpCassetteML_effects"));
            effects.AddPredicate(new FastName("pickUpCassetteML_eff_0"), new Holding(client, lp, false));
            effects.AddPredicate(new FastName("pickUpCassetteML_eff_1"), new Atplace(lp, p, true));
            effects.AddPredicate(new FastName("pickUpCassetteML_eff_2"), new Vgempty(client, true));
            effects.AddPredicate(new FastName("pickUpCassetteML_eff_3"), new Positionfree(p, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;

    }
}

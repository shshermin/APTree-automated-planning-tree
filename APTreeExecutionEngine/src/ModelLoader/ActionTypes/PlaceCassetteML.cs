using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class PlaceCassetteML : PActionNode
    {
        // Parameter: lp of type plate
        public Plate lp { get; private set; }

        // Parameter: mod of type cassette
        public Cassette mod { get; private set; }

        // Parameter: sp of type stackposition
        public Stackposition sp { get; private set; }

        // Parameter: client of type robot
        public Robot client { get; private set; }

        // Parameter: vg of type vacgripper
        public VacGripper vg { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public PlaceCassetteML(string actionType, string instanceName, Blackboard<FastName> blackboard, Plate lp, Cassette mod, Stackposition sp, Robot client, VacGripper vg)
            : base(actionType, instanceName, blackboard)
        {
            this.lp = lp;
            this.mod = mod;
            this.sp = sp;
            this.client = client;
            this.vg = vg;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("placeCassetteML_preconditions"));
            preconditions.AddPredicate(new FastName("placeCassetteML_pre_0"), new HasTool(client, vg, false));
            preconditions.AddPredicate(new FastName("placeCassetteML_pre_1"), new ActiveTool(vg, false));
            preconditions.AddPredicate(new FastName("placeCassetteML_pre_2"), new Holding(client, lp, false));
            preconditions.AddPredicate(new FastName("placeCassetteML_pre_3"), new AtAgent(client, sp, false));
            preconditions.AddPredicate(new FastName("placeCassetteML_pre_4"), new Belongstomodule(lp, mod, false));
            preconditions.AddPredicate(new FastName("placeCassetteML_pre_5"), new Positionfree(sp, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("placeCassetteML_effects"));
            effects.AddPredicate(new FastName("placeCassetteML_eff_0"), new Atplace(lp, sp, false));
            effects.AddPredicate(new FastName("placeCassetteML_eff_1"), new Holding(client, lp, true));
            effects.AddPredicate(new FastName("placeCassetteML_eff_2"), new Vgempty(client, false));
            effects.AddPredicate(new FastName("placeCassetteML_eff_3"), new Positionfree(sp, true));
            effects.AddPredicate(new FastName("placeCassetteML_eff_4"), new CassetteAtStack(mod, sp, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;

    }
}

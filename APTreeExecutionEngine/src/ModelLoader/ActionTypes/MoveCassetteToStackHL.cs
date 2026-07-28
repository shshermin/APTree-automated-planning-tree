using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class MoveCassetteToStackHL : PActionNode
    {
        public override ActionLevel Level => ActionLevel.HighLevel;

        // Parameter: tp of type plate (top plate)
        public Plate tp { get; private set; }

        // Parameter: lp of type plate (lower plate)
        public Plate lp { get; private set; }

        // Parameter: pr of type positiononrail
        public Location pr { get; private set; }

        // Parameter: sp of type stackposition
        public Stackposition sp { get; private set; }

        // Parameter: client of type robot
        public Robot client { get; private set; }

        // Parameter: mod of type cassette
        public Cassette mod { get; private set; }

        // Parameter: lay of type stack (layer)
        public Stack lay { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public MoveCassetteToStackHL(string actionType, string instanceName, Blackboard<FastName> blackboard, Plate tp, Plate lp, Location pr, Stackposition sp, Robot client, Cassette mod, Stack lay)
            : base(actionType, instanceName, blackboard)
        {
            this.tp = tp;
            this.lp = lp;
            this.pr = pr;
            this.sp = sp;
            this.client = client;
            this.mod = mod;
            this.lay = lay;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("moveCassetteToStackHL_preconditions"));
            preconditions.AddPredicate(new FastName("moveCassetteToStackHL_pre_0"), new Vgempty(client, false));
            preconditions.AddPredicate(new FastName("moveCassetteToStackHL_pre_1"), new Nailed(tp, false));
            preconditions.AddPredicate(new FastName("moveCassetteToStackHL_pre_2"), new Atplace(lp, pr, false));
            preconditions.AddPredicate(new FastName("moveCassetteToStackHL_pre_3"), new Belongstomodule(tp, mod, false));
            preconditions.AddPredicate(new FastName("moveCassetteToStackHL_pre_4"), new Belongstomodule(lp, mod, false));
            preconditions.AddPredicate(new FastName("moveCassetteToStackHL_pre_5"), new Allset(lay, mod, false));
            preconditions.AddPredicate(new FastName("moveCassetteToStackHL_pre_6"), new Positionfree(sp, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("moveCassetteToStackHL_effects"));
            effects.AddPredicate(new FastName("moveCassetteToStackHL_eff_0"), new CassetteAtStack(mod, sp, false));
            effects.AddPredicate(new FastName("moveCassetteToStackHL_eff_1"), new Positionfree(pr, false));
            effects.AddPredicate(new FastName("moveCassetteToStackHL_eff_2"), new Atplace(lp, pr, true));
            effects.AddPredicate(new FastName("moveCassetteToStackHL_eff_3"), new Atplace(lp, sp, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;

    }
}

using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class NailingBeamHL : PActionNode
    {
        public override ActionLevel Level => ActionLevel.HighLevel;

        // Parameter: obj of type element
        public Element obj { get; private set; }

        // Parameter: client of type robot
        public Robot client { get; private set; }

        // Parameter: mod of type cassette
        public Cassette mod { get; private set; }

        // Parameter: lay of type stack
        public Stack lay { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public NailingBeamHL(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj, Robot client, Cassette mod, Stack lay)
            : base(actionType, instanceName, blackboard)
        {
            this.obj = obj;
            this.client = client;
            this.mod = mod;
            this.lay = lay;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("nailingBeamHL_preconditions"));
            preconditions.AddPredicate(new FastName("nailingBeamHL_pre_0"), new Vgempty(client, false));
            preconditions.AddPredicate(new FastName("nailingBeamHL_pre_1"), new Clear(obj, false));
            preconditions.AddPredicate(new FastName("nailingBeamHL_pre_2"), new Nailed(obj, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("nailingBeamHL_effects"));
            effects.AddPredicate(new FastName("nailingBeamHL_eff_0"), new Nailed(obj, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;

    }
}

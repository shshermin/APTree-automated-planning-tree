using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class PlaceML : PActionNode
    {
        public override ActionLevel Level => ActionLevel.MidLevel;

        // Parameter: obj of type element
        public Element obj { get; private set; }

        // Parameter: placepos of type location
        public Location placepos { get; private set; }

        // Parameter: client of type robot
        public Robot client { get; private set; }

        // Parameter: vg of type vacuumGripper
        public VacGripper vg { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public PlaceML(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj, Location placepos, Robot client, VacGripper vg)
            : base(actionType, instanceName, blackboard)
        {
            this.obj = obj;
            this.placepos = placepos;
            this.client = client;
            this.vg = vg;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("placeML_preconditions"));
            preconditions.AddPredicate(new FastName("placeML_pre_0"), new Vgempty(client, true));
            preconditions.AddPredicate(new FastName("placeML_pre_1"), new Holding(client, obj, false));
            preconditions.AddPredicate(new FastName("placeML_pre_2"), new AtAgent(client, placepos, false));
            preconditions.AddPredicate(new FastName("placeML_pre_3"), new ActiveTool(vg, false));
            preconditions.AddPredicate(new FastName("placeML_pre_4"), new Clear(obj, true));
            preconditions.AddPredicate(new FastName("placeML_pre_5"), new Positionfree(placepos, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("placeML_effects"));
            effects.AddPredicate(new FastName("placeML_eff_0"), new Atplace(obj, placepos, false));
            effects.AddPredicate(new FastName("placeML_eff_1"), new Holding(client, obj, true));
            effects.AddPredicate(new FastName("placeML_eff_2"), new Vgempty(client, false));
            effects.AddPredicate(new FastName("placeML_eff_3"), new Clear(obj, false));
            effects.AddPredicate(new FastName("placeML_eff_4"), new Positionfree(placepos, true));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;

    }
}

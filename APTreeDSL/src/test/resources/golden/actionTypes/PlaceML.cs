using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class PlaceML : PActionNode
    {
        // Parameter: obj of type Element
        public Element obj { get; private set; }

        // Parameter: placepos of type Location
        public Location placepos { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: vg of type VacGripper
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
            preconditions.AddPredicate(new FastName("placeML_pre_0"), new vgempty(client, true));
            preconditions.AddPredicate(new FastName("placeML_pre_1"), new holding(client, obj, false));
            preconditions.AddPredicate(new FastName("placeML_pre_2"), new atAgent(client, placepos, false));
            preconditions.AddPredicate(new FastName("placeML_pre_3"), new activeTool(vg, false));
            preconditions.AddPredicate(new FastName("placeML_pre_4"), new clear(obj, true));
            preconditions.AddPredicate(new FastName("placeML_pre_5"), new positionfree(placepos, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("placeML_effects"));
            effects.AddPredicate(new FastName("placeML_eff_0"), new atplace(obj, placepos, false));
            effects.AddPredicate(new FastName("placeML_eff_1"), new holding(client, obj, true));
            effects.AddPredicate(new FastName("placeML_eff_2"), new vgempty(client, false));
            effects.AddPredicate(new FastName("placeML_eff_3"), new clear(obj, false));
            effects.AddPredicate(new FastName("placeML_eff_4"), new positionfree(placepos, true));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

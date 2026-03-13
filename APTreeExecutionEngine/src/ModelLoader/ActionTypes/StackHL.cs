using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class StackHL : PActionNode
    {
        // Parameter: stackingobject of type Element
        public Element stackingobject { get; private set; }

        // Parameter: existingobject of type Element
        public Element existingobject { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: objposition of type Location
        public Location objposition { get; private set; }

        // Parameter: g of type Gripper
        public Gripper g { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public StackHL(string actionType, string instanceName, Blackboard<FastName> blackboard, Element stackingobject, Element existingobject, Robot client, Location objposition, Gripper g)
            : base(actionType, instanceName, blackboard)
        {
            this.stackingobject = stackingobject;
            this.existingobject = existingobject;
            this.client = client;
            this.objposition = objposition;
            this.g = g;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("stackHL_preconditions"));
            preconditions.AddPredicate(new FastName("stackHL_pre_0"), new GripperEmpty(client, true));
            preconditions.AddPredicate(new FastName("stackHL_pre_1"), new Holding(client, stackingobject, false));
            preconditions.AddPredicate(new FastName("stackHL_pre_2"), new HasTool(client, g, false));
            preconditions.AddPredicate(new FastName("stackHL_pre_3"), new AtFinalPosition(existingobject, false));
            preconditions.AddPredicate(new FastName("stackHL_pre_4"), new AtPlace(stackingobject, objposition, true));
            preconditions.AddPredicate(new FastName("stackHL_pre_5"), new ObjectFinalPosition(stackingobject, objposition, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("stackHL_effects"));
            effects.AddPredicate(new FastName("stackHL_eff_0"), new Holding(client, stackingobject, true));
            effects.AddPredicate(new FastName("stackHL_eff_1"), new AtFinalPosition(stackingobject, false));
            effects.AddPredicate(new FastName("stackHL_eff_2"), new AtPlace(stackingobject, objposition, false));
            effects.AddPredicate(new FastName("stackHL_eff_3"), new GripperEmpty(client, false));
            effects.AddPredicate(new FastName("stackHL_eff_4"), new Clear(stackingobject, false));
            effects.AddPredicate(new FastName("stackHL_eff_5"), new Accessible(existingobject, true));
            effects.AddPredicate(new FastName("stackHL_eff_6"), new Accessible(stackingobject, false));
            effects.AddPredicate(new FastName("stackHL_eff_7"), new Stacked(stackingobject, existingobject, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

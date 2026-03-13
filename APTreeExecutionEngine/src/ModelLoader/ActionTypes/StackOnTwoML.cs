using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    public class StackOnTwoML : PActionNode
    {
        // Parameter: stackingobj of type Element
        public Element stackingobj { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: objposition of type Location
        public Location objposition { get; private set; }

        // Parameter: robotpos of type RobotPosition
        public RobotPosition robotpos { get; private set; }

        // Parameter: existingobj1 of type Element
        public Element existingobj1 { get; private set; }

        // Parameter: existingobj2 of type Element
        public Element existingobj2 { get; private set; }

        // Parameter: vg of type Gripper
        public Gripper vg { get; private set; }

        // Parameter: layer1 of type Stack
        public Stack layer1 { get; private set; }

        // Parameter: layer2 of type Stack
        public Stack layer2 { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public StackOnTwoML(string actionType, string instanceName, Blackboard<FastName> blackboard, Element stackingobj, Robot client, Location objposition, RobotPosition robotpos, Element existingobj1, Element existingobj2, Gripper vg, Stack layer1, Stack layer2)
            : base(actionType, instanceName, blackboard)
        {
            this.stackingobj = stackingobj;
            this.client = client;
            this.objposition = objposition;
            this.robotpos = robotpos;
            this.existingobj1 = existingobj1;
            this.existingobj2 = existingobj2;
            this.vg = vg;
            this.layer1 = layer1;
            this.layer2 = layer2;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("stackOnTwoML_preconditions"));
            preconditions.AddPredicate(new FastName("stackOnTwoML_pre_0"), new GripperEmpty(client, true));
            preconditions.AddPredicate(new FastName("stackOnTwoML_pre_1"), new Holding(client, stackingobj, false));
            preconditions.AddPredicate(new FastName("stackOnTwoML_pre_2"), new AtFinalPosition(existingobj2, false));
            preconditions.AddPredicate(new FastName("stackOnTwoML_pre_3"), new AtFinalPosition(existingobj1, false));
            preconditions.AddPredicate(new FastName("stackOnTwoML_pre_4"), new HasTool(client, vg, false));
            preconditions.AddPredicate(new FastName("stackOnTwoML_pre_5"), new AtAgent(client, robotpos, false));
            preconditions.AddPredicate(new FastName("stackOnTwoML_pre_6"), new Clear(existingobj1, false));
            preconditions.AddPredicate(new FastName("stackOnTwoML_pre_7"), new Clear(existingobj2, false));
            preconditions.AddPredicate(new FastName("stackOnTwoML_pre_8"), new BelongsToLayer(stackingobj, layer2, false));
            preconditions.AddPredicate(new FastName("stackOnTwoML_pre_9"), new BelongsToLayer(existingobj1, layer1, false));
            preconditions.AddPredicate(new FastName("stackOnTwoML_pre_10"), new BelongsToLayer(existingobj2, layer1, false));
            preconditions.AddPredicate(new FastName("stackOnTwoML_pre_11"), new ObjectFinalPosition(stackingobj, objposition, false));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("stackOnTwoML_effects"));
            effects.AddPredicate(new FastName("stackOnTwoML_eff_0"), new Holding(client, stackingobj, true));
            effects.AddPredicate(new FastName("stackOnTwoML_eff_1"), new AtPlace(stackingobj, objposition, false));
            effects.AddPredicate(new FastName("stackOnTwoML_eff_2"), new GripperEmpty(client, false));
            effects.AddPredicate(new FastName("stackOnTwoML_eff_3"), new Clear(stackingobj, false));
            effects.AddPredicate(new FastName("stackOnTwoML_eff_4"), new Accessible(stackingobj, false));
            effects.AddPredicate(new FastName("stackOnTwoML_eff_5"), new Stacked(stackingobj, existingobj1, false));
            effects.AddPredicate(new FastName("stackOnTwoML_eff_6"), new Stacked(stackingobj, existingobj2, false));
            effects.AddPredicate(new FastName("stackOnTwoML_eff_7"), new AtFinalPosition(stackingobj, false));
            effects.AddPredicate(new FastName("stackOnTwoML_eff_8"), new Accessible(existingobj1, true));
            effects.AddPredicate(new FastName("stackOnTwoML_eff_9"), new Accessible(existingobj2, true));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

using System;
using System.Collections.Generic;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;

namespace BehaviorTreeMainProject
{
    /// <summary>
    /// ML action: robot puts a currently held object down at a free first-position
    /// (staging location). Symmetric to PickUpML — enables the planner to perform
    /// rearrangement, e.g. setting a blocking object aside to access another one
    /// underneath.
    /// </summary>
    public class PutDownML : PActionNode
    {
        // Parameter: obj of type Element
        public Element obj { get; private set; }

        // Parameter: p of type Location (typed as firstposition in PDDL)
        public Location p { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: vg of type Gripper
        public Gripper vg { get; private set; }

        // Parameter: rp of type RobotPosition (typed as rppickup in PDDL)
        public RobotPosition rp { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public PutDownML(string actionType, string instanceName, Blackboard<FastName> blackboard,
                         Element obj, Location p, Robot client, Gripper vg, RobotPosition rp)
            : base(actionType, instanceName, blackboard)
        {
            this.obj = obj;
            this.p = p;
            this.client = client;
            this.vg = vg;
            this.rp = rp;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Preconditions (isNegated=false means the positive literal must hold)
            preconditions = new State(StateType.Precondition, new FastName("putDownML_preconditions"));
            preconditions.AddPredicate(new FastName("putDownML_pre_0"), new HasTool(client, vg, false));
            preconditions.AddPredicate(new FastName("putDownML_pre_1"), new Holding(client, obj, false));
            preconditions.AddPredicate(new FastName("putDownML_pre_2"), new AtAgent(client, rp, false));
            preconditions.AddPredicate(new FastName("putDownML_pre_3"), new PositionFree(p, false));

            // Effects (isNegated=true means the literal becomes false after execution)
            effects = new State(StateType.Effect, new FastName("putDownML_effects"));
            effects.AddPredicate(new FastName("putDownML_eff_0"), new Holding(client, obj, true));
            effects.AddPredicate(new FastName("putDownML_eff_1"), new AtPlace(obj, p, false));
            effects.AddPredicate(new FastName("putDownML_eff_2"), new PositionFree(p, true));
            effects.AddPredicate(new FastName("putDownML_eff_3"), new GripperEmpty(client, false));
            effects.AddPredicate(new FastName("putDownML_eff_4"), new Clear(obj, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

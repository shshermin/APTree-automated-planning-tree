using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;
using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject
{
    public class UnstackML : PActionNode
    {
        public Element       stackingobject { get; private set; }
        public Element       existingobject { get; private set; }
        public Robot         client         { get; private set; }
        public Gripper       gripper        { get; private set; }
        public Location      objposition    { get; private set; }
        public RobotPosition robotposition  { get; private set; }

        private State preconditions;
        private State effects;

        public UnstackML(
            string actionType,
            string instanceName,
            Blackboard<FastName> blackboard,
            Element       stackingobject,
            Element       existingobject,
            Robot         client,
            Gripper       gripper,
            Location      objposition,
            RobotPosition robotposition)
            : base(actionType, instanceName, blackboard)
        {
            this.stackingobject = stackingobject;
            this.existingobject = existingobject;
            this.client         = client;
            this.gripper        = gripper;
            this.objposition    = objposition;
            this.robotposition  = robotposition;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            preconditions = new State(StateType.Precondition, new FastName("unstackML_preconditions"));
            preconditions.AddPredicate(new FastName("unstackML_pre_0"), new GripperEmpty(client, false));
            preconditions.AddPredicate(new FastName("unstackML_pre_1"), new AtAgent(client, robotposition, false));
            preconditions.AddPredicate(new FastName("unstackML_pre_2"), new HasTool(client, gripper, false));
            preconditions.AddPredicate(new FastName("unstackML_pre_3"), new AtPlace(stackingobject, objposition, false));
            preconditions.AddPredicate(new FastName("unstackML_pre_4"), new Stacked(stackingobject, existingobject, false));
            preconditions.AddPredicate(new FastName("unstackML_pre_5"), new Clear(stackingobject, false));
            preconditions.AddPredicate(new FastName("unstackML_pre_6"), new Accessible(stackingobject, false));

            effects = new State(StateType.Effect, new FastName("unstackML_effects"));
            effects.AddPredicate(new FastName("unstackML_eff_0"), new Holding(client, stackingobject, false));
            effects.AddPredicate(new FastName("unstackML_eff_1"), new AtFinalPosition(stackingobject, true));          // NOT atfinalposition
            effects.AddPredicate(new FastName("unstackML_eff_2"), new AtPlace(stackingobject, objposition, true));     // NOT atplace
            effects.AddPredicate(new FastName("unstackML_eff_3"), new GripperEmpty(client, true));                    // NOT gripperempty
            effects.AddPredicate(new FastName("unstackML_eff_4"), new Clear(stackingobject, true));                   // NOT clear(stackingobject)
            effects.AddPredicate(new FastName("unstackML_eff_5"), new Accessible(stackingobject, true));              // NOT accessible(stackingobject)
            effects.AddPredicate(new FastName("unstackML_eff_6"), new Accessible(existingobject, false));
            effects.AddPredicate(new FastName("unstackML_eff_7"), new Clear(existingobject, false));
            effects.AddPredicate(new FastName("unstackML_eff_8"), new Stacked(stackingobject, existingobject, true)); // NOT stacked
        }

        protected override State Preconditions => preconditions;
        protected override State Effects       => effects;
    }
}

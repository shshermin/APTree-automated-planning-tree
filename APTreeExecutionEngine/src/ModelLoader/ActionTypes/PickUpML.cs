using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;
using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject
{
    public class PickUpML : PActionNode
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
        private const uint MB_OKCANCEL = 0x00000001;
        private const uint MB_ICONWARNING = 0x00000030;
        private const uint MB_TOPMOST = 0x00040000;
        private const int IDOK = 1;
        // Parameter: obj of type Element
        public Element obj { get; private set; }

        // Parameter: p of type Location
        public Location p { get; private set; }

        // Parameter: client of type Robot
        public Robot client { get; private set; }

        // Parameter: vg of type Gripper
        public Gripper vg { get; private set; }

        // Parameter: rp of type RobotPosition
        public RobotPosition rp { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public PickUpML(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj, Location p, Robot client, Gripper vg, RobotPosition rp)
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
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("pickUpML_preconditions"));
            preconditions.AddPredicate(new FastName("pickUpML_pre_0"), new HasTool(client, vg, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_1"), new AtPlace(obj, p, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_2"), new AtAgent(client, rp, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_3"), new GripperEmpty(client, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_4"), new PositionFree(p, true));
            preconditions.AddPredicate(new FastName("pickUpML_pre_5"), new Clear(obj, false));
            preconditions.AddPredicate(new FastName("pickUpML_pre_6"), new Fixed(obj, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("pickUpML_effects"));
            effects.AddPredicate(new FastName("pickUpML_eff_0"), new Holding(client, obj, false));
            effects.AddPredicate(new FastName("pickUpML_eff_1"), new AtPlace(obj, p, true));
            effects.AddPredicate(new FastName("pickUpML_eff_2"), new GripperEmpty(client, true));
            effects.AddPredicate(new FastName("pickUpML_eff_3"), new Clear(obj, true));
            effects.AddPredicate(new FastName("pickUpML_eff_4"), new PositionFree(p, false));
        }

        protected override void OnEnter()
        {
            base.OnEnter();

            // If preconditions already failed, don't show dialog
            if (status == BTNodeResult.Failure)
                return;

            // Only show confirmation dialog when execution is active
            bool executionActive = false;
            try { executionActive = blackboard.GetBool(new FastName("ExecutionActive")); } catch { }

            if (!executionActive)
                return;

            var message = $"About to execute PickUpML:\n\n" +
                          $"  Instance: {InstanceName}\n" +
                          $"  Object:   {obj?.ID}\n" +
                          $"  Location: {p?.ID}\n" +
                          $"  Robot:    {client?.ID}\n\n" +
                          $"Press OK to proceed, Cancel to abort.";

            LoggingService.LogInfo($"⏸️ PickUpML: Waiting for operator confirmation for '{InstanceName}'");

            int result = MessageBoxW(IntPtr.Zero, message, $"Confirm: PickUpML", MB_OKCANCEL | MB_ICONWARNING | MB_TOPMOST);

            if (result != IDOK)
            {
                LoggingService.LogWarning($"🛑 PickUpML: Operator CANCELLED '{InstanceName}'");
                status = BTNodeResult.Failure;
                return;
            }

            LoggingService.LogSuccess($"▶️ PickUpML: Operator confirmed '{InstanceName}' — proceeding");
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;
    }
}

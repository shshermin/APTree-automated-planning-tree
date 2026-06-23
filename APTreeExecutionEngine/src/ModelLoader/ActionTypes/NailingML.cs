using System;
using System.Collections.Generic;
using System.Linq;
using ModelLoader.ParameterTypes;
using ModelLoader.PredicateTypes;
using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject
{
    public class NailingML : PActionNode
    {
        // Parameter: obj of type element
        public Element obj { get; private set; }

        // Parameter: pos of type positionOnRail
        public PositionOnRail pos { get; private set; }

        // Parameter: client of type robot
        public Robot client { get; private set; }

        // Parameter: ng of type nailGripper
        public NailGripper ng { get; private set; }

        // Preconditions and Effects as State objects
        private State preconditions;
        private State effects;

        public NailingML(string actionType, string instanceName, Blackboard<FastName> blackboard, Element obj, PositionOnRail pos, Robot client, NailGripper ng)
            : base(actionType, instanceName, blackboard)
        {
            this.obj = obj;
            this.pos = pos;
            this.client = client;
            this.ng = ng;
            InitializePredicates();
        }

        private void InitializePredicates()
        {
            // Initialize preconditions
            preconditions = new State(StateType.Precondition, new FastName("nailingML_preconditions"));
            preconditions.AddPredicate(new FastName("nailingML_pre_0"), new AtAgent(client, pos, false));
            preconditions.AddPredicate(new FastName("nailingML_pre_1"), new Atplace(obj, pos, false));
            preconditions.AddPredicate(new FastName("nailingML_pre_2"), new ActiveTool(ng, false));
            preconditions.AddPredicate(new FastName("nailingML_pre_3"), new Nailed(obj, true));

            // Initialize effects
            effects = new State(StateType.Effect, new FastName("nailingML_effects"));
            effects.AddPredicate(new FastName("nailingML_eff_0"), new Nailed(obj, false));
        }

        protected override State Preconditions => preconditions;
        protected override State Effects => effects;

        protected override void OnAfterApplyEffects()
        {
            var name = obj?.NameKey?.ToString() ?? "";
            if (!(obj is Plate) || !name.StartsWith("tp")) return;

            var number = name.Substring(2); // "tp1" → "1"
            var allPredicates = blackboard.GetAllPredicates();

            var lay = allPredicates.OfType<Belongstolayer>()
                .FirstOrDefault(p => p.lay?.NameKey?.ToString() == "lay" + number)?.lay;
            var mod = allPredicates.OfType<Belongstomodule>()
                .FirstOrDefault(p => p.mod?.NameKey?.ToString() == "m" + number)?.mod;

            if (lay == null || mod == null) return;

            var allsetPred = new Allset(lay, mod, false);
            blackboard.SetPredicateSync(allsetPred.PredicateName, allsetPred);
            LoggingService.LogInfo($"✅ NailingML: {name} nailed → (allset {lay.NameKey} {mod.NameKey})");
        }

    }
}

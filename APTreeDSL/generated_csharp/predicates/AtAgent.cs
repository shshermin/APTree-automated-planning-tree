using System;
using System.Collections.Generic;
using BehaviorTree.Types;

namespace BehaviorTree.Predicates {

    public class AtAgent : Predicate {
        public bool not { get; set; }
        public Agent agent { get; set; }
        public Location location { get; set; }

        public AtAgent(Agent agent, Location location, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("atagent");
            this.agent = agent;
            this.location = location;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetPDDLParameterValues()
        {
            return new List<string>
            {
                agent?.NameKey?.ToString() ?? "null",
                location?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

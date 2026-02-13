using System;
using System.Collections.Generic;
using BehaviorTree.Types;

namespace BehaviorTree.Predicates {

    public class Holding : Predicate {
        public bool not { get; set; }
        public Element item { get; set; }
        public Agent agent { get; set; }

        public Holding(Element item, Agent agent, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("holding");
            this.item = item;
            this.agent = agent;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                item?.NameKey?.ToString() ?? "null",
                agent?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

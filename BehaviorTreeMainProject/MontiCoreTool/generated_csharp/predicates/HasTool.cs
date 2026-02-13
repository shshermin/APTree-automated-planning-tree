using System;
using System.Collections.Generic;
using BehaviorTree.Types;

namespace BehaviorTree.Predicates {

    public class HasTool : Predicate {
        public bool not { get; set; }
        public Agent agent { get; set; }
        public Tool tool { get; set; }

        public HasTool(Agent agent, Tool tool, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("hastool");
            this.agent = agent;
            this.tool = tool;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                agent?.NameKey?.ToString() ?? "null",
                tool?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

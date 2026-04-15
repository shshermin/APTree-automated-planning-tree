using System;
using System.Collections.Generic;
using BehaviorTree.Types;

namespace BehaviorTree.Predicates {

    public class PositionFree : Predicate {
        public bool not { get; set; }
        public Location pos { get; set; }

        public PositionFree(Location pos, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("positionfree");
            this.pos = pos;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetPDDLParameterValues()
        {
            return new List<string>
            {
                pos?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

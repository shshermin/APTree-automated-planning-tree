using System;
using System.Collections.Generic;
using BehaviorTree.Types;

namespace BehaviorTree.Predicates {

    public class AtPlace : Predicate {
        public bool not { get; set; }
        public Element item { get; set; }
        public Location loc { get; set; }

        public AtPlace(Element item, Location loc, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("atplace");
            this.item = item;
            this.loc = loc;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetPDDLParameterValues()
        {
            return new List<string>
            {
                item?.NameKey?.ToString() ?? "null",
                loc?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

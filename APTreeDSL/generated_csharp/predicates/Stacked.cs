using System;
using System.Collections.Generic;
using BehaviorTree.Types;

namespace BehaviorTree.Predicates {

    public class Stacked : Predicate {
        public bool not { get; set; }
        public Element myObject { get; set; }

        public Stacked(Element myObject, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("stacked");
            this.myObject = myObject;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetPDDLParameterValues()
        {
            return new List<string>
            {
                myObject?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

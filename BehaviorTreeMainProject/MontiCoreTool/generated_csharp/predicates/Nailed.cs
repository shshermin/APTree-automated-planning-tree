using System;
using System.Collections.Generic;
using BehaviorTree.Types;

namespace BehaviorTree.Predicates {

    public class Nailed : Predicate {
        public bool not { get; set; }
        public Element myObject { get; set; }

        public Nailed(Element myObject, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("nailed");
            this.myObject = myObject;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                myObject?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

using System;
using System.Collections.Generic;
using BehaviorTree.Types;

namespace BehaviorTree.Predicates {

    public class Clear : Predicate {
        public bool not { get; set; }
        public Element myObject { get; set; }

        public Clear(Element myObject, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("clear");
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

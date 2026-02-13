using System;
using System.Collections.Generic;
using BehaviorTree.Types;

namespace BehaviorTree.Predicates {

    public class Glued : Predicate {
        public bool not { get; set; }
        public Element myObject { get; set; }

        public Glued(Element myObject, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("glued");
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

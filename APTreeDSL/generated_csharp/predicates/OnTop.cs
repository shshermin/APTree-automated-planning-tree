using System;
using System.Collections.Generic;
using BehaviorTree.Types;

namespace BehaviorTree.Predicates {

    public class OnTop : Predicate {
        public bool not { get; set; }
        public Element myObject1 { get; set; }
        public Element myObject2 { get; set; }

        public OnTop(Element myObject1, Element myObject2, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("ontop");
            this.myObject1 = myObject1;
            this.myObject2 = myObject2;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                myObject1?.NameKey?.ToString() ?? "null",
                myObject2?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

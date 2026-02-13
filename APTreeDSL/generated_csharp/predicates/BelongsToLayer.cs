using System;
using System.Collections.Generic;
using BehaviorTree.Types;

namespace BehaviorTree.Predicates {

    public class BelongsToLayer : Predicate {
        public bool not { get; set; }
        public Element myObject { get; set; }
        public Layer lay { get; set; }

        public BelongsToLayer(Element myObject, Layer lay, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("belongstolayer");
            this.myObject = myObject;
            this.lay = lay;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                myObject?.NameKey?.ToString() ?? "null",
                lay?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

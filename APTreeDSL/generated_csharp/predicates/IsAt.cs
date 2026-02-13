using System;
using System.Collections.Generic;
using BehaviorTree.Types;

namespace BehaviorTree.Predicates {

    public class IsAt : Predicate {
        public bool not { get; set; }
        public Element myObject { get; set; }
        public Location location { get; set; }

        public IsAt(Element myObject, Location location, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("isat");
            this.myObject = myObject;
            this.location = location;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                myObject?.NameKey?.ToString() ?? "null",
                location?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

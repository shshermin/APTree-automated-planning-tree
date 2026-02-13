using System;
using System.Collections.Generic;
using BehaviorTree.Types;

namespace BehaviorTree.Predicates {

    public class AllSet : Predicate {
        public bool not { get; set; }
        public Layer lay { get; set; }
        public Module mod { get; set; }

        public AllSet(Layer lay, Module mod, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("allset");
            this.lay = lay;
            this.mod = mod;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                lay?.NameKey?.ToString() ?? "null",
                mod?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

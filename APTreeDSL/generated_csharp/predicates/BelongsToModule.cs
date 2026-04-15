using System;
using System.Collections.Generic;
using BehaviorTree.Types;

namespace BehaviorTree.Predicates {

    public class BelongsToModule : Predicate {
        public bool not { get; set; }
        public Element myObject { get; set; }
        public Module mod { get; set; }

        public BelongsToModule(Element myObject, Module mod, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("belongstomodule");
            this.myObject = myObject;
            this.mod = mod;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetPDDLParameterValues()
        {
            return new List<string>
            {
                myObject?.NameKey?.ToString() ?? "null",
                mod?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

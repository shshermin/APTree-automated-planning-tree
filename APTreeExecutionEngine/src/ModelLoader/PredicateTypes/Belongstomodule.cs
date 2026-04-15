using System;

namespace ModelLoader.PredicateTypes
{
    public class BelongsToModule : Predicate
    {
        public Element obj { get; set; }
        public Module mod { get; set; }

        public BelongsToModule(Element obj, Module mod, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("belongstomodule");
            this.obj = obj;
            this.mod = mod;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetPDDLParameterValues()
        {
            return new List<string>
            {
                obj?.NameKey?.ToString() ?? "null",
                mod?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

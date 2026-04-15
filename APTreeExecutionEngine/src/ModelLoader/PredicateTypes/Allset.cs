using System;

namespace ModelLoader.PredicateTypes
{
    public class AllSet : Predicate
    {
        public Layer lay { get; set; }
        public Module mod { get; set; }

        public AllSet(Layer lay, Module mod, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("allset");
            this.lay = lay;
            this.mod = mod;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetPDDLParameterValues()
        {
            return new List<string>
            {
                lay?.NameKey?.ToString() ?? "null",
                mod?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

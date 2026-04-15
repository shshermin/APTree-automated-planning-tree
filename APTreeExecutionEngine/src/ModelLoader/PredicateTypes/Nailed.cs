using System;

namespace ModelLoader.PredicateTypes
{
    public class Nailed : Predicate
    {
        public Element obj1 { get; set; }
        public Element obj2 { get; set; }
        public Location nailloc { get; set; }

        public Nailed(Element obj1, Element obj2, bool isNegated, Location nailloc = null) : base(isNegated)
        {
            PredicateType = new FastName("nailed");
            this.obj1 = obj1;
            this.obj2 = obj2;
            this.nailloc = nailloc;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetPDDLParameterValues()
        {
            return new List<string>
            {
                obj1?.NameKey?.ToString() ?? "null",
                obj2?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

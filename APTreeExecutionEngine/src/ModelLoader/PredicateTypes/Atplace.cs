using System;

namespace ModelLoader.PredicateTypes
{
    public class Atplace : Predicate
    {
        public Element item { get; set; }
        public Location loc { get; set; }

        public Atplace(Element item, Location loc, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("atplace");
            this.item = item;
            this.loc = loc;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                item?.NameKey?.ToString() ?? "null",
                loc?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

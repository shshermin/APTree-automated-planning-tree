using System;

namespace ModelLoader.PredicateTypes
{
    public class AtPlace : Predicate
    {
        public Element obj { get; set; }
        public Location objLoc { get; set; }

        public AtPlace(Element obj, Location objLoc, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("atplace");
            this.obj = obj;
            this.objLoc = objLoc;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                obj?.NameKey?.ToString() ?? "null",
                objLoc?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

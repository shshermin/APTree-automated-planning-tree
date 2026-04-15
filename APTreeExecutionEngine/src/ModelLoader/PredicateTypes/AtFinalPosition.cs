using System;

namespace ModelLoader.PredicateTypes
{
    public class AtFinalPosition : Predicate
    {
        public Element obj { get; set; }

        public AtFinalPosition(Element obj, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("atfinalposition");
            this.obj = obj;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetPDDLParameterValues()
        {
            return new List<string>
            {
                obj?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

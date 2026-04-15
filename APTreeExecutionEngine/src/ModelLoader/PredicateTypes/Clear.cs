using System;

namespace ModelLoader.PredicateTypes
{
    public class Clear : Predicate
    {
        public Element obj { get; set; }

        public Clear(Element obj, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("clear");
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

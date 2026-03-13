using System;

namespace ModelLoader.PredicateTypes
{
    public class Accessible : Predicate
    {
        public Element obj { get; set; }

        public Accessible(Element obj, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("accessible");
            this.obj = obj;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                obj?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

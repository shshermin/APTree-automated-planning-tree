using System;

namespace ModelLoader.PredicateTypes
{
    public class Fixed : Predicate
    {
        public Element obj { get; set; }

        public Fixed(Element obj, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("fixed");
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

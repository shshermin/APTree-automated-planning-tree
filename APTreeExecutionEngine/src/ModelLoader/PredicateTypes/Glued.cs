using System;

namespace ModelLoader.PredicateTypes
{
    public class Glued : Predicate
    {
        public Element obj { get; set; }

        public Glued(Element obj, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("glued");
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

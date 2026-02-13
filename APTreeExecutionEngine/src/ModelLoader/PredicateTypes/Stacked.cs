using System;

namespace ModelLoader.PredicateTypes
{
    public class Stacked : Predicate
    {
        public Element myObject { get; set; }

        public Stacked(Element myObject, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("stacked");
            this.myObject = myObject;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                myObject?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

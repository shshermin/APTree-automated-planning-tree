using System;

namespace ModelLoader.PredicateTypes
{
    public class Nailed : Predicate
    {
        public Element myObject { get; set; }

        public Nailed(Element myObject, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("nailed");
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

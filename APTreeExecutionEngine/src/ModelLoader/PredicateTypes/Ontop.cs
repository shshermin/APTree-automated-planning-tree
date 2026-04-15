using System;

namespace ModelLoader.PredicateTypes
{
    public class Ontop : Predicate
    {
        public Element myObject1 { get; set; }
        public Element myObject2 { get; set; }

        public Ontop(Element myObject1, Element myObject2, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("ontop");
            this.myObject1 = myObject1;
            this.myObject2 = myObject2;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetPDDLParameterValues()
        {
            return new List<string>
            {
                myObject1?.NameKey?.ToString() ?? "null",
                myObject2?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

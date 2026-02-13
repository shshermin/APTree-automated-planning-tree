using System;

namespace ModelLoader.PredicateTypes
{
    public class Belongstolayer : Predicate
    {
        public Element myObject { get; set; }
        public Layer lay { get; set; }

        public Belongstolayer(Element myObject, Layer lay, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("belongstolayer");
            this.myObject = myObject;
            this.lay = lay;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                myObject?.NameKey?.ToString() ?? "null",
                lay?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

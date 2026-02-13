using System;

namespace ModelLoader.PredicateTypes
{
    public class Glued : Predicate
    {
        public Element myObject { get; set; }

        public Glued(Element myObject, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("glued");
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

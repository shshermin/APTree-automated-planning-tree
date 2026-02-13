using System;

namespace ModelLoader.PredicateTypes
{
    public class Atplace : Predicate
    {
        public Element myObject { get; set; }
        public Location place { get; set; }

        public Atplace(Element myObject, Location place, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("atplace");
            this.myObject = myObject;
            this.place = place;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                myObject?.NameKey?.ToString() ?? "null",
                place?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

using System;

namespace ModelLoader.PredicateTypes
{
    public class IsAt : Predicate
    {
        public Element myObject { get; set; }
        public Location location { get; set; }

        public IsAt(Element myObject, Location location, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("isAt");
            this.myObject = myObject;
            this.location = location;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetPDDLParameterValues()
        {
            return new List<string>
            {
                myObject?.NameKey?.ToString() ?? "null",
                location?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

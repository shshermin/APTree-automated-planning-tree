using System;

namespace ModelLoader.PredicateTypes
{
    public class ObjectFinalPosition : Predicate
    {
        public Element obj { get; set; }
        public Location pos { get; set; }

        public ObjectFinalPosition(Element obj, Location pos, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("objectfinalposition");
            this.obj = obj;
            this.pos = pos;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                obj?.NameKey?.ToString() ?? "null",
                pos?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

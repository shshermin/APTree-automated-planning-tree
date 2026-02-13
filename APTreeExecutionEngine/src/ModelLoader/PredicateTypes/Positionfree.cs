using System;

namespace ModelLoader.PredicateTypes
{
    public class Positionfree : Predicate
    {
        public Location pos { get; set; }

        public Positionfree(Location pos, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("positionfree");
            this.pos = pos;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                pos?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

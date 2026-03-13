using System;

namespace ModelLoader.PredicateTypes
{
    public class PositionFree : Predicate
    {
        public Location loc { get; set; }

        public PositionFree(Location loc, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("positionfree");
            this.loc = loc;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                loc?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

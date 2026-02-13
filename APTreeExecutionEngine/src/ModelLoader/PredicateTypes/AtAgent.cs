using System;

namespace ModelLoader.PredicateTypes
{
    public class AtAgent : Predicate
    {
        public Agent agent { get; set; }
        public Location location { get; set; }

        public AtAgent(Agent agent, Location location, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("atAgent");
            this.agent = agent;
            this.location = location;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                agent?.NameKey?.ToString() ?? "null",
                location?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

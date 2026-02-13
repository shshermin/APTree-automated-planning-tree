using System;

namespace ModelLoader.PredicateTypes
{
    public class Robotequipped : Predicate
    {
        public Agent client { get; set; }

        public Robotequipped(Agent client, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("robotequipped");
            this.client = client;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                client?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

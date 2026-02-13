using System;

namespace ModelLoader.PredicateTypes
{
    public class Vgempty : Predicate
    {
        public Agent client { get; set; }

        public Vgempty(Agent client, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("vgempty");
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

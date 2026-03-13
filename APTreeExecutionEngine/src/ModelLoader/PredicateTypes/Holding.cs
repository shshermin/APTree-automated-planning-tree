using System;

namespace ModelLoader.PredicateTypes
{
    public class Holding : Predicate
    {
        public Agent client { get; set; }
        public Element obj { get; set; }

        public Holding(Agent client, Element obj, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("holding");
            this.client = client;
            this.obj = obj;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                client?.NameKey?.ToString() ?? "null",
                obj?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

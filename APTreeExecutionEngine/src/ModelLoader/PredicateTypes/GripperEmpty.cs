using System;

namespace ModelLoader.PredicateTypes
{
    public class GripperEmpty : Predicate
    {
        public Agent client { get; set; }

        public GripperEmpty(Agent client, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("gripperempty");
            this.client = client;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetPDDLParameterValues()
        {
            return new List<string>
            {
                client?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

using System;

namespace ModelLoader.PredicateTypes
{
    public class AtAgent : Predicate
    {
        public Agent client { get; set; }
        public Location agentLoc { get; set; }

        public AtAgent(Agent client, Location agentLoc, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("atagent");
            this.client = client;
            this.agentLoc = agentLoc;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetPDDLParameterValues()
        {
            return new List<string>
            {
                client?.NameKey?.ToString() ?? "null",
                agentLoc?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

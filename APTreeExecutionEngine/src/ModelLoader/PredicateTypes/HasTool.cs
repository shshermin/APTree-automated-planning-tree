using System;

namespace ModelLoader.PredicateTypes
{
    public class HasTool : Predicate
    {
        public Agent client { get; set; }
        public Tool tool { get; set; }

        public HasTool(Agent client, Tool tool, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("hastool");
            this.client = client;
            this.tool = tool;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                client?.NameKey?.ToString() ?? "null",
                tool?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

using System;

namespace ModelLoader.PredicateTypes
{
    public class HasTool : Predicate
    {
        public Agent agent { get; set; }
        public Tool tool { get; set; }

        public HasTool(Agent agent, Tool tool, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("hasTool");
            this.agent = agent;
            this.tool = tool;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                agent?.NameKey?.ToString() ?? "null",
                tool?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

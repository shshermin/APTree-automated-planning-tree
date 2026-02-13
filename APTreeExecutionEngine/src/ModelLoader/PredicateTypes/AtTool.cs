using System;

namespace ModelLoader.PredicateTypes
{
    public class AtTool : Predicate
    {
        public Tool tool { get; set; }
        public Location loc { get; set; }

        public AtTool(Tool tool, Location loc, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("atTool");
            this.tool = tool;
            this.loc = loc;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                tool?.NameKey?.ToString() ?? "null",
                loc?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

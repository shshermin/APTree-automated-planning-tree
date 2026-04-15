using System;

namespace ModelLoader.PredicateTypes
{
    public class AtTool : Predicate
    {
        public Tool tool { get; set; }
        public Location toolLoc { get; set; }

        public AtTool(Tool tool, Location toolLoc, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("attool");
            this.tool = tool;
            this.toolLoc = toolLoc;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetPDDLParameterValues()
        {
            return new List<string>
            {
                tool?.NameKey?.ToString() ?? "null",
                toolLoc?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

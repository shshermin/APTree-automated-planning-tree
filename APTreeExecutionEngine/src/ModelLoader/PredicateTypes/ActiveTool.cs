using System;

namespace ModelLoader.PredicateTypes
{
    public class ActiveTool : Predicate
    {
        public Tool tool { get; set; }

        public ActiveTool(Tool tool, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("activetool");
            this.tool = tool;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                tool?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

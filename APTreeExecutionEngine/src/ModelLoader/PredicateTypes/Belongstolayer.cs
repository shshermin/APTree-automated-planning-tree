using System;

namespace ModelLoader.PredicateTypes
{
    public class BelongsToLayer : Predicate
    {
        public Element obj { get; set; }
        public Layer lay { get; set; }

        public BelongsToLayer(Element obj, Layer lay, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("belongstolayer");
            this.obj = obj;
            this.lay = lay;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetPDDLParameterValues()
        {
            return new List<string>
            {
                obj?.NameKey?.ToString() ?? "null",
                lay?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

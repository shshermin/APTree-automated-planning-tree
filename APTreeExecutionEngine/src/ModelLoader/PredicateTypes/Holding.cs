using System;

namespace ModelLoader.PredicateTypes
{
    public class Holding : Predicate
    {
        public Agent agent { get; set; }
        public Element myObject { get; set; }

        public Holding(Agent agent, Element myObject, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("holding");
            this.agent = agent;
            this.myObject = myObject;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                agent?.NameKey?.ToString() ?? "null",
                myObject?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

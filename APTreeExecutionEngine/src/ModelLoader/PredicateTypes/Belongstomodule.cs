using System;

namespace ModelLoader.PredicateTypes
{
    public class Belongstomodule : Predicate
    {
        public Element myObject { get; set; }
        public Module mod { get; set; }

        public Belongstomodule(Element myObject, Module mod, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("belongstomodule");
            this.myObject = myObject;
            this.mod = mod;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                myObject?.NameKey?.ToString() ?? "null",
                mod?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

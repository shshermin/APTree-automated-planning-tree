using System;
using ModelLoader.ParameterTypes;

namespace ModelLoader.PredicateTypes
{
    public class CassetteAtStack : Predicate
    {
        public Cassette mod { get; set; }
        public Stackposition sp { get; set; }

        public CassetteAtStack(Cassette mod, Stackposition sp, bool isNegated) : base(isNegated)
        {
            PredicateType = new FastName("cassetteAtStack");
            this.mod = mod;
            this.sp = sp;
            this.PredicateName = GetUniqueKey();
        }

        public override List<string> GetParameterValues()
        {
            return new List<string>
            {
                mod?.NameKey?.ToString() ?? "null",
                sp?.NameKey?.ToString() ?? "null"
            };
        }
    }
}

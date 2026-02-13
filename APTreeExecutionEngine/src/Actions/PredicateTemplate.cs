using System;
using System.Collections.Generic;

public class ActionPredicateTemplate
{
    public string PredicateName { get; set; }
    public List<PredicateParameterMapping> ParameterMappings { get; set; }

    public ActionPredicateTemplate(string predicateName, List<PredicateParameterMapping> parameterMappings)
    {
        PredicateName = predicateName;
        ParameterMappings = parameterMappings ?? new List<PredicateParameterMapping>();
    }

    public ActionPredicateTemplate(string predicateName) : this(predicateName, new List<PredicateParameterMapping>())
    {
    }
}

public class PredicateParameterMapping
{
    public string ActionParameterName { get; set; }  // Parameter name from action (e.g., "obj")
    public string PredicateParameterName { get; set; }  // Parameter name in predicate (e.g., "object")
    public string ParameterType { get; set; }  // Type of the parameter (e.g., "Element", "Agent")

    public PredicateParameterMapping(string actionParameterName, string predicateParameterName, string parameterType)
    {
        ActionParameterName = actionParameterName;
        PredicateParameterName = predicateParameterName;
        ParameterType = parameterType;
    }
}

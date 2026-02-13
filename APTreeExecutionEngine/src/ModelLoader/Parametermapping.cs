public class ParameterMapping
{
    public string ParameterName { get; set; }
    public string ParameterValue { get; set; }

    public ParameterMapping(string parameterName, string parameterValue)
    {
        ParameterName = parameterName;
        ParameterValue = parameterValue;
    }
}
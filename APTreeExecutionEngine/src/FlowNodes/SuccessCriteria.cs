public enum SuccessCriteria
{
    ALL,            // All children must succeed
    ANY,            // At least one child must succeed
    COUNT,          // X number of children must succeed
    PERCENTAGE,     // X% of children must succeed
    SIGNAL
}
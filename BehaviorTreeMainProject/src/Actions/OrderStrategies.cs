public interface IOrderStrategy
{
    bool CanExecute(BTNodeResult currentState, BTNodeResult nextState, float elapsedTime, float delay);
}

public class TotalOrder : IOrderStrategy
{
    public bool CanExecute(BTNodeResult currentState, BTNodeResult nextState, float elapsedTime, float delay)
    {
         // Next action can only start when current action is completely finished
        return currentState == BTNodeResult.Success || 
               currentState == BTNodeResult.Failure;
    }
}

public class StrictParalellOrder : IOrderStrategy
{
    public bool CanExecute(BTNodeResult currentState, BTNodeResult nextState, float elapsedTime, float delay)
    {
         // Next action can start as soon as current action has started
        return currentState == BTNodeResult.InProgress;
    }
}

public class ParallelOrder : IOrderStrategy
{
    public bool CanExecute(BTNodeResult currentState, BTNodeResult nextState, float elapsedTime, float delay)
    {
        // Next action can start after current action has been running for 'delay' time
        return currentState == BTNodeResult.InProgress && 
               elapsedTime >= delay;
    }
}
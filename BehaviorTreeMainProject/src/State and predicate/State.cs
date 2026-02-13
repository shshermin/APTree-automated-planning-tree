using System.Collections.Generic;
using System.Linq;

public class State
{
    public  FastName StateName;
    public readonly StateType StateType;
    private Dictionary<FastName, Predicate> predicates;
    public IEnumerable<Predicate> TruePredicates => GetTruePredicates();

    public State(StateType stateType, FastName stateName)
    {
        StateType = stateType;
        StateName = stateName;
        predicates = new Dictionary<FastName, Predicate>();
    }

    // Add a predicate to the state
    public void AddPredicate(FastName objectKey, Predicate predicate)
    {
        if (!predicates.ContainsKey(objectKey))
        {
            predicates[objectKey] = predicate;
        }
        else
        {
            throw new InvalidOperationException($"Predicate for object {objectKey} already exists");
        }
    }

    // Remove a predicate from the state
    public void RemovePredicate(FastName objectKey, Predicate predicate)
    {
        if (predicates.ContainsKey(objectKey))
        {
            predicates.Remove(objectKey);
        }
    }

    // Check if a predicate exists for an object
    public bool HasPredicate(FastName objectKey, Predicate predicate)
    {
        return predicates.ContainsKey(objectKey) && 
               predicates[objectKey] == predicate;
    }

    // Get all predicates for an object
    public IEnumerable<Predicate> GetAllPredicates()
    {
        return predicates.Values;
    }

    // Create a deep copy of the current state
    public State Clone()
    {
        var newState = new State(StateType, StateName);
        foreach (var kvp in predicates)
        {
            newState.AddPredicate(kvp.Key, kvp.Value.Clone());
        }
        return newState;
    }

    // Clear all predicates
    public void Clear()
    {
        predicates.Clear();
    }

    // Get all object keys in the state
    public IEnumerable<FastName> GetAllObjects()
    {
        return predicates.Keys;
    }

    // Debug method to print current state
    public void PrintState()
    {
        Console.WriteLine("Current State:");
        foreach (var kvp in predicates)
        {
            Console.WriteLine($"Object {kvp.Key}:");
            Console.WriteLine($"  {kvp.Value}");
        }
        }

    // Get all non-negated (positive) predicates
public IEnumerable<Predicate> GetTruePredicates()
{
    return predicates.Values
        .Where(p => !p.not);
}

    }



using Neo4j.Driver;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// // This class manages connections and operations with Neo4j graph database
// It's used to create a graph representation of predicates and their relationships
/// </summary>
public class EnvironmentGraph : IDisposable
{
    private readonly IDriver _driver;
    private bool _disposed = false;
/// <summary>
/// Constructor for Neo4jService
/// </summary>
/// <param name="uri"> The URI of the Neo4j database   </param>
/// <param name="user"> The username for the Neo4j database </param>
/// <param name="password"> The password for the Neo4j database </param>
    public EnvironmentGraph(string uri, string user, string password)
    {

        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
    }
    /// <summary>
    /// Adds a predicate to the Neo4j graph database
    /// </summary>
    /// <param name="predicate">The predicate to add to the graph</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task SetPredicateOnGraph(Predicate predicate)
    {
        using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            var parameters = predicate.GetAllProperties();
            
            // Only keep real predicate parameters that map to entities in the graph
            var paramList = parameters
                .Where(p => p.Key != "PredicateName" && p.Key != "PredicateType" && p.Key != "isNegated")
                .Where(p => p.Value is Entity)
                .ToList();

            string query;
            var queryParams = new Dictionary<string, object?>();

            if (paramList.Count == 1)
            {
                var value = paramList[0].Value as Entity;
                query = $@"
                    MERGE (p0:{paramList[0].Value.GetType().Name} {{name: $firstParamName}})
                    SET p0:{predicate.GetPredicateType()}
                    RETURN p0";

                // Safely resolve first parameter name (fallback to entity.ToString() if NameKey is null)
                var firstName =
                    value?.NameKey?.ToString()
                    ?? paramList[0].Value?.ToString()
                    ?? string.Empty;
                queryParams.Add("firstParamName", firstName);
            }
            else if (paramList.Count == 2)
            {
                var value1 = paramList[0].Value as Entity;
                var value2 = paramList[1].Value as Entity;
                query = $@"
                    MERGE (p0:{paramList[0].Value.GetType().Name} {{name: $firstParamName}})
                    MERGE (p1:{paramList[1].Value.GetType().Name} {{name: $secondParamName}})
                    MERGE (p0)-[r:{predicate.GetPredicateType()}]->(p1)
                    RETURN p0, p1";

                // Safely resolve parameter names (fallbacks avoid null reference exceptions)
                var firstParamName =
                    value1?.NameKey?.ToString()
                    ?? paramList[0].Value?.ToString()
                    ?? string.Empty;

                var secondParamName =
                    value2?.NameKey?.ToString()
                    ?? paramList[1].Value?.ToString()
                    ?? string.Empty;

                queryParams.Add("firstParamName", firstParamName);
                queryParams.Add("secondParamName", secondParamName);
            }
            else
            {
                throw new ArgumentException($"Unsupported number of parameters: {paramList.Count}");
            }

            await tx.RunAsync(query, queryParams);
        });
    }

   

    public async Task<bool> TestConnection()
    {
        try
        {
            // Try to open a session and run a simple query
            using var session = _driver.AsyncSession();
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync("RETURN 1 as n");
                var record = await cursor.SingleAsync();
                return record["n"].As<int>();
            });
            
            return result == 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Neo4j connection test failed: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _driver?.Dispose();
            }
            _disposed = true;
        }
    }
} 
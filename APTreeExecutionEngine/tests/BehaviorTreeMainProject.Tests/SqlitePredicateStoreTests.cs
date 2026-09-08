namespace BehaviorTreeMainProject.Tests;

public class SqlitePredicateStoreTests : PredicateStoreContractTests
{
    protected override IPredicateStore CreateStore() => new SqlitePredicateStore(":memory:");
}

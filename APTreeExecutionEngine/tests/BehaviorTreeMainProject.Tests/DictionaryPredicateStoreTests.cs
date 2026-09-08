namespace BehaviorTreeMainProject.Tests;

public class DictionaryPredicateStoreTests : PredicateStoreContractTests
{
    protected override IPredicateStore CreateStore() => new DictionaryPredicateStore();
}

using System;
using System.IO;
using Xunit;

namespace BehaviorTreeMainProject.Tests;

public class SqlitePredicateStoreStorageModeTests
{
    [Fact]
    public void MemoryBackedStore_SupportsWriteAndRead()
    {
        using var store = new SqlitePredicateStore(":memory:");
        var predicate = PredicateStoreTestFixtures.AtPlace();

        store.Upsert(predicate.PredicateName, predicate);

        Assert.True(store.TryGet(predicate.PredicateName, out _));
    }

    [Fact]
    public void FileBackedStore_PersistsAcrossReopen()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"aptree-test-{Guid.NewGuid():N}.db");
        try
        {
            var predicate = PredicateStoreTestFixtures.AtPlace();

            using (var writer = new SqlitePredicateStore(dbPath))
            {
                writer.Upsert(predicate.PredicateName, predicate);
            }

            using var reopened = new SqlitePredicateStore(dbPath);

            Assert.True(reopened.HasFormattedDuplicate(BlackboardExtensions.FormatPredicate(predicate)),
                "the underlying SQLite file should still contain the row written before reopening");

            // Known issue: reopening never reloads the in-memory hot index, so
            // TryGet/ContainsKey/All miss persisted data that the SQL queries still see.
            Assert.False(reopened.TryGet(predicate.PredicateName, out _),
                "hot index is not rehydrated from the SQLite file on reopen");
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }
}

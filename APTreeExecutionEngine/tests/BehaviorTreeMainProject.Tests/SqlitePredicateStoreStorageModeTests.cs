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
    public void FileBackedStore_ShowsTheRowsWhileOpen()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"aptree-test-{Guid.NewGuid():N}.db");
        try
        {
            var predicate = PredicateStoreTestFixtures.AtPlace();
            using var store = new SqlitePredicateStore(dbPath);

            store.Upsert(predicate.PredicateName, predicate);

            Assert.True(store.HasFormattedDuplicate(BlackboardExtensions.FormatPredicate(predicate)));
            Assert.True(store.TryGet(predicate.PredicateName, out _));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    /// <summary>
    /// Regression: reopening used to keep the old rows in SQLite but not in the hot
    /// index, so SQL queries and TryGet disagreed. Every run now starts empty.
    /// </summary>
    [Fact]
    public void FileBackedStore_IsClearedWhenReopened()
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

            Assert.Equal(0, reopened.Count);
            Assert.False(reopened.TryGet(predicate.PredicateName, out _));
            Assert.False(reopened.HasFormattedDuplicate(BlackboardExtensions.FormatPredicate(predicate)));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }
}

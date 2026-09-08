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

            // A fresh SqlitePredicateStore only exposes the SQLite-backed pattern
            // queries (HasSimilar / HasFormattedDuplicate), not the hot-index -
            // InitSchema() populates the table but the new instance's in-memory
            // hot index starts empty, so TryGet/ContainsKey on it should be
            // false while the underlying file-backed data is still there.
            using var reopened = new SqlitePredicateStore(dbPath);

            Assert.True(reopened.HasFormattedDuplicate(BlackboardExtensions.FormatPredicate(predicate)),
                "the underlying SQLite file should still contain the row written before reopening");

            // Documents a real gotcha: the constructor only calls InitSchema(),
            // it never hydrates _hot from the file. So the hot-index-backed
            // reads (TryGet/ContainsKey/All/AllTrue) do NOT see previously
            // persisted data after a reopen, even though the SQL-backed
            // queries (HasFormattedDuplicate/HasSimilar/CleanupAtAgent) do.
            Assert.False(reopened.TryGet(predicate.PredicateName, out _),
                "hot index is not rehydrated from the SQLite file on reopen - if this now passes, update this comment, it's no longer accurate");
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }
}

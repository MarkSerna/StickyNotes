using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StickyNotes.Data.Context;

namespace StickyNotes.Tests.TestHelpers;

public class SqliteTestContext : IDisposable
{
    public SqliteConnection Connection { get; }
    public NotesDbContext DbContext { get; }

    public SqliteTestContext()
    {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();

        var options = new DbContextOptionsBuilder<NotesDbContext>()
            .UseSqlite(Connection)
            .Options;

        DbContext = new NotesDbContext(options);
        DbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        DbContext.Dispose();
        Connection.Dispose();
        GC.SuppressFinalize(this);
    }
}

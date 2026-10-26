using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JobMatcher.API.Data;

public static class DatabaseMigrator
{
    /// <summary>Brings the database schema up to date, keeping the data.</summary>
    public static async Task MigrateWithBaselineAsync(this AppDbContext db, ILogger logger, CancellationToken ct = default)
    {
        await BaselineLegacyDatabaseAsync(db, logger, ct);
        await db.Database.MigrateAsync(ct);
    }

    /// <summary>
    /// Databases created by EnsureCreated(), before migrations were introduced, already have the tables
    /// but no migrations history, so Migrate() would try to create the tables again and fail.
    /// Such a database is marked as having the first migration applied; it describes exactly that schema.
    /// </summary>
    private static async Task BaselineLegacyDatabaseAsync(AppDbContext db, ILogger logger, CancellationToken ct)
    {
        if (!await TableExistsAsync(db, "Users", ct) || await TableExistsAsync(db, "__EFMigrationsHistory", ct))
            return;

        var initialMigration = db.Database.GetMigrations().First();
        logger.LogWarning("Database was created without migrations, marking {Migration} as applied", initialMigration);

        var history = db.GetService<IHistoryRepository>();
        var sql = history.GetCreateScript() + history.GetInsertScript(new HistoryRow(initialMigration, ProductInfo.GetVersion()));
        await db.Database.ExecuteSqlRawAsync(sql, ct);
    }

    private static Task<bool> TableExistsAsync(AppDbContext db, string table, CancellationToken ct) =>
        db.Database
            .SqlQuery<bool>($"""
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.tables
                    WHERE table_schema = current_schema() AND table_name = {table}
                ) AS "Value"
                """)
            .SingleAsync(ct);
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StickyNotes.Data.Migrations;

/// <inheritdoc />
[Migration("20260906_InitialCreate")]
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Notes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Content = table.Column<string>(type: "TEXT", nullable: false),
                Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                PositionX = table.Column<double>(type: "REAL", nullable: false),
                PositionY = table.Column<double>(type: "REAL", nullable: false),
                Width = table.Column<double>(type: "REAL", nullable: false, defaultValue: 300.0),
                Height = table.Column<double>(type: "REAL", nullable: false, defaultValue: 260.0),
                Monitor = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                IsAlwaysOnTop = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                DeviceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                SyncStatus = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notes", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Notes_DeletedAt",
            table: "Notes",
            column: "DeletedAt");

        migrationBuilder.CreateIndex(
            name: "IX_Notes_SyncStatus",
            table: "Notes",
            column: "SyncStatus");

        migrationBuilder.CreateIndex(
            name: "IX_Notes_UpdatedAt",
            table: "Notes",
            column: "UpdatedAt");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Notes");
    }
}
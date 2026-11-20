using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fleet.Dal.Migrations;

/// <inheritdoc />
public partial class AddProcessedEvents : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProcessedEvents",
            columns: table => new
            {
                EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProcessedEvents", x => x.EventId);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ProcessedEvents");
    }
}


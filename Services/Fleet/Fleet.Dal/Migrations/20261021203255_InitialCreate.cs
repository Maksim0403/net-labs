using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Fleet.Dal.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryZones",
                columns: table => new
                {
                    ZoneId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryZones", x => x.ZoneId);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.WarehouseId);
                });

            migrationBuilder.CreateTable(
                name: "Couriers",
                columns: table => new
                {
                    CourierId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Couriers", x => x.CourierId);
                    table.ForeignKey(
                        name: "FK_Couriers_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    VehicleId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlateNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VehicleType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Van"),
                    CapacityKg = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.VehicleId);
                    table.CheckConstraint("CK_Vehicles_Capacity", "[CapacityKg] > 0");
                    table.ForeignKey(
                        name: "FK_Vehicles_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "WarehouseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourierLicenses",
                columns: table => new
                {
                    CourierId = table.Column<long>(type: "bigint", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false, defaultValue: "B"),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierLicenses", x => x.CourierId);
                    table.ForeignKey(
                        name: "FK_CourierLicenses_Couriers_CourierId",
                        column: x => x.CourierId,
                        principalTable: "Couriers",
                        principalColumn: "CourierId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourierZones",
                columns: table => new
                {
                    CourierId = table.Column<long>(type: "bigint", nullable: false),
                    ZoneId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierZones", x => new { x.CourierId, x.ZoneId });
                    table.ForeignKey(
                        name: "FK_CourierZones_Couriers_CourierId",
                        column: x => x.CourierId,
                        principalTable: "Couriers",
                        principalColumn: "CourierId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourierZones_DeliveryZones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "DeliveryZones",
                        principalColumn: "ZoneId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    RouteId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourierId = table.Column<long>(type: "bigint", nullable: false),
                    VehicleId = table.Column<long>(type: "bigint", nullable: false),
                    ZoneId = table.Column<long>(type: "bigint", nullable: false),
                    PlannedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Planned"),
                    PlannedDistanceKm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false, defaultValue: 0m),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.RouteId);
                    table.CheckConstraint("CK_Routes_Distance", "[PlannedDistanceKm] >= 0");
                    table.CheckConstraint("CK_Routes_Status", "[Status] IN ('Planned','InProgress','Completed','Cancelled')");
                    table.ForeignKey(
                        name: "FK_Routes_Couriers_CourierId",
                        column: x => x.CourierId,
                        principalTable: "Couriers",
                        principalColumn: "CourierId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Routes_DeliveryZones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "DeliveryZones",
                        principalColumn: "ZoneId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Routes_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "VehicleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "DeliveryZones",
                columns: new[] { "ZoneId", "CreatedAt", "CreatedBy", "IsDeleted", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", false, "Центр", null, null },
                    { 2L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", false, "Околиці", null, null },
                    { 3L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", false, "Міжміська", null, null }
                });

            migrationBuilder.InsertData(
                table: "Warehouses",
                columns: new[] { "WarehouseId", "Address", "City", "Code", "CreatedAt", "CreatedBy", "IsDeleted", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1L, "вул. Руська, 100", "Чернівці", "CV-01", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", false, null, null },
                    { 2L, "пр. Перемоги, 50", "Київ", "KY-01", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "seed", false, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourierLicenses_Number",
                table: "CourierLicenses",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Couriers_Phone",
                table: "Couriers",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Couriers_WarehouseId",
                table: "Couriers",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierZones_ZoneId",
                table: "CourierZones",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryZones_Name",
                table: "DeliveryZones",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Courier_Date",
                table: "Routes",
                columns: new[] { "CourierId", "PlannedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Status_Date",
                table: "Routes",
                columns: new[] { "Status", "PlannedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Routes_VehicleId",
                table: "Routes",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_ZoneId",
                table: "Routes",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_PlateNumber",
                table: "Vehicles",
                column: "PlateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_WarehouseId",
                table: "Vehicles",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_City",
                table: "Warehouses",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_Code",
                table: "Warehouses",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourierLicenses");

            migrationBuilder.DropTable(
                name: "CourierZones");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "Couriers");

            migrationBuilder.DropTable(
                name: "DeliveryZones");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "Warehouses");
        }
    }
}

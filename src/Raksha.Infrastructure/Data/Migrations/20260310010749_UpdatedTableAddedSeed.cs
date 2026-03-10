using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Raksha.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedTableAddedSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy", "Name", "NormalizedName", "Status" },
                values: new object[,]
                {
                    { new Guid("1fbd35a5-ec5a-48ce-9d93-47ca2494ff11"), null, new DateTime(2026, 3, 10, 1, 7, 48, 805, DateTimeKind.Utc).AddTicks(5738), "System", new DateTime(2026, 3, 10, 1, 7, 48, 805, DateTimeKind.Utc).AddTicks(5739), "System", "User", "USER", 1 },
                    { new Guid("95e139bb-6751-4d4b-b14f-12e1597ef982"), null, new DateTime(2026, 3, 10, 1, 7, 48, 805, DateTimeKind.Utc).AddTicks(5726), "System", new DateTime(2026, 3, 10, 1, 7, 48, 805, DateTimeKind.Utc).AddTicks(5730), "System", "Admin", "ADMIN", 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("1fbd35a5-ec5a-48ce-9d93-47ca2494ff11"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("95e139bb-6751-4d4b-b14f-12e1597ef982"));
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmerSample.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FarmPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "farm_photos",
                schema: "supply",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalBlobName = table.Column<string>(type: "text", nullable: false),
                    RenditionBlobName = table.Column<string>(type: "text", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    Caption = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ShowsPeople = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UploadedBy = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedBy = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_farm_photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_farm_photos_farms_FarmId",
                        column: x => x.FarmId,
                        principalSchema: "supply",
                        principalTable: "farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_farm_photos_FarmId",
                schema: "supply",
                table: "farm_photos",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_farm_photos_PublicId",
                schema: "supply",
                table: "farm_photos",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "farm_photos",
                schema: "supply");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmerSample.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LotPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lot_photos",
                schema: "supply",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalBlobName = table.Column<string>(type: "text", nullable: false),
                    RenditionBlobName = table.Column<string>(type: "text", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    Caption = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UploadedBy = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedBy = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lot_photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lot_photos_lots_LotId",
                        column: x => x.LotId,
                        principalSchema: "supply",
                        principalTable: "lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lot_photos_LotId",
                schema: "supply",
                table: "lot_photos",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_lot_photos_PublicId",
                schema: "supply",
                table: "lot_photos",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lot_photos",
                schema: "supply");
        }
    }
}

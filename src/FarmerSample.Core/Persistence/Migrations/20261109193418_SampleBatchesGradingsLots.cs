using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmerSample.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SampleBatchesGradingsLots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LotCode",
                schema: "catalogue",
                table: "sample_requests",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "lots",
                schema: "supply",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicCode = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Product = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Grade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ClosedReason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PriceMinPerKgMinor = table.Column<long>(type: "bigint", nullable: false),
                    PriceMaxPerKgMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Incoterm = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sample_batches",
                schema: "supply",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Product = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    WeightGrams = table.Column<int>(type: "integer", nullable: false),
                    CollectedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CollectedBy = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sample_batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sample_batches_farms_FarmId",
                        column: x => x.FarmId,
                        principalSchema: "supply",
                        principalTable: "farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lot_samples",
                schema: "supply",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ShareOverrideReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lot_samples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lot_samples_lots_LotId",
                        column: x => x.LotId,
                        principalSchema: "supply",
                        principalTable: "lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lot_contributions",
                schema: "supply",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SampleBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    VolumeKg = table.Column<int>(type: "integer", nullable: false),
                    CompositeSharePercent = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lot_contributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lot_contributions_lots_LotId",
                        column: x => x.LotId,
                        principalSchema: "supply",
                        principalTable: "lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lot_contributions_sample_batches_SampleBatchId",
                        column: x => x.SampleBatchId,
                        principalSchema: "supply",
                        principalTable: "sample_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gradings",
                schema: "supply",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SampleBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    LotSampleId = table.Column<Guid>(type: "uuid", nullable: true),
                    MoisturePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    DensityGramsPerLitre = table.Column<int>(type: "integer", nullable: true),
                    Cleanliness = table.Column<string>(type: "text", nullable: true),
                    Grade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Operator = table.Column<string>(type: "text", nullable: false),
                    GradedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrectionReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gradings", x => x.Id);
                    table.CheckConstraint("ck_grading_one_owner", "(\"SampleBatchId\" IS NULL) <> (\"LotSampleId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_gradings_lot_samples_LotSampleId",
                        column: x => x.LotSampleId,
                        principalSchema: "supply",
                        principalTable: "lot_samples",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_gradings_sample_batches_SampleBatchId",
                        column: x => x.SampleBatchId,
                        principalSchema: "supply",
                        principalTable: "sample_batches",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_gradings_LotSampleId",
                schema: "supply",
                table: "gradings",
                column: "LotSampleId");

            migrationBuilder.CreateIndex(
                name: "IX_gradings_SampleBatchId",
                schema: "supply",
                table: "gradings",
                column: "SampleBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_gradings_SupersedesId",
                schema: "supply",
                table: "gradings",
                column: "SupersedesId",
                unique: true,
                filter: "\"SupersedesId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_lot_contributions_LotId",
                schema: "supply",
                table: "lot_contributions",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_lot_contributions_SampleBatchId",
                schema: "supply",
                table: "lot_contributions",
                column: "SampleBatchId",
                unique: true,
                filter: "\"Active\"");

            migrationBuilder.CreateIndex(
                name: "IX_lot_samples_LotId",
                schema: "supply",
                table: "lot_samples",
                column: "LotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lots_PublicCode",
                schema: "supply",
                table: "lots",
                column: "PublicCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sample_batches_Code",
                schema: "supply",
                table: "sample_batches",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sample_batches_FarmId",
                schema: "supply",
                table: "sample_batches",
                column: "FarmId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gradings",
                schema: "supply");

            migrationBuilder.DropTable(
                name: "lot_contributions",
                schema: "supply");

            migrationBuilder.DropTable(
                name: "lot_samples",
                schema: "supply");

            migrationBuilder.DropTable(
                name: "sample_batches",
                schema: "supply");

            migrationBuilder.DropTable(
                name: "lots",
                schema: "supply");

            migrationBuilder.DropColumn(
                name: "LotCode",
                schema: "catalogue",
                table: "sample_requests");
        }
    }
}

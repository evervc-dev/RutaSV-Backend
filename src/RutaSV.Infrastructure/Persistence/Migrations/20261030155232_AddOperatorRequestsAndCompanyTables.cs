using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RutaSV.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatorRequestsAndCompanyTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "company",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    operator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trade_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    contact_email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    whatsapp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    municipality_id = table.Column<int>(type: "integer", nullable: true),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    status = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, defaultValue: "PENDIENTE"),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company", x => x.id);
                    table.ForeignKey(
                        name: "FK_company_app_user_approved_by",
                        column: x => x.approved_by,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_company_app_user_operator_id",
                        column: x => x.operator_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operator_request",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "PENDIENTE"),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operator_request", x => x.id);
                    table.ForeignKey(
                        name: "FK_operator_request_app_user_reviewed_by",
                        column: x => x.reviewed_by,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_operator_request_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_operator_request_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "operator_request_document",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    object_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operator_request_document", x => x.id);
                    table.ForeignKey(
                        name: "FK_operator_request_document_operator_request_request_id",
                        column: x => x.request_id,
                        principalTable: "operator_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_company_approved_by",
                table: "company",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_company_operator_id",
                table: "company",
                column: "operator_id");

            migrationBuilder.CreateIndex(
                name: "IX_operator_request_company_id",
                table: "operator_request",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_operator_request_reviewed_by",
                table: "operator_request",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "ix_operator_request_status_created_at",
                table: "operator_request",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_operator_request_user_id",
                table: "operator_request",
                column: "user_id",
                unique: true,
                filter: "status = 'PENDIENTE'");

            migrationBuilder.CreateIndex(
                name: "ix_operator_request_document_request_id",
                table: "operator_request_document",
                column: "request_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operator_request_document");

            migrationBuilder.DropTable(
                name: "operator_request");

            migrationBuilder.DropTable(
                name: "company");
        }
    }
}

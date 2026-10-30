using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RutaSV.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailNotificationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_notification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    recipient = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "PENDIENTE"),
                    attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    sent_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_notification", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_notification_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_email_notification_status_created_at",
                table: "email_notification",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_email_notification_user_id",
                table: "email_notification",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_notification");
        }
    }
}

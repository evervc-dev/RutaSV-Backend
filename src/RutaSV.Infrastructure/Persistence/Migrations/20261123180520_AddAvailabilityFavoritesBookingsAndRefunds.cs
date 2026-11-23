using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RutaSV.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailabilityFavoritesBookingsAndRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "email_notification",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.CreateTable(
                name: "experience_schedule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<short>(type: "smallint", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    default_capacity = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_schedule", x => x.id);
                    table.CheckConstraint("ck_experience_schedule_capacity", "default_capacity > 0");
                    table.CheckConstraint("ck_experience_schedule_dow", "day_of_week >= 0 AND day_of_week <= 6");
                    table.ForeignKey(
                        name: "FK_experience_schedule_experience_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experience",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "favorite",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_favorite", x => new { x.user_id, x.experience_id });
                    table.ForeignKey(
                        name: "FK_favorite_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_favorite_experience_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experience",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "availability",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    total_capacity = table.Column<int>(type: "integer", nullable: false),
                    reserved_capacity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    special_price = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    status = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, defaultValue: "ABIERTA"),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability", x => x.id);
                    table.CheckConstraint("ck_availability_reserved_capacity", "reserved_capacity >= 0 AND reserved_capacity <= total_capacity");
                    table.CheckConstraint("ck_availability_special_price", "special_price IS NULL OR special_price >= 0");
                    table.CheckConstraint("ck_availability_total_capacity", "total_capacity > 0");
                    table.ForeignKey(
                        name: "FK_availability_experience_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experience",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_availability_experience_schedule_schedule_id",
                        column: x => x.schedule_id,
                        principalTable: "experience_schedule",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "reservation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reservation_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    availability_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visitor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, defaultValue: "WEB"),
                    people_count = table.Column<short>(type: "smallint", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    currency = table.Column<string>(type: "character(3)", nullable: false, defaultValue: "USD"),
                    status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false, defaultValue: "PENDIENTE"),
                    payment_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "PAGADO_DEMO"),
                    payment_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    contact_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    contact_email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    special_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservation", x => x.id);
                    table.CheckConstraint("ck_reservation_people_count", "people_count > 0");
                    table.CheckConstraint("ck_reservation_total", "total >= 0");
                    table.CheckConstraint("ck_reservation_unit_price", "unit_price >= 0");
                    table.ForeignKey(
                        name: "FK_reservation_app_user_cancelled_by",
                        column: x => x.cancelled_by,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_reservation_app_user_created_by",
                        column: x => x.created_by,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reservation_app_user_visitor_id",
                        column: x => x.visitor_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_reservation_availability_availability_id",
                        column: x => x.availability_id,
                        principalTable: "availability",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reservation_experience_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experience",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refund",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    refund_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    percentage = table.Column<short>(type: "smallint", nullable: false),
                    reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, defaultValue: "EN_PROCESO"),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    processed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    processed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refund", x => x.id);
                    table.CheckConstraint("ck_refund_amount", "amount >= 0");
                    table.CheckConstraint("ck_refund_percentage", "percentage >= 0 AND percentage <= 100");
                    table.ForeignKey(
                        name: "FK_refund_app_user_processed_by",
                        column: x => x.processed_by,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_refund_reservation_reservation_id",
                        column: x => x.reservation_id,
                        principalTable: "reservation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reservation_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    new_status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservation_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_reservation_history_app_user_changed_by",
                        column: x => x.changed_by,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_reservation_history_reservation_reservation_id",
                        column: x => x.reservation_id,
                        principalTable: "reservation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reservation_incident",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cause = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PENDIENTE"),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    original_availability_id = table.Column<Guid>(type: "uuid", nullable: false),
                    new_availability_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decision_deadline = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    auto_resolved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservation_incident", x => x.id);
                    table.ForeignKey(
                        name: "FK_reservation_incident_availability_new_availability_id",
                        column: x => x.new_availability_id,
                        principalTable: "availability",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reservation_incident_availability_original_availability_id",
                        column: x => x.original_availability_id,
                        principalTable: "availability",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reservation_incident_reservation_reservation_id",
                        column: x => x.reservation_id,
                        principalTable: "reservation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_availability_experience_date_status",
                table: "availability",
                columns: new[] { "experience_id", "date", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_availability_schedule_id",
                table: "availability",
                column: "schedule_id");

            migrationBuilder.CreateIndex(
                name: "uq_availability_experience_date_start",
                table: "availability",
                columns: new[] { "experience_id", "date", "start_time" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_experience_schedule_experience_id",
                table: "experience_schedule",
                column: "experience_id");

            migrationBuilder.CreateIndex(
                name: "IX_favorite_experience_id",
                table: "favorite",
                column: "experience_id");

            migrationBuilder.CreateIndex(
                name: "IX_refund_processed_by",
                table: "refund",
                column: "processed_by");

            migrationBuilder.CreateIndex(
                name: "uq_refund_code",
                table: "refund",
                column: "refund_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_refund_reservation_id",
                table: "refund",
                column: "reservation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reservation_availability_id",
                table: "reservation",
                column: "availability_id");

            migrationBuilder.CreateIndex(
                name: "IX_reservation_cancelled_by",
                table: "reservation",
                column: "cancelled_by");

            migrationBuilder.CreateIndex(
                name: "IX_reservation_created_by",
                table: "reservation",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_reservation_experience_status",
                table: "reservation",
                columns: new[] { "experience_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_reservation_visitor_created_at",
                table: "reservation",
                columns: new[] { "visitor_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "uq_reservation_code",
                table: "reservation",
                column: "reservation_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reservation_history_changed_by",
                table: "reservation_history",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "IX_reservation_history_reservation_id",
                table: "reservation_history",
                column: "reservation_id");

            migrationBuilder.CreateIndex(
                name: "IX_reservation_incident_new_availability_id",
                table: "reservation_incident",
                column: "new_availability_id");

            migrationBuilder.CreateIndex(
                name: "IX_reservation_incident_original_availability_id",
                table: "reservation_incident",
                column: "original_availability_id");

            migrationBuilder.CreateIndex(
                name: "uq_reservation_incident_pending",
                table: "reservation_incident",
                column: "reservation_id",
                unique: true,
                filter: "status = 'PENDIENTE'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "favorite");

            migrationBuilder.DropTable(
                name: "refund");

            migrationBuilder.DropTable(
                name: "reservation_history");

            migrationBuilder.DropTable(
                name: "reservation_incident");

            migrationBuilder.DropTable(
                name: "reservation");

            migrationBuilder.DropTable(
                name: "availability");

            migrationBuilder.DropTable(
                name: "experience_schedule");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "email_notification",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);
        }
    }
}

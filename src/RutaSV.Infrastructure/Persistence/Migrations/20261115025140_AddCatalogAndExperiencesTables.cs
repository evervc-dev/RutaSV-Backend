using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RutaSV.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogAndExperiencesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "department",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_department", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "municipality",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    department_id = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_municipality", x => x.id);
                    table.ForeignKey(
                        name: "FK_municipality_department_department_id",
                        column: x => x.department_id,
                        principalTable: "department",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "experience",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    slug = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    short_description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    includes = table.Column<string>(type: "text", nullable: true),
                    excludes = table.Column<string>(type: "text", nullable: true),
                    what_to_bring = table.Column<string>(type: "text", nullable: true),
                    price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    currency = table.Column<string>(type: "character(3)", nullable: false, defaultValue: "USD"),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    difficulty = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    min_age = table.Column<short>(type: "smallint", nullable: true),
                    service_language = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    has_limited_capacity = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    min_people = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    max_people = table.Column<short>(type: "smallint", nullable: true),
                    municipality_id = table.Column<int>(type: "integer", nullable: false),
                    reference_address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    auto_confirmation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "BORRADOR"),
                    withdrawn_by = table.Column<Guid>(type: "uuid", nullable: true),
                    withdrawn_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    withdrawal_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    average_rating = table.Column<decimal>(type: "numeric(2,1)", nullable: false, defaultValue: 0m),
                    total_reviews = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    published_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience", x => x.id);
                    table.CheckConstraint("ck_experience_capacity", "(NOT has_limited_capacity) OR (max_people IS NOT NULL AND max_people >= min_people)");
                    table.CheckConstraint("ck_experience_duration_minutes", "duration_minutes IS NULL OR duration_minutes > 0");
                    table.CheckConstraint("ck_experience_latitude", "latitude >= -90 AND latitude <= 90");
                    table.CheckConstraint("ck_experience_longitude", "longitude >= -180 AND longitude <= 180");
                    table.CheckConstraint("ck_experience_min_people", "min_people > 0");
                    table.CheckConstraint("ck_experience_price", "price >= 0");
                    table.ForeignKey(
                        name: "FK_experience_app_user_withdrawn_by",
                        column: x => x.withdrawn_by,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_experience_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_experience_municipality_municipality_id",
                        column: x => x.municipality_id,
                        principalTable: "municipality",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "experience_category",
                columns: table => new
                {
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_category", x => new { x.experience_id, x.category_id });
                    table.ForeignKey(
                        name: "FK_experience_category_category_category_id",
                        column: x => x.category_id,
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_experience_category_experience_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experience",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_image",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    object_key = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    thumbnail_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    alt_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    is_cover = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    removed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    removed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    removal_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_image", x => x.id);
                    table.ForeignKey(
                        name: "FK_experience_image_app_user_removed_by",
                        column: x => x.removed_by,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_experience_image_experience_experience_id",
                        column: x => x.experience_id,
                        principalTable: "experience",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "category",
                columns: new[] { "id", "description", "icon", "is_active", "name", "slug", "sort_order" },
                values: new object[,]
                {
                    { 1, "Experiencias frente al mar y costas salvadoreñas", "beach", true, "Playa", "playa", (short)1 },
                    { 2, "Rutas de altura, volcanes y bosques nebulosos", "mountain", true, "Montaña", "montana", (short)2 },
                    { 3, "Clases y campamentos de surf en Surf City", "waves", true, "Surf", "surf", (short)3 },
                    { 4, "Senderismo guiado y ecoturismo", "hiking", true, "Caminata", "caminata", (short)4 },
                    { 5, "Gastronomía local, café y experiencias culinarias", "utensils", true, "Comida", "comida", (short)5 },
                    { 6, "Pueblos vivos, historia y artesanías", "landmark", true, "Cultura", "cultura", (short)6 },
                    { 7, "Canopy, rapel, kayak y deportes extremos", "compass", true, "Aventura", "aventura", (short)7 },
                    { 8, "Recorridos por lagos, esteros y golfos", "ship", true, "Paseo en lancha", "paseo-en-lancha", (short)8 },
                    { 9, "Sitios arqueológicos y museos nacionales", "building", true, "Museo", "museo", (short)9 },
                    { 10, "Parques nacionales y reservas naturales", "trees", true, "Parque", "parque", (short)10 }
                });

            migrationBuilder.InsertData(
                table: "department",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { (short)1, "Ahuachapán" },
                    { (short)2, "Santa Ana" },
                    { (short)3, "Sonsonate" },
                    { (short)4, "Chalatenango" },
                    { (short)5, "La Libertad" },
                    { (short)6, "San Salvador" },
                    { (short)7, "Cuscatlán" },
                    { (short)8, "La Paz" },
                    { (short)9, "Cabañas" },
                    { (short)10, "San Vicente" },
                    { (short)11, "Usulután" },
                    { (short)12, "San Miguel" },
                    { (short)13, "Morazán" },
                    { (short)14, "La Unión" }
                });

            migrationBuilder.InsertData(
                table: "municipality",
                columns: new[] { "id", "department_id", "name" },
                values: new object[,]
                {
                    { 1, (short)1, "Ahuachapán" },
                    { 2, (short)1, "Apaneca" },
                    { 3, (short)1, "Concepción de Ataco" },
                    { 4, (short)2, "Santa Ana" },
                    { 5, (short)2, "Chalchuapa" },
                    { 6, (short)2, "Metapán" },
                    { 7, (short)3, "Sonsonate" },
                    { 8, (short)3, "Juayúa" },
                    { 9, (short)3, "Nahuizalco" },
                    { 10, (short)3, "Acajutla" },
                    { 11, (short)4, "Chalatenango" },
                    { 12, (short)4, "La Palma" },
                    { 13, (short)4, "San Ignacio" },
                    { 14, (short)5, "Santa Tecla" },
                    { 15, (short)5, "La Libertad" },
                    { 16, (short)5, "Tamanique" },
                    { 17, (short)5, "Antiguo Cuscatlán" },
                    { 18, (short)6, "San Salvador" },
                    { 19, (short)6, "Panchimalco" },
                    { 20, (short)6, "Ilopango" },
                    { 21, (short)7, "Cojutepeque" },
                    { 22, (short)7, "Suchitoto" },
                    { 23, (short)8, "Zacatecoluca" },
                    { 24, (short)8, "San Luis Talpa" },
                    { 25, (short)9, "Sensuntepeque" },
                    { 26, (short)9, "Ilobasco" },
                    { 27, (short)10, "San Vicente" },
                    { 28, (short)11, "Usulután" },
                    { 29, (short)11, "Alegría" },
                    { 30, (short)11, "Jiquilisco" },
                    { 31, (short)12, "San Miguel" },
                    { 32, (short)13, "San Francisco Gotera" },
                    { 33, (short)13, "Perquín" },
                    { 34, (short)14, "La Unión" },
                    { 35, (short)14, "Conchagua" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_company_municipality_id",
                table: "company",
                column: "municipality_id");

            migrationBuilder.CreateIndex(
                name: "uq_category_name",
                table: "category",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_category_slug",
                table: "category",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_department_name",
                table: "department",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_experience_company_id",
                table: "experience",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_experience_municipality_id",
                table: "experience",
                column: "municipality_id");

            migrationBuilder.CreateIndex(
                name: "ix_experience_status_average_rating",
                table: "experience",
                columns: new[] { "status", "average_rating" });

            migrationBuilder.CreateIndex(
                name: "ix_experience_status_municipality_id",
                table: "experience",
                columns: new[] { "status", "municipality_id" });

            migrationBuilder.CreateIndex(
                name: "ix_experience_status_price",
                table: "experience",
                columns: new[] { "status", "price" });

            migrationBuilder.CreateIndex(
                name: "IX_experience_withdrawn_by",
                table: "experience",
                column: "withdrawn_by");

            migrationBuilder.CreateIndex(
                name: "uq_experience_slug",
                table: "experience",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_experience_category_category_id_experience_id",
                table: "experience_category",
                columns: new[] { "category_id", "experience_id" });

            migrationBuilder.CreateIndex(
                name: "ix_experience_image_experience_id",
                table: "experience_image",
                column: "experience_id");

            migrationBuilder.CreateIndex(
                name: "IX_experience_image_removed_by",
                table: "experience_image",
                column: "removed_by");

            migrationBuilder.CreateIndex(
                name: "uq_municipality_department_id_name",
                table: "municipality",
                columns: new[] { "department_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_company_municipality_municipality_id",
                table: "company",
                column: "municipality_id",
                principalTable: "municipality",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_company_municipality_municipality_id",
                table: "company");

            migrationBuilder.DropTable(
                name: "experience_category");

            migrationBuilder.DropTable(
                name: "experience_image");

            migrationBuilder.DropTable(
                name: "category");

            migrationBuilder.DropTable(
                name: "experience");

            migrationBuilder.DropTable(
                name: "municipality");

            migrationBuilder.DropTable(
                name: "department");

            migrationBuilder.DropIndex(
                name: "IX_company_municipality_id",
                table: "company");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HavaPaneli.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClimateNormals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "climate_normals",
                columns: table => new
                {
                    CityId = table.Column<int>(type: "integer", nullable: false),
                    DailyMeansC = table.Column<double[]>(type: "double precision[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_climate_normals", x => x.CityId);
                    table.ForeignKey(
                        name: "FK_climate_normals_cities_CityId",
                        column: x => x.CityId,
                        principalTable: "cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "climate_normals");
        }
    }
}

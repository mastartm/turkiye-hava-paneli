using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HavaPaneli.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentWeather : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "current_weather",
                columns: table => new
                {
                    CityId = table.Column<int>(type: "integer", nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TemperatureC = table.Column<double>(type: "double precision", nullable: true),
                    WeatherCode = table.Column<int>(type: "integer", nullable: true),
                    PrecipitationMm = table.Column<double>(type: "double precision", nullable: true),
                    FetchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_current_weather", x => x.CityId);
                    table.ForeignKey(
                        name: "FK_current_weather_cities_CityId",
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
                name: "current_weather");
        }
    }
}

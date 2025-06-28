using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Film_website.Migrations
{
    /// <inheritdoc />
    public partial class ReAddMovieTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubtitlePath",
                table: "Movies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubtitlePath",
                table: "Movies");
        }
    }
}

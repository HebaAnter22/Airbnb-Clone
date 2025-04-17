using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class moreInformationForHost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           
            migrationBuilder.AddColumn<string>(
                name: "DreamDestination",
                table: "hosts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FunFact",
                table: "hosts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LivesIn",
                table: "hosts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObsessedWith",
                table: "hosts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pets",
                table: "hosts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecialAbout",
                table: "hosts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(
                name: "DreamDestination",
                table: "hosts");

            migrationBuilder.DropColumn(
                name: "FunFact",
                table: "hosts");

            migrationBuilder.DropColumn(
                name: "LivesIn",
                table: "hosts");

            migrationBuilder.DropColumn(
                name: "ObsessedWith",
                table: "hosts");

            migrationBuilder.DropColumn(
                name: "Pets",
                table: "hosts");

            migrationBuilder.DropColumn(
                name: "SpecialAbout",
                table: "hosts");
        }
    }
}

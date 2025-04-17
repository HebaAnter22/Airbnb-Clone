using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class moreInformationToHostConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Pets",
                table: "hosts",
                newName: "pets");

            migrationBuilder.RenameColumn(
                name: "SpecialAbout",
                table: "hosts",
                newName: "special_about");

            migrationBuilder.RenameColumn(
                name: "ObsessedWith",
                table: "hosts",
                newName: "obsessed_with");

            migrationBuilder.RenameColumn(
                name: "LivesIn",
                table: "hosts",
                newName: "lives_in");

            migrationBuilder.RenameColumn(
                name: "FunFact",
                table: "hosts",
                newName: "fun_fact");

            migrationBuilder.RenameColumn(
                name: "DreamDestination",
                table: "hosts",
                newName: "dream_destination");

            migrationBuilder.AlterColumn<string>(
                name: "pets",
                table: "hosts",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "special_about",
                table: "hosts",
                type: "varchar(100)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "obsessed_with",
                table: "hosts",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "lives_in",
                table: "hosts",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "fun_fact",
                table: "hosts",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "dream_destination",
                table: "hosts",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "pets",
                table: "hosts",
                newName: "Pets");

            migrationBuilder.RenameColumn(
                name: "special_about",
                table: "hosts",
                newName: "SpecialAbout");

            migrationBuilder.RenameColumn(
                name: "obsessed_with",
                table: "hosts",
                newName: "ObsessedWith");

            migrationBuilder.RenameColumn(
                name: "lives_in",
                table: "hosts",
                newName: "LivesIn");

            migrationBuilder.RenameColumn(
                name: "fun_fact",
                table: "hosts",
                newName: "FunFact");

            migrationBuilder.RenameColumn(
                name: "dream_destination",
                table: "hosts",
                newName: "DreamDestination");

            migrationBuilder.AlterColumn<string>(
                name: "Pets",
                table: "hosts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SpecialAbout",
                table: "hosts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ObsessedWith",
                table: "hosts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LivesIn",
                table: "hosts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FunFact",
                table: "hosts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DreamDestination",
                table: "hosts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}

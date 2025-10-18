using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PassBuy.Migrations
{
    /// <inheritdoc />
    public partial class BuildAPIs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_email",
                table: "users");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.RenameColumn(
                name: "password",
                table: "Users",
                newName: "Password");

            migrationBuilder.RenameColumn(
                name: "lastName",
                table: "Users",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "firstName",
                table: "Users",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Users",
                newName: "Email");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Users",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "uuid_generate_v4()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "EducationProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EduCode = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PassBuyCardApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardType = table.Column<int>(type: "integer", nullable: false),
                    DateApplied = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassBuyCardApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PassBuyCardApplications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransportEmployers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportEmployers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EducationDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationId = table.Column<int>(type: "integer", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentNumber = table.Column<int>(type: "integer", nullable: false),
                    CourseCode = table.Column<int>(type: "integer", nullable: false),
                    CourseTitle = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducationDetails_EducationProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "EducationProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EducationDetails_PassBuyCardApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "PassBuyCardApplications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GovIdDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationId = table.Column<int>(type: "integer", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovIdDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GovIdDetails_PassBuyCardApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "PassBuyCardApplications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PassBuyCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardType = table.Column<int>(type: "integer", nullable: false),
                    DateApproved = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApplicationId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassBuyCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PassBuyCards_PassBuyCardApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "PassBuyCardApplications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PassBuyCards_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransportEmploymentDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationId = table.Column<int>(type: "integer", nullable: false),
                    TransportEmployerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportEmploymentDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransportEmploymentDetails_PassBuyCardApplications_Applicat~",
                        column: x => x.ApplicationId,
                        principalTable: "PassBuyCardApplications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransportEmploymentDetails_TransportEmployers_TransportEmpl~",
                        column: x => x.TransportEmployerId,
                        principalTable: "TransportEmployers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EducationDetails_ApplicationId",
                table: "EducationDetails",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EducationDetails_ProviderId",
                table: "EducationDetails",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_GovIdDetails_ApplicationId",
                table: "GovIdDetails",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PassBuyCardApplications_UserId",
                table: "PassBuyCardApplications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PassBuyCards_ApplicationId",
                table: "PassBuyCards",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PassBuyCards_UserId",
                table: "PassBuyCards",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportEmploymentDetails_ApplicationId",
                table: "TransportEmploymentDetails",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransportEmploymentDetails_TransportEmployerId",
                table: "TransportEmploymentDetails",
                column: "TransportEmployerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EducationDetails");

            migrationBuilder.DropTable(
                name: "GovIdDetails");

            migrationBuilder.DropTable(
                name: "PassBuyCards");

            migrationBuilder.DropTable(
                name: "TransportEmploymentDetails");

            migrationBuilder.DropTable(
                name: "EducationProviders");

            migrationBuilder.DropTable(
                name: "PassBuyCardApplications");

            migrationBuilder.DropTable(
                name: "TransportEmployers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.RenameColumn(
                name: "Password",
                table: "users",
                newName: "password");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "users",
                newName: "lastName");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "users",
                newName: "firstName");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "users",
                newName: "email");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValueSql: "uuid_generate_v4()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }
    }
}

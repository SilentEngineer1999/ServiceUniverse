using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PassBuy.Migrations
{
    /// <inheritdoc />
    public partial class UniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransportEmploymentDetails_TransportEmployers_TransportEmpl~",
                table: "TransportEmploymentDetails");

            migrationBuilder.DropIndex(
                name: "IX_TransportEmploymentDetails_TransportEmployerId",
                table: "TransportEmploymentDetails");

            migrationBuilder.DropIndex(
                name: "IX_EducationDetails_ProviderId",
                table: "EducationDetails");

            migrationBuilder.RenameColumn(
                name: "TransportEmployerId",
                table: "TransportEmploymentDetails",
                newName: "EmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportEmploymentDetails_EmployerId_EmployeeNumber",
                table: "TransportEmploymentDetails",
                columns: new[] { "EmployerId", "EmployeeNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransportEmployers_Name",
                table: "TransportEmployers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EducationProviders_EduCode",
                table: "EducationProviders",
                column: "EduCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EducationProviders_Name",
                table: "EducationProviders",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EducationDetails_ProviderId_StudentNumber",
                table: "EducationDetails",
                columns: new[] { "ProviderId", "StudentNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TransportEmploymentDetails_TransportEmployers_EmployerId",
                table: "TransportEmploymentDetails",
                column: "EmployerId",
                principalTable: "TransportEmployers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransportEmploymentDetails_TransportEmployers_EmployerId",
                table: "TransportEmploymentDetails");

            migrationBuilder.DropIndex(
                name: "IX_TransportEmploymentDetails_EmployerId_EmployeeNumber",
                table: "TransportEmploymentDetails");

            migrationBuilder.DropIndex(
                name: "IX_TransportEmployers_Name",
                table: "TransportEmployers");

            migrationBuilder.DropIndex(
                name: "IX_EducationProviders_EduCode",
                table: "EducationProviders");

            migrationBuilder.DropIndex(
                name: "IX_EducationProviders_Name",
                table: "EducationProviders");

            migrationBuilder.DropIndex(
                name: "IX_EducationDetails_ProviderId_StudentNumber",
                table: "EducationDetails");

            migrationBuilder.RenameColumn(
                name: "EmployerId",
                table: "TransportEmploymentDetails",
                newName: "TransportEmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportEmploymentDetails_TransportEmployerId",
                table: "TransportEmploymentDetails",
                column: "TransportEmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_EducationDetails_ProviderId",
                table: "EducationDetails",
                column: "ProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransportEmploymentDetails_TransportEmployers_TransportEmpl~",
                table: "TransportEmploymentDetails",
                column: "TransportEmployerId",
                principalTable: "TransportEmployers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherIdToStudentProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TeacherId",
                table: "student_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_student_profiles_teacher_id",
                table: "student_profiles",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "fk_student_profiles_teacher",
                table: "student_profiles",
                column: "TeacherId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_student_profiles_teacher",
                table: "student_profiles");

            migrationBuilder.DropIndex(
                name: "ix_student_profiles_teacher_id",
                table: "student_profiles");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "student_profiles");
        }
    }
}

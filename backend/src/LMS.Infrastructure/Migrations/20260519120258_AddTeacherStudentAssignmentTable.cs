using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherStudentAssignmentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "teacher_student_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teacher_student_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "fk_teacher_student_assignments_assigned_by",
                        column: x => x.AssignedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_teacher_student_assignments_student",
                        column: x => x.StudentProfileId,
                        principalTable: "student_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_teacher_student_assignments_teacher",
                        column: x => x.TeacherUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_teacher_student_assignments_AssignedByUserId",
                table: "teacher_student_assignments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "ix_teacher_student_assignments_student",
                table: "teacher_student_assignments",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "ix_teacher_student_assignments_teacher",
                table: "teacher_student_assignments",
                column: "TeacherUserId");

            migrationBuilder.CreateIndex(
                name: "ix_teacher_student_assignments_unique",
                table: "teacher_student_assignments",
                columns: new[] { "TeacherUserId", "StudentProfileId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "teacher_student_assignments");

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
    }
}

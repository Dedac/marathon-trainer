using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarathonTrainer.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    WeightLbs = table.Column<double>(type: "REAL", nullable: false),
                    HeightInches = table.Column<double>(type: "REAL", nullable: false),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CurrentWeeklyMileage = table.Column<double>(type: "REAL", nullable: false),
                    RunningExperience = table.Column<string>(type: "TEXT", nullable: false),
                    PreferredRunDaysPerWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    LongRunDay = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FitnessAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentLongestRunMiles = table.Column<double>(type: "REAL", nullable: false),
                    RecentRaceTimeTicks = table.Column<long>(type: "INTEGER", nullable: true),
                    RecentRaceDistanceMiles = table.Column<double>(type: "REAL", nullable: true),
                    RestingHeartRate = table.Column<int>(type: "INTEGER", nullable: true),
                    ComfortablePaceMinutesPerMile = table.Column<double>(type: "REAL", nullable: false),
                    CrossTrainingPreferences = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FitnessAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FitnessAssessments_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    HasKneeIssues = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasPlantarFasciitis = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasAsthma = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasHeartCondition = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecentInjuries = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DoctorClearance = table.Column<bool>(type: "INTEGER", nullable: false),
                    MedicationsAffectingHeartRate = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalInfos_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    RaceType = table.Column<string>(type: "TEXT", nullable: false),
                    RaceDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PlanStartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalWeeks = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingPlans_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingWeeks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrainingPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    WeekNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalMileage = table.Column<double>(type: "REAL", nullable: false),
                    IsStepBackWeek = table.Column<bool>(type: "INTEGER", nullable: false),
                    Phase = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingWeeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingWeeks_TrainingPlans_TrainingPlanId",
                        column: x => x.TrainingPlanId,
                        principalTable: "TrainingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrainingWeekId = table.Column<int>(type: "INTEGER", nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    RunType = table.Column<string>(type: "TEXT", nullable: false),
                    DistanceMiles = table.Column<double>(type: "REAL", nullable: false),
                    TargetPaceMinPerMile = table.Column<double>(type: "REAL", nullable: true),
                    TargetPaceMaxMinPerMile = table.Column<double>(type: "REAL", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    MedicalModifications = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingDays_TrainingWeeks_TrainingWeekId",
                        column: x => x.TrainingWeekId,
                        principalTable: "TrainingWeeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FitnessAssessments_UserProfileId",
                table: "FitnessAssessments",
                column: "UserProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalInfos_UserProfileId",
                table: "MedicalInfos",
                column: "UserProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDays_TrainingWeekId",
                table: "TrainingDays",
                column: "TrainingWeekId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPlans_UserProfileId",
                table: "TrainingPlans",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingWeeks_TrainingPlanId",
                table: "TrainingWeeks",
                column: "TrainingPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FitnessAssessments");

            migrationBuilder.DropTable(
                name: "MedicalInfos");

            migrationBuilder.DropTable(
                name: "TrainingDays");

            migrationBuilder.DropTable(
                name: "TrainingWeeks");

            migrationBuilder.DropTable(
                name: "TrainingPlans");

            migrationBuilder.DropTable(
                name: "UserProfiles");
        }
    }
}

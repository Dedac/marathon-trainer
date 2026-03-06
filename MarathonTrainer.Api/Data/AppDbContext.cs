using Microsoft.EntityFrameworkCore;
using MarathonTrainer.Api.Models;

namespace MarathonTrainer.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<MedicalInfo> MedicalInfos => Set<MedicalInfo>();
    public DbSet<FitnessAssessment> FitnessAssessments => Set<FitnessAssessment>();
    public DbSet<TrainingPlan> TrainingPlans => Set<TrainingPlan>();
    public DbSet<TrainingWeek> TrainingWeeks => Set<TrainingWeek>();
    public DbSet<TrainingDay> TrainingDays => Set<TrainingDay>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // UserProfile -> MedicalInfo (one-to-one, cascade delete)
        modelBuilder.Entity<UserProfile>()
            .HasOne(u => u.MedicalInfo)
            .WithOne(m => m.UserProfile)
            .HasForeignKey<MedicalInfo>(m => m.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // UserProfile -> FitnessAssessment (one-to-one, cascade delete)
        modelBuilder.Entity<UserProfile>()
            .HasOne(u => u.FitnessAssessment)
            .WithOne(f => f.UserProfile)
            .HasForeignKey<FitnessAssessment>(f => f.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // UserProfile -> TrainingPlans (one-to-many, cascade delete)
        modelBuilder.Entity<UserProfile>()
            .HasMany(u => u.TrainingPlans)
            .WithOne(t => t.UserProfile)
            .HasForeignKey(t => t.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // TrainingPlan -> TrainingWeeks (one-to-many, cascade delete)
        modelBuilder.Entity<TrainingPlan>()
            .HasMany(p => p.TrainingWeeks)
            .WithOne(w => w.TrainingPlan)
            .HasForeignKey(w => w.TrainingPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // TrainingWeek -> TrainingDays (one-to-many, cascade delete)
        modelBuilder.Entity<TrainingWeek>()
            .HasMany(w => w.TrainingDays)
            .WithOne(d => d.TrainingWeek)
            .HasForeignKey(d => d.TrainingWeekId)
            .OnDelete(DeleteBehavior.Cascade);

        // Store enums as strings
        modelBuilder.Entity<UserProfile>()
            .Property(u => u.RunningExperience)
            .HasConversion<string>();

        modelBuilder.Entity<TrainingPlan>()
            .Property(p => p.RaceType)
            .HasConversion<string>();

        modelBuilder.Entity<TrainingDay>()
            .Property(d => d.RunType)
            .HasConversion<string>();
    }
}

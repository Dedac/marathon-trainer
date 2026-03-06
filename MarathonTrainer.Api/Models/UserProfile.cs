using System.ComponentModel.DataAnnotations;

namespace MarathonTrainer.Api.Models;

public class UserProfile
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int Age { get; set; }

    [Required]
    public double WeightLbs { get; set; }

    [Required]
    public double HeightInches { get; set; }

    [Required]
    [MaxLength(20)]
    public string Gender { get; set; } = string.Empty;

    [Required]
    public double CurrentWeeklyMileage { get; set; }

    [Required]
    public RunningExperience RunningExperience { get; set; }

    [Required]
    [Range(3, 6)]
    public int PreferredRunDaysPerWeek { get; set; }

    [Required]
    public DayOfWeek LongRunDay { get; set; }

    // Navigation properties
    public MedicalInfo? MedicalInfo { get; set; }
    public FitnessAssessment? FitnessAssessment { get; set; }
    public List<TrainingPlan> TrainingPlans { get; set; } = [];
}

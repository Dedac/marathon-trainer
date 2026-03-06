using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MarathonTrainer.Api.Models;

public class TrainingWeek
{
    public int Id { get; set; }

    [Required]
    public int TrainingPlanId { get; set; }

    [Required]
    public int WeekNumber { get; set; }

    [Required]
    public double TotalMileage { get; set; }

    [Required]
    public bool IsStepBackWeek { get; set; }

    [Required]
    [MaxLength(20)]
    public string Phase { get; set; } = string.Empty;

    // Navigation properties
    [JsonIgnore]
    public TrainingPlan? TrainingPlan { get; set; }
    [JsonPropertyName("days")]
    public List<TrainingDay> TrainingDays { get; set; } = [];
}

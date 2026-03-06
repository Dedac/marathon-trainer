using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MarathonTrainer.Api.Models;

public class TrainingDay
{
    public int Id { get; set; }

    [Required]
    public int TrainingWeekId { get; set; }

    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public RunType RunType { get; set; }

    [Required]
    public double DistanceMiles { get; set; }

    public double? TargetPaceMinPerMile { get; set; }

    public double? TargetPaceMaxMinPerMile { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? MedicalModifications { get; set; }

    // Navigation property
    [JsonIgnore]
    public TrainingWeek? TrainingWeek { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MarathonTrainer.Api.Models;

public class TrainingPlan
{
    public int Id { get; set; }

    [Required]
    public int UserProfileId { get; set; }

    [Required]
    public RaceType RaceType { get; set; }

    [Required]
    public DateTime RaceDate { get; set; }

    [Required]
    public DateTime PlanStartDate { get; set; }

    [Required]
    public int TotalWeeks { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    [JsonIgnore]
    public UserProfile? UserProfile { get; set; }
    [JsonPropertyName("weeks")]
    public List<TrainingWeek> TrainingWeeks { get; set; } = [];
}

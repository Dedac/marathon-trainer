using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MarathonTrainer.Api.Models;

public class FitnessAssessment
{
    public int Id { get; set; }

    [Required]
    public int UserProfileId { get; set; }

    [Required]
    public double CurrentLongestRunMiles { get; set; }

    public long? RecentRaceTimeTicks { get; set; }

    public double? RecentRaceDistanceMiles { get; set; }

    public int? RestingHeartRate { get; set; }

    [Required]
    public double ComfortablePaceMinutesPerMile { get; set; }

    [MaxLength(500)]
    public string? CrossTrainingPreferences { get; set; }

    // Navigation property
    [JsonIgnore]
    public UserProfile? UserProfile { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MarathonTrainer.Api.Models;

public class MedicalInfo
{
    public int Id { get; set; }

    [Required]
    public int UserProfileId { get; set; }

    [Required]
    public bool HasKneeIssues { get; set; }

    [Required]
    public bool HasPlantarFasciitis { get; set; }

    [Required]
    public bool HasAsthma { get; set; }

    [Required]
    public bool HasHeartCondition { get; set; }

    [MaxLength(500)]
    public string? RecentInjuries { get; set; }

    [Required]
    public bool DoctorClearance { get; set; }

    [MaxLength(500)]
    public string? MedicationsAffectingHeartRate { get; set; }

    // Navigation property
    [JsonIgnore]
    public UserProfile? UserProfile { get; set; }
}

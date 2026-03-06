using System.ComponentModel.DataAnnotations;

namespace MarathonTrainer.Api.Models.DTOs;

public class GeneratePlanRequest
{
    [Required]
    public int UserProfileId { get; set; }

    [Required]
    public string RaceType { get; set; } = string.Empty;

    [Required]
    public DateTime RaceDate { get; set; }
}

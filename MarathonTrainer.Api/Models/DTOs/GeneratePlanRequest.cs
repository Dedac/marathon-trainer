using System.ComponentModel.DataAnnotations;

namespace MarathonTrainer.Api.Models.DTOs;

public class GeneratePlanRequest
{
    [Required]
    public int UserProfileId { get; set; }

    [Required]
    public RaceType RaceType { get; set; }

    [Required]
    public DateTime RaceDate { get; set; }
}

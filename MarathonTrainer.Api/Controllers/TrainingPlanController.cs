using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarathonTrainer.Api.Data;
using MarathonTrainer.Api.Models;
using MarathonTrainer.Api.Models.DTOs;
using MarathonTrainer.Api.Services;

namespace MarathonTrainer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrainingPlanController(
    AppDbContext db,
    ITrainingPlanGenerator planGenerator,
    IMedicalAdjustmentService medicalAdjustment) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<IActionResult> GeneratePlan(GeneratePlanRequest request)
    {
        if (!Enum.TryParse<RaceType>(request.RaceType, ignoreCase: true, out var raceType))
            return BadRequest($"Invalid RaceType '{request.RaceType}'. Valid values: {string.Join(", ", Enum.GetNames<RaceType>())}");

        var profile = await db.UserProfiles
            .Include(p => p.MedicalInfo)
            .Include(p => p.FitnessAssessment)
            .FirstOrDefaultAsync(p => p.Id == request.UserProfileId);

        if (profile is null)
            return NotFound($"UserProfile with ID {request.UserProfileId} not found.");

        var plan = planGenerator.GeneratePlan(profile, raceType, request.RaceDate);

        if (profile.MedicalInfo is not null)
            medicalAdjustment.AdjustPlan(plan, profile.MedicalInfo);

        plan.UserProfileId = profile.Id;
        db.TrainingPlans.Add(plan);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPlanById), new { id = plan.Id }, plan);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPlanById(int id)
    {
        var plan = await db.TrainingPlans
            .Include(p => p.TrainingWeeks)
                .ThenInclude(w => w.TrainingDays)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan is null)
            return NotFound();

        return Ok(plan);
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestPlan()
    {
        var plan = await db.TrainingPlans
            .Include(p => p.TrainingWeeks)
                .ThenInclude(w => w.TrainingDays)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (plan is null)
            return NotFound();

        return Ok(plan);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePlan(int id)
    {
        var plan = await db.TrainingPlans.FindAsync(id);

        if (plan is null)
            return NotFound();

        db.TrainingPlans.Remove(plan);
        await db.SaveChangesAsync();

        return NoContent();
    }
}

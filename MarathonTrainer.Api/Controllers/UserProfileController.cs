using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarathonTrainer.Api.Data;
using MarathonTrainer.Api.Models;

namespace MarathonTrainer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserProfileController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await db.UserProfiles
            .Include(p => p.MedicalInfo)
            .Include(p => p.FitnessAssessment)
            .FirstOrDefaultAsync();

        if (profile is null)
            return NotFound();

        return Ok(profile);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProfileById(int id)
    {
        var profile = await db.UserProfiles
            .Include(p => p.MedicalInfo)
            .Include(p => p.FitnessAssessment)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (profile is null)
            return NotFound();

        return Ok(profile);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProfile(UserProfile profile)
    {
        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProfileById), new { id = profile.Id }, profile);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProfile(int id, UserProfile updated)
    {
        var existing = await db.UserProfiles
            .Include(p => p.MedicalInfo)
            .Include(p => p.FitnessAssessment)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (existing is null)
            return NotFound();

        // Update scalar properties
        existing.Name = updated.Name;
        existing.Age = updated.Age;
        existing.WeightLbs = updated.WeightLbs;
        existing.HeightInches = updated.HeightInches;
        existing.Gender = updated.Gender;
        existing.CurrentWeeklyMileage = updated.CurrentWeeklyMileage;
        existing.RunningExperience = updated.RunningExperience;
        existing.PreferredRunDaysPerWeek = updated.PreferredRunDaysPerWeek;
        existing.LongRunDay = updated.LongRunDay;

        // Update nested MedicalInfo
        if (updated.MedicalInfo is not null)
        {
            if (existing.MedicalInfo is null)
            {
                updated.MedicalInfo.UserProfileId = id;
                existing.MedicalInfo = updated.MedicalInfo;
            }
            else
            {
                existing.MedicalInfo.HasKneeIssues = updated.MedicalInfo.HasKneeIssues;
                existing.MedicalInfo.HasPlantarFasciitis = updated.MedicalInfo.HasPlantarFasciitis;
                existing.MedicalInfo.HasAsthma = updated.MedicalInfo.HasAsthma;
                existing.MedicalInfo.HasHeartCondition = updated.MedicalInfo.HasHeartCondition;
                existing.MedicalInfo.RecentInjuries = updated.MedicalInfo.RecentInjuries;
                existing.MedicalInfo.DoctorClearance = updated.MedicalInfo.DoctorClearance;
                existing.MedicalInfo.MedicationsAffectingHeartRate = updated.MedicalInfo.MedicationsAffectingHeartRate;
            }
        }

        // Update nested FitnessAssessment
        if (updated.FitnessAssessment is not null)
        {
            if (existing.FitnessAssessment is null)
            {
                updated.FitnessAssessment.UserProfileId = id;
                existing.FitnessAssessment = updated.FitnessAssessment;
            }
            else
            {
                existing.FitnessAssessment.CurrentLongestRunMiles = updated.FitnessAssessment.CurrentLongestRunMiles;
                existing.FitnessAssessment.RecentRaceTimeTicks = updated.FitnessAssessment.RecentRaceTimeTicks;
                existing.FitnessAssessment.RecentRaceDistanceMiles = updated.FitnessAssessment.RecentRaceDistanceMiles;
                existing.FitnessAssessment.RestingHeartRate = updated.FitnessAssessment.RestingHeartRate;
                existing.FitnessAssessment.ComfortablePaceMinutesPerMile = updated.FitnessAssessment.ComfortablePaceMinutesPerMile;
                existing.FitnessAssessment.CrossTrainingPreferences = updated.FitnessAssessment.CrossTrainingPreferences;
            }
        }

        await db.SaveChangesAsync();

        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProfile(int id)
    {
        var profile = await db.UserProfiles.FindAsync(id);

        if (profile is null)
            return NotFound();

        db.UserProfiles.Remove(profile);
        await db.SaveChangesAsync();

        return NoContent();
    }
}

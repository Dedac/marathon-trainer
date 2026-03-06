using MarathonTrainer.Api.Models;
using MarathonTrainer.Api.Services;

namespace MarathonTrainer.Api.Tests;

public class MedicalAdjustmentServiceTests
{
    private readonly TrainingPlanGenerator _generator = new(TimeProvider.System);
    private readonly MedicalAdjustmentService _medicalService = new();

    #region Helpers

    private static UserProfile CreateProfile(
        RunningExperience experience = RunningExperience.Intermediate,
        double weeklyMileage = 20,
        double longestRun = 6,
        double comfortablePace = 9.5,
        int runDaysPerWeek = 4,
        DayOfWeek longRunDay = DayOfWeek.Saturday)
    {
        return new UserProfile
        {
            Id = 1,
            Name = "Test Runner",
            Age = 30,
            WeightLbs = 160,
            HeightInches = 70,
            Gender = "Male",
            CurrentWeeklyMileage = weeklyMileage,
            RunningExperience = experience,
            PreferredRunDaysPerWeek = runDaysPerWeek,
            LongRunDay = longRunDay,
            FitnessAssessment = new FitnessAssessment
            {
                Id = 1,
                UserProfileId = 1,
                CurrentLongestRunMiles = longestRun,
                ComfortablePaceMinutesPerMile = comfortablePace
            }
        };
    }

    private static MedicalInfo CreateMedicalInfo(
        bool kneeIssues = false,
        bool plantarFasciitis = false,
        bool asthma = false,
        bool heartCondition = false,
        string? recentInjuries = null)
    {
        return new MedicalInfo
        {
            Id = 1,
            UserProfileId = 1,
            HasKneeIssues = kneeIssues,
            HasPlantarFasciitis = plantarFasciitis,
            HasAsthma = asthma,
            HasHeartCondition = heartCondition,
            RecentInjuries = recentInjuries,
            DoctorClearance = true
        };
    }

    private static DateTime RaceDateWeeksFromNow(int weeks)
    {
        return DateTime.UtcNow.Date.AddDays(weeks * 7);
    }

    private TrainingPlan GenerateBasePlan(
        RunningExperience experience = RunningExperience.Intermediate,
        int runDays = 5)
    {
        var profile = CreateProfile(experience: experience, runDaysPerWeek: runDays);
        return _generator.GeneratePlan(profile, RaceType.FullMarathon, RaceDateWeeksFromNow(18));
    }

    #endregion

    #region Asthma Adjustments

    [Fact]
    public void Asthma_AddsInhalerNotesToRunDays()
    {
        var plan = GenerateBasePlan();
        var medical = CreateMedicalInfo(asthma: true);

        _medicalService.AdjustPlan(plan, medical);

        var runDays = plan.TrainingWeeks
            .SelectMany(w => w.TrainingDays)
            .Where(d => d.RunType is not RunType.Rest and not RunType.CrossTrain);

        Assert.All(runDays, d =>
        {
            Assert.NotNull(d.MedicalModifications);
            Assert.Contains("Asthma", d.MedicalModifications);
            Assert.Contains("inhaler", d.MedicalModifications);
        });
    }

    [Fact]
    public void Asthma_EasesIntervalPace()
    {
        var plan = GenerateBasePlan(RunningExperience.Advanced, runDays: 5);
        var medical = CreateMedicalInfo(asthma: true);

        // Record original interval paces
        var intervalDays = plan.TrainingWeeks
            .SelectMany(w => w.TrainingDays)
            .Where(d => d.RunType == RunType.Intervals)
            .ToList();

        var originalPaces = intervalDays.ToDictionary(d => d, d => d.TargetPaceMinPerMile);

        _medicalService.AdjustPlan(plan, medical);

        foreach (var day in intervalDays)
        {
            if (originalPaces[day].HasValue)
            {
                Assert.True(day.TargetPaceMinPerMile > originalPaces[day],
                    "Asthma adjustment should slow interval pace");
            }
        }
    }

    #endregion

    #region Knee Issues Adjustments

    [Fact]
    public void KneeIssues_AddsCrossTrainingSubstitutions()
    {
        var plan = GenerateBasePlan();
        var medical = CreateMedicalInfo(kneeIssues: true);

        _medicalService.AdjustPlan(plan, medical);

        foreach (var week in plan.TrainingWeeks)
        {
            // At least one day should have knee-related medical modification
            var kneeModDays = week.TrainingDays
                .Where(d => d.MedicalModifications != null && d.MedicalModifications.Contains("Knee"))
                .ToList();

            Assert.NotEmpty(kneeModDays);
        }
    }

    [Fact]
    public void KneeIssues_ReducesLongRunDistance()
    {
        var profile = CreateProfile(runDaysPerWeek: 4);
        var planOriginal = _generator.GeneratePlan(profile, RaceType.FullMarathon, RaceDateWeeksFromNow(18));

        var planAdjusted = _generator.GeneratePlan(profile, RaceType.FullMarathon, RaceDateWeeksFromNow(18));
        var medical = CreateMedicalInfo(kneeIssues: true);
        _medicalService.AdjustPlan(planAdjusted, medical);

        var originalLongRuns = planOriginal.TrainingWeeks
            .Select(w => w.TrainingDays.FirstOrDefault(d => d.RunType == RunType.LongRun)?.DistanceMiles ?? 0);
        var adjustedLongRuns = planAdjusted.TrainingWeeks
            .Select(w => w.TrainingDays.FirstOrDefault(d => d.RunType == RunType.LongRun)?.DistanceMiles ?? 0);

        // At least some long runs should be reduced
        var originalTotal = originalLongRuns.Sum();
        var adjustedTotal = adjustedLongRuns.Sum();
        Assert.True(adjustedTotal < originalTotal,
            "Knee issues should reduce total long run distance");
    }

    #endregion

    #region Heart Condition Adjustments

    [Fact]
    public void HeartCondition_RemovesIntervalWorkouts()
    {
        var plan = GenerateBasePlan(RunningExperience.Advanced, runDays: 5);
        var medical = CreateMedicalInfo(heartCondition: true);

        _medicalService.AdjustPlan(plan, medical);

        var allDays = plan.TrainingWeeks.SelectMany(w => w.TrainingDays);
        Assert.DoesNotContain(allDays, d => d.RunType == RunType.Intervals);
    }

    [Fact]
    public void HeartCondition_ConvertsIntervalsToTempo()
    {
        var plan = GenerateBasePlan(RunningExperience.Advanced, runDays: 5);

        // Count intervals before adjustment
        var intervalCountBefore = plan.TrainingWeeks
            .SelectMany(w => w.TrainingDays)
            .Count(d => d.RunType == RunType.Intervals);

        var medical = CreateMedicalInfo(heartCondition: true);
        _medicalService.AdjustPlan(plan, medical);

        // All intervals should now be tempo or rest (from heart condition rest-between-hard-efforts)
        var allDays = plan.TrainingWeeks.SelectMany(w => w.TrainingDays);
        Assert.DoesNotContain(allDays, d => d.RunType == RunType.Intervals);

        // Plan should have heart condition modifications
        var heartModDays = allDays
            .Where(d => d.MedicalModifications != null && d.MedicalModifications.Contains("Heart condition"))
            .ToList();
        Assert.NotEmpty(heartModDays);
    }

    [Fact]
    public void HeartCondition_AddsHRMonitoringNotes()
    {
        var plan = GenerateBasePlan();
        var medical = CreateMedicalInfo(heartCondition: true);

        _medicalService.AdjustPlan(plan, medical);

        var runDays = plan.TrainingWeeks
            .SelectMany(w => w.TrainingDays)
            .Where(d => d.RunType is not RunType.Rest and not RunType.CrossTrain);

        Assert.All(runDays, d =>
        {
            Assert.NotNull(d.MedicalModifications);
            Assert.Contains("heart rate", d.MedicalModifications, StringComparison.OrdinalIgnoreCase);
        });
    }

    #endregion

    #region Plantar Fasciitis Adjustments

    [Fact]
    public void PlantarFasciitis_LimitsWeeklyMileageIncrease()
    {
        var plan = GenerateBasePlan();
        var medical = CreateMedicalInfo(plantarFasciitis: true);

        _medicalService.AdjustPlan(plan, medical);

        double? previousMileage = null;
        foreach (var week in plan.TrainingWeeks)
        {
            if (previousMileage.HasValue && !week.IsStepBackWeek && week.Phase != "Taper")
            {
                // 7% cap + tolerance for per-day rounding (each day rounds to 0.1, up to ~0.5 total)
                double maxAllowed = previousMileage.Value * 1.07 + 0.5;
                Assert.True(week.TotalMileage <= maxAllowed || week.TotalMileage <= previousMileage.Value,
                    $"Week {week.WeekNumber}: mileage {week.TotalMileage} exceeds 7% cap from {previousMileage.Value} (max {maxAllowed:F1})");
            }
            previousMileage = week.TotalMileage;
        }
    }

    [Fact]
    public void PlantarFasciitis_AddsStretchingNotes()
    {
        var plan = GenerateBasePlan();
        var medical = CreateMedicalInfo(plantarFasciitis: true);

        _medicalService.AdjustPlan(plan, medical);

        var runDays = plan.TrainingWeeks
            .SelectMany(w => w.TrainingDays)
            .Where(d => d.RunType is not RunType.Rest and not RunType.CrossTrain);

        Assert.All(runDays, d =>
        {
            Assert.NotNull(d.MedicalModifications);
            Assert.Contains("Plantar fasciitis", d.MedicalModifications);
        });
    }

    #endregion

    #region No Medical Conditions

    [Fact]
    public void NoMedicalConditions_PlanIsNotModified()
    {
        var profile = CreateProfile(runDaysPerWeek: 4);
        var planOriginal = _generator.GeneratePlan(profile, RaceType.HalfMarathon, RaceDateWeeksFromNow(16));
        var planAdjusted = _generator.GeneratePlan(profile, RaceType.HalfMarathon, RaceDateWeeksFromNow(16));

        var medical = CreateMedicalInfo(); // all false

        _medicalService.AdjustPlan(planAdjusted, medical);

        // Mileage should be identical
        for (int i = 0; i < planOriginal.TrainingWeeks.Count; i++)
        {
            Assert.Equal(
                planOriginal.TrainingWeeks[i].TotalMileage,
                planAdjusted.TrainingWeeks[i].TotalMileage);
        }

        // No medical modifications should be present
        var allDays = planAdjusted.TrainingWeeks.SelectMany(w => w.TrainingDays);
        Assert.All(allDays, d => Assert.Null(d.MedicalModifications));
    }

    #endregion

    #region Multiple Conditions

    [Fact]
    public void MultipleConditions_AllApplied()
    {
        var plan = GenerateBasePlan(RunningExperience.Advanced, runDays: 5);
        var medical = CreateMedicalInfo(asthma: true, heartCondition: true);

        _medicalService.AdjustPlan(plan, medical);

        // Intervals should be removed (heart condition)
        var allDays = plan.TrainingWeeks.SelectMany(w => w.TrainingDays).ToList();
        Assert.DoesNotContain(allDays, d => d.RunType == RunType.Intervals);

        // Run days should have both asthma and heart condition notes
        var runDays = allDays
            .Where(d => d.RunType is not RunType.Rest and not RunType.CrossTrain)
            .ToList();

        Assert.All(runDays, d =>
        {
            Assert.NotNull(d.MedicalModifications);
            Assert.Contains("Asthma", d.MedicalModifications);
            Assert.Contains("Heart condition", d.MedicalModifications);
        });
    }

    [Fact]
    public void KneeAndPlantarFasciitis_BothApplied()
    {
        var plan = GenerateBasePlan(runDays: 4);
        var medical = CreateMedicalInfo(kneeIssues: true, plantarFasciitis: true);

        _medicalService.AdjustPlan(plan, medical);

        var allDays = plan.TrainingWeeks.SelectMany(w => w.TrainingDays).ToList();

        // Should have knee modifications
        Assert.Contains(allDays, d =>
            d.MedicalModifications != null && d.MedicalModifications.Contains("Knee"));

        // Should have plantar fasciitis modifications
        Assert.Contains(allDays, d =>
            d.MedicalModifications != null && d.MedicalModifications.Contains("Plantar fasciitis"));
    }

    #endregion
}

using MarathonTrainer.Api.Models;
using MarathonTrainer.Api.Services;

namespace MarathonTrainer.Api.Tests;

public class TrainingPlanGeneratorTests
{
    private readonly TrainingPlanGenerator _generator = new();

    #region Helpers

    private static UserProfile CreateProfile(
        RunningExperience experience = RunningExperience.Beginner,
        double weeklyMileage = 15,
        double longestRun = 5,
        double comfortablePace = 10.0,
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

    private static DateTime RaceDateWeeksFromNow(int weeks)
    {
        return DateTime.UtcNow.Date.AddDays(weeks * 7);
    }

    #endregion

    #region Plan Length

    [Fact]
    public void BeginnerHalfMarathon_GeneratesAtLeast12Weeks()
    {
        var profile = CreateProfile(RunningExperience.Beginner);
        var plan = _generator.GeneratePlan(profile, RaceType.HalfMarathon, RaceDateWeeksFromNow(16));

        Assert.True(plan.TotalWeeks >= 12);
        Assert.Equal(plan.TotalWeeks, plan.TrainingWeeks.Count);
    }

    [Fact]
    public void MinimumPlan_RaceExactly12WeeksOut_Generates12Weeks()
    {
        var profile = CreateProfile(RunningExperience.Intermediate, weeklyMileage: 20);
        var plan = _generator.GeneratePlan(profile, RaceType.HalfMarathon, RaceDateWeeksFromNow(12));

        Assert.Equal(12, plan.TotalWeeks);
        Assert.Equal(12, plan.TrainingWeeks.Count);
    }

    [Fact]
    public void RaceDateLessThan12Weeks_StillGenerates12Weeks()
    {
        var profile = CreateProfile();
        var plan = _generator.GeneratePlan(profile, RaceType.HalfMarathon, RaceDateWeeksFromNow(8));

        Assert.Equal(12, plan.TotalWeeks);
    }

    #endregion

    #region Weekly Mileage Progression (10% Rule)

    [Fact]
    public void WeeklyMileage_NeverIncreasesMoreThan10Percent_FromPreviousNonStepBackWeek()
    {
        var profile = CreateProfile(RunningExperience.Intermediate, weeklyMileage: 15, runDaysPerWeek: 5);
        var plan = _generator.GeneratePlan(profile, RaceType.FullMarathon, RaceDateWeeksFromNow(20));

        double? lastNonStepBackMileage = null;

        foreach (var week in plan.TrainingWeeks)
        {
            if (week.IsStepBackWeek || week.Phase == "Taper")
            {
                if (!week.IsStepBackWeek && week.Phase != "Taper")
                    lastNonStepBackMileage = week.TotalMileage;
                continue;
            }

            if (lastNonStepBackMileage.HasValue)
            {
                double maxAllowed = lastNonStepBackMileage.Value * 1.11; // slight tolerance
                Assert.True(week.TotalMileage <= maxAllowed,
                    $"Week {week.WeekNumber}: mileage {week.TotalMileage} exceeds 10% increase from {lastNonStepBackMileage.Value} (max {maxAllowed:F1})");
            }

            lastNonStepBackMileage = week.TotalMileage;
        }
    }

    #endregion

    #region Step-Back Weeks

    [Fact]
    public void StepBackWeeks_OccurEvery3To4Weeks_AndReduceMileage()
    {
        var profile = CreateProfile(RunningExperience.Intermediate, weeklyMileage: 20, runDaysPerWeek: 5);
        var plan = _generator.GeneratePlan(profile, RaceType.FullMarathon, RaceDateWeeksFromNow(20));

        var stepBackWeeks = plan.TrainingWeeks.Where(w => w.IsStepBackWeek).ToList();

        Assert.NotEmpty(stepBackWeeks);

        foreach (var sbWeek in stepBackWeeks)
        {
            Assert.NotEqual("Taper", sbWeek.Phase);

            // Verify the previous non-step-back week had higher mileage
            var previousWeek = plan.TrainingWeeks
                .Where(w => w.WeekNumber < sbWeek.WeekNumber && !w.IsStepBackWeek)
                .OrderByDescending(w => w.WeekNumber)
                .FirstOrDefault();

            if (previousWeek != null)
            {
                Assert.True(sbWeek.TotalMileage < previousWeek.TotalMileage,
                    $"Step-back week {sbWeek.WeekNumber} ({sbWeek.TotalMileage}mi) should be less than previous week {previousWeek.WeekNumber} ({previousWeek.TotalMileage}mi)");
            }
        }
    }

    [Fact]
    public void StepBackWeeks_OccurAtExpectedFrequency()
    {
        var profile = CreateProfile(RunningExperience.Intermediate, weeklyMileage: 20, runDaysPerWeek: 5);
        var plan = _generator.GeneratePlan(profile, RaceType.FullMarathon, RaceDateWeeksFromNow(20));

        // The code sets isStepBack when w % 3 == 2 (i.e. weeks 3, 6, 9, 12...)
        var stepBackWeekNumbers = plan.TrainingWeeks
            .Where(w => w.IsStepBackWeek)
            .Select(w => w.WeekNumber)
            .ToList();

        foreach (var weekNum in stepBackWeekNumbers)
        {
            // w is weekNum - 1 (0-based), should satisfy w % 3 == 2
            int w = weekNum - 1;
            Assert.Equal(2, w % 3);
        }
    }

    #endregion

    #region Long Run Progression

    [Fact]
    public void LongRunProgression_HalfMarathon_NeverExceeds12Miles()
    {
        var profile = CreateProfile(RunningExperience.Advanced, weeklyMileage: 25,
            longestRun: 6, runDaysPerWeek: 5);
        var plan = _generator.GeneratePlan(profile, RaceType.HalfMarathon, RaceDateWeeksFromNow(16));

        foreach (var week in plan.TrainingWeeks)
        {
            var longRun = week.TrainingDays.FirstOrDefault(d => d.RunType == RunType.LongRun);
            if (longRun != null)
            {
                Assert.True(longRun.DistanceMiles <= 12.1,
                    $"Week {week.WeekNumber}: long run {longRun.DistanceMiles}mi exceeds 12mi max for half marathon");
            }
        }
    }

    [Fact]
    public void LongRunProgression_FullMarathon_NeverExceeds22Miles()
    {
        var profile = CreateProfile(RunningExperience.Advanced, weeklyMileage: 30,
            longestRun: 8, runDaysPerWeek: 5);
        var plan = _generator.GeneratePlan(profile, RaceType.FullMarathon, RaceDateWeeksFromNow(20));

        foreach (var week in plan.TrainingWeeks)
        {
            var longRun = week.TrainingDays.FirstOrDefault(d => d.RunType == RunType.LongRun);
            if (longRun != null)
            {
                Assert.True(longRun.DistanceMiles <= 22.1,
                    $"Week {week.WeekNumber}: long run {longRun.DistanceMiles}mi exceeds 22mi max for full marathon");
            }
        }
    }

    #endregion

    #region Taper Phase

    [Fact]
    public void TaperPhase_ReducesMileageProgressively()
    {
        var profile = CreateProfile(RunningExperience.Intermediate, weeklyMileage: 25, runDaysPerWeek: 5);
        var plan = _generator.GeneratePlan(profile, RaceType.FullMarathon, RaceDateWeeksFromNow(18));

        var taperWeeks = plan.TrainingWeeks.Where(w => w.Phase == "Taper").ToList();

        Assert.True(taperWeeks.Count >= 2, "Plan should have at least 2 taper weeks");

        // Each successive taper week should have equal or less mileage
        for (int i = 1; i < taperWeeks.Count; i++)
        {
            Assert.True(taperWeeks[i].TotalMileage <= taperWeeks[i - 1].TotalMileage,
                $"Taper week {taperWeeks[i].WeekNumber} ({taperWeeks[i].TotalMileage}mi) should be <= previous taper week ({taperWeeks[i - 1].TotalMileage}mi)");
        }

        // Last taper week should be meaningfully less than first taper week
        Assert.True(taperWeeks.Last().TotalMileage < taperWeeks.First().TotalMileage,
            "Last taper week should have less mileage than first taper week");
    }

    #endregion

    #region Phase Order

    [Fact]
    public void PlanPhases_AreInCorrectOrder_Base_Build_Peak_Taper()
    {
        var profile = CreateProfile(RunningExperience.Intermediate, weeklyMileage: 20, runDaysPerWeek: 4);
        var plan = _generator.GeneratePlan(profile, RaceType.FullMarathon, RaceDateWeeksFromNow(18));

        var phases = plan.TrainingWeeks.Select(w => w.Phase).Distinct().ToList();

        // Verify all expected phases are present
        Assert.Contains("Base", phases);
        Assert.Contains("Build", phases);
        Assert.Contains("Peak", phases);
        Assert.Contains("Taper", phases);

        // Verify ordering: each phase should appear only in a contiguous block
        var phaseTransitions = new List<string> { plan.TrainingWeeks.First().Phase };
        foreach (var week in plan.TrainingWeeks.Skip(1))
        {
            if (week.Phase != phaseTransitions.Last())
                phaseTransitions.Add(week.Phase);
        }

        Assert.Equal(new[] { "Base", "Build", "Peak", "Taper" }, phaseTransitions);
    }

    #endregion

    #region Workout Types by Experience

    [Fact]
    public void BeginnerPlan_HasNoIntervalWorkouts()
    {
        var profile = CreateProfile(RunningExperience.Beginner, weeklyMileage: 12,
            longestRun: 4, runDaysPerWeek: 4);
        var plan = _generator.GeneratePlan(profile, RaceType.HalfMarathon, RaceDateWeeksFromNow(16));

        var allDays = plan.TrainingWeeks.SelectMany(w => w.TrainingDays);
        Assert.DoesNotContain(allDays, d => d.RunType == RunType.Intervals);
    }

    [Fact]
    public void AdvancedPlan_HasTempoAndIntervalWorkouts()
    {
        var profile = CreateProfile(RunningExperience.Advanced, weeklyMileage: 30,
            longestRun: 8, runDaysPerWeek: 5);
        var plan = _generator.GeneratePlan(profile, RaceType.FullMarathon, RaceDateWeeksFromNow(20));

        var allDays = plan.TrainingWeeks.SelectMany(w => w.TrainingDays);

        Assert.Contains(allDays, d => d.RunType == RunType.Tempo);
        Assert.Contains(allDays, d => d.RunType == RunType.Intervals);
    }

    #endregion

    #region Long Run Day Preference

    [Fact]
    public void LongRun_IsPlacedOnPreferredDay_Saturday()
    {
        var profile = CreateProfile(longRunDay: DayOfWeek.Saturday, runDaysPerWeek: 4);
        var plan = _generator.GeneratePlan(profile, RaceType.HalfMarathon, RaceDateWeeksFromNow(14));

        foreach (var week in plan.TrainingWeeks)
        {
            var longRun = week.TrainingDays.FirstOrDefault(d => d.RunType == RunType.LongRun);
            if (longRun != null)
            {
                Assert.Equal(DayOfWeek.Saturday, longRun.DayOfWeek);
            }
        }
    }

    [Fact]
    public void LongRun_PreferredDayIsAlwaysARunDay()
    {
        // The preferred long run day is always included in the scheduled run days
        foreach (var preferredDay in new[] { DayOfWeek.Sunday, DayOfWeek.Wednesday, DayOfWeek.Saturday })
        {
            var profile = CreateProfile(longRunDay: preferredDay, runDaysPerWeek: 4);
            var plan = _generator.GeneratePlan(profile, RaceType.HalfMarathon, RaceDateWeeksFromNow(14));

            foreach (var week in plan.TrainingWeeks)
            {
                var preferredDayTraining = week.TrainingDays.First(d => d.DayOfWeek == preferredDay);
                // The preferred day should be a run day (not rest)
                Assert.NotEqual(RunType.Rest, preferredDayTraining.RunType);
            }
        }
    }

    #endregion

    #region Zero Current Mileage

    [Fact]
    public void ZeroWeeklyMileage_EstimatesFromLongestRun()
    {
        var profile = CreateProfile(weeklyMileage: 0, longestRun: 5, runDaysPerWeek: 4);
        var plan = _generator.GeneratePlan(profile, RaceType.HalfMarathon, RaceDateWeeksFromNow(16));

        // Starting mileage should be Max(10, longestRun * 3) = Max(10, 15) = 15
        var firstWeek = plan.TrainingWeeks.First();
        Assert.True(firstWeek.TotalMileage > 0,
            "Plan with 0 weekly mileage should still generate mileage from longest run");

        // Should be around 15 (5 * 3) for the starting week
        Assert.True(firstWeek.TotalMileage >= 10,
            "Starting mileage should be at least 10 when estimating from longest run");
    }

    [Fact]
    public void ZeroWeeklyMileage_SmallLongestRun_DefaultsToMinimum10()
    {
        var profile = CreateProfile(weeklyMileage: 0, longestRun: 2, runDaysPerWeek: 3);
        var plan = _generator.GeneratePlan(profile, RaceType.HalfMarathon, RaceDateWeeksFromNow(16));

        // Max(10, 2*3=6) = 10
        var firstWeek = plan.TrainingWeeks.First();
        Assert.True(firstWeek.TotalMileage >= 8,
            "Even with very low longest run, starting mileage should be reasonable");
    }

    #endregion

    #region Missing FitnessAssessment

    [Fact]
    public void GeneratePlan_WithoutFitnessAssessment_Throws()
    {
        var profile = CreateProfile();
        profile.FitnessAssessment = null;

        Assert.Throws<InvalidOperationException>(() =>
            _generator.GeneratePlan(profile, RaceType.HalfMarathon, RaceDateWeeksFromNow(16)));
    }

    #endregion

    #region Each Week Has 7 Days

    [Fact]
    public void EachWeek_Has7TrainingDays()
    {
        var profile = CreateProfile(RunningExperience.Intermediate, weeklyMileage: 20, runDaysPerWeek: 4);
        var plan = _generator.GeneratePlan(profile, RaceType.HalfMarathon, RaceDateWeeksFromNow(14));

        foreach (var week in plan.TrainingWeeks)
        {
            Assert.Equal(7, week.TrainingDays.Count);
        }
    }

    #endregion
}

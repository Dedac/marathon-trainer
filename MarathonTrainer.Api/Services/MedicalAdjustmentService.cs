using MarathonTrainer.Api.Models;

namespace MarathonTrainer.Api.Services;

public class MedicalAdjustmentService : IMedicalAdjustmentService
{
    private const double ReducedIncreaseRate = 0.07;

    public void AdjustPlan(TrainingPlan plan, MedicalInfo medicalInfo)
    {
        if (medicalInfo.HasKneeIssues)
            ApplyKneeAdjustments(plan);

        if (medicalInfo.HasPlantarFasciitis)
            ApplyPlantarFasciitisAdjustments(plan);

        if (medicalInfo.HasAsthma)
            ApplyAsthmaAdjustments(plan);

        if (medicalInfo.HasHeartCondition)
            ApplyHeartConditionAdjustments(plan);

        if (!string.IsNullOrWhiteSpace(medicalInfo.RecentInjuries))
            ApplyRecentInjuryAdjustments(plan);
    }

    private static void ApplyKneeAdjustments(TrainingPlan plan)
    {
        foreach (var week in plan.TrainingWeeks)
        {
            // Replace one easy run with cross-training
            var easyRun = week.TrainingDays
                .FirstOrDefault(d => d.RunType == RunType.Easy);
            if (easyRun != null)
            {
                easyRun.RunType = RunType.CrossTrain;
                easyRun.DistanceMiles = 0;
                easyRun.TargetPaceMinPerMile = null;
                easyRun.TargetPaceMaxMinPerMile = null;
                easyRun.MedicalModifications = "Knee issues: replaced easy run with cross-training (cycling or swimming recommended)";
                easyRun.Notes = "Low-impact cross-training to protect knees";
            }

            // Reduce long run progression by 30%
            var longRun = week.TrainingDays
                .FirstOrDefault(d => d.RunType == RunType.LongRun);
            if (longRun != null)
            {
                longRun.DistanceMiles = Math.Round(longRun.DistanceMiles * 0.70, 1);
                AppendModification(longRun,
                    "Knee issues: long run reduced 30%. Ice knees for 15 min post-run and stretch quads/IT band.");
            }

            RecalculateWeekMileage(week);
        }
    }

    private static void ApplyPlantarFasciitisAdjustments(TrainingPlan plan)
    {
        double? previousWeekMileage = null;

        foreach (var week in plan.TrainingWeeks)
        {
            // Cap weekly mileage increase at 7%
            if (previousWeekMileage.HasValue && !week.IsStepBackWeek)
            {
                double maxMileage = previousWeekMileage.Value * (1.0 + ReducedIncreaseRate);
                double currentTotal = week.TrainingDays.Sum(d => d.DistanceMiles);
                if (currentTotal > maxMileage)
                {
                    double scaleFactor = maxMileage / currentTotal;
                    foreach (var day in week.TrainingDays.Where(d => d.DistanceMiles > 0))
                    {
                        day.DistanceMiles = Math.Round(day.DistanceMiles * scaleFactor, 1);
                    }
                    RecalculateWeekMileage(week);
                }
            }

            // Add stretching notes to all run days
            foreach (var day in week.TrainingDays.Where(d =>
                d.RunType is not RunType.Rest and not RunType.CrossTrain))
            {
                AppendModification(day,
                    "Plantar fasciitis: stretch calves and plantar fascia before and after run. Roll foot on frozen water bottle post-run.");
            }

            // Extra rest day in Base phase: convert last easy run to rest
            if (week.Phase == "Base")
            {
                var lastEasy = week.TrainingDays
                    .LastOrDefault(d => d.RunType == RunType.Easy);
                if (lastEasy != null)
                {
                    lastEasy.RunType = RunType.Rest;
                    lastEasy.DistanceMiles = 0;
                    lastEasy.TargetPaceMinPerMile = null;
                    lastEasy.TargetPaceMaxMinPerMile = null;
                    lastEasy.MedicalModifications = "Plantar fasciitis: extra rest day during Base phase";
                    lastEasy.Notes = "Rest day — roll and stretch plantar fascia";
                    RecalculateWeekMileage(week);
                }
            }

            previousWeekMileage = week.TotalMileage;
        }
    }

    private static void ApplyAsthmaAdjustments(TrainingPlan plan)
    {
        foreach (var week in plan.TrainingWeeks)
        {
            foreach (var day in week.TrainingDays)
            {
                if (day.RunType is RunType.Rest or RunType.CrossTrain)
                    continue;

                AppendModification(day,
                    "Asthma: carry inhaler on all runs. Consider indoor treadmill on high pollen/poor air quality days.");

                // Reduce interval intensity
                if (day.RunType == RunType.Intervals)
                {
                    if (day.TargetPaceMinPerMile.HasValue)
                        day.TargetPaceMinPerMile += 0.3;
                    AppendModification(day,
                        "Asthma: interval pace eased. Extend recovery between reps. Stop if wheezing occurs.");
                }
            }
        }
    }

    private static void ApplyHeartConditionAdjustments(TrainingPlan plan)
    {
        foreach (var week in plan.TrainingWeeks)
        {
            // Convert intervals to tempo (cap max effort)
            foreach (var day in week.TrainingDays.Where(d => d.RunType == RunType.Intervals))
            {
                day.RunType = RunType.Tempo;
                if (day.TargetPaceMinPerMile.HasValue)
                    day.TargetPaceMinPerMile += 0.5;
                day.MedicalModifications = "Heart condition: intervals replaced with tempo. Monitor heart rate throughout.";
                day.Notes = "Moderate tempo — do not exceed tempo effort. Wear HR monitor.";
            }

            // Add HR monitoring notes to all run days
            foreach (var day in week.TrainingDays.Where(d =>
                d.RunType is not RunType.Rest and not RunType.CrossTrain))
            {
                AppendModification(day,
                    "Heart condition: monitor heart rate. Stop immediately if experiencing chest pain, dizziness, or unusual shortness of breath.");
            }

            // Ensure mandatory rest day between harder efforts (tempo/long run)
            EnsureRestBetweenHardEfforts(week);

            RecalculateWeekMileage(week);
        }
    }

    private static void EnsureRestBetweenHardEfforts(TrainingWeek week)
    {
        var days = week.TrainingDays.OrderBy(d => d.DayOfWeek).ToList();

        for (int i = 1; i < days.Count; i++)
        {
            bool previousIsHard = days[i - 1].RunType is RunType.Tempo or RunType.LongRun;
            bool currentIsHard = days[i].RunType is RunType.Tempo or RunType.LongRun;

            if (previousIsHard && currentIsHard && days[i].RunType == RunType.Tempo)
            {
                days[i].RunType = RunType.Rest;
                days[i].DistanceMiles = 0;
                days[i].TargetPaceMinPerMile = null;
                days[i].TargetPaceMaxMinPerMile = null;
                days[i].MedicalModifications = "Heart condition: mandatory rest day between hard efforts";
                days[i].Notes = "Rest day — recovery between hard sessions";
            }
        }
    }

    private static void ApplyRecentInjuryAdjustments(TrainingPlan plan)
    {
        // Extend base phase by converting first 2 Build weeks to Base
        int converted = 0;
        foreach (var week in plan.TrainingWeeks.Where(w => w.Phase == "Build"))
        {
            if (converted >= 2) break;
            week.Phase = "Base";
            converted++;
        }

        // Apply slower progression (7% rule) and injury monitoring notes
        double? previousWeekMileage = null;

        foreach (var week in plan.TrainingWeeks)
        {
            if (previousWeekMileage.HasValue && !week.IsStepBackWeek)
            {
                double maxMileage = previousWeekMileage.Value * (1.0 + ReducedIncreaseRate);
                double currentTotal = week.TrainingDays.Sum(d => d.DistanceMiles);
                if (currentTotal > maxMileage)
                {
                    double scaleFactor = maxMileage / currentTotal;
                    foreach (var day in week.TrainingDays.Where(d => d.DistanceMiles > 0))
                    {
                        day.DistanceMiles = Math.Round(day.DistanceMiles * scaleFactor, 1);
                    }
                    RecalculateWeekMileage(week);
                }
            }

            foreach (var day in week.TrainingDays.Where(d =>
                d.RunType is not RunType.Rest and not RunType.CrossTrain))
            {
                AppendModification(day,
                    "Recent injury: monitor for pain. If pain exceeds 3/10, stop and walk. Reduce or skip if soreness persists beyond 24 hours.");
            }

            previousWeekMileage = week.TotalMileage;
        }
    }

    private static void RecalculateWeekMileage(TrainingWeek week)
    {
        week.TotalMileage = Math.Round(
            week.TrainingDays.Sum(d => d.DistanceMiles), 1);
    }

    private static void AppendModification(TrainingDay day, string modification)
    {
        if (string.IsNullOrEmpty(day.MedicalModifications))
            day.MedicalModifications = modification;
        else
            day.MedicalModifications += " | " + modification;
    }
}

using MarathonTrainer.Api.Models;

namespace MarathonTrainer.Api.Services;

public class TrainingPlanGenerator : ITrainingPlanGenerator
{
    private const int MinimumWeeks = 12;
    private const double DefaultWeeklyIncreaseRate = 0.10;
    private const double StepBackReduction = 0.25;
    private const int StepBackFrequency = 3;

    public TrainingPlan GeneratePlan(UserProfile profile, RaceType raceType, DateTime raceDate)
    {
        var fitness = profile.FitnessAssessment
            ?? throw new InvalidOperationException("UserProfile must have a FitnessAssessment to generate a plan.");

        var today = DateTime.UtcNow.Date;
        int totalWeeks = Math.Max(MinimumWeeks, (int)((raceDate.Date - today).TotalDays / 7));

        var plan = new TrainingPlan
        {
            UserProfileId = profile.Id,
            RaceType = raceType,
            RaceDate = raceDate,
            PlanStartDate = today,
            TotalWeeks = totalWeeks,
            CreatedAt = DateTime.UtcNow
        };

        var phases = AssignPhases(totalWeeks);
        double startingMileage = DetermineStartingMileage(profile, fitness);
        double peakMileage = DeterminePeakMileage(raceType, profile.RunningExperience);
        double startingLongRun = fitness.CurrentLongestRunMiles;
        double maxLongRun = DetermineMaxLongRun(raceType);

        int taperStartWeek = phases.Count(p => p != "Taper");

        double currentMileage = startingMileage;
        double currentLongRun = startingLongRun;

        for (int w = 0; w < totalWeeks; w++)
        {
            int weekNumber = w + 1;
            string phase = phases[w];
            bool isStepBack = phase != "Taper" && weekNumber > 1 && w % StepBackFrequency == StepBackFrequency - 1;

            if (phase == "Taper")
            {
                int taperWeekIndex = w - taperStartWeek;
                int taperWeeks = totalWeeks - taperStartWeek;
                (currentMileage, currentLongRun) = CalculateTaperValues(
                    peakMileage, maxLongRun, taperWeekIndex, taperWeeks);
            }
            else if (isStepBack)
            {
                currentMileage *= (1.0 - StepBackReduction);
                currentLongRun = Math.Max(startingLongRun, currentLongRun - 2.5);
            }
            else if (weekNumber > 1)
            {
                double progressFraction = (double)w / taperStartWeek;
                double targetMileage = startingMileage + (peakMileage - startingMileage) * progressFraction;
                double maxAllowed = currentMileage * (1.0 + DefaultWeeklyIncreaseRate);
                currentMileage = Math.Min(targetMileage, maxAllowed);
                currentMileage = Math.Min(currentMileage, peakMileage);

                double targetLongRun = startingLongRun + (maxLongRun - startingLongRun) * progressFraction;
                currentLongRun = Math.Min(currentLongRun + 1.5, targetLongRun);
                currentLongRun = Math.Min(currentLongRun, maxLongRun);
            }

            var week = new TrainingWeek
            {
                WeekNumber = weekNumber,
                TotalMileage = Math.Round(currentMileage, 1),
                IsStepBackWeek = isStepBack,
                Phase = phase
            };

            week.TrainingDays = BuildWeekDays(
                profile, fitness, week, currentMileage, currentLongRun, phase);

            // Recalculate total mileage from actual assigned days
            week.TotalMileage = Math.Round(
                week.TrainingDays.Sum(d => d.DistanceMiles), 1);

            plan.TrainingWeeks.Add(week);
        }

        return plan;
    }

    private static List<string> AssignPhases(int totalWeeks)
    {
        int baseWeeks = (int)Math.Round(totalWeeks * 0.30);
        int buildWeeks = (int)Math.Round(totalWeeks * 0.35);
        int peakWeeks = (int)Math.Round(totalWeeks * 0.20);
        int taperWeeks = totalWeeks - baseWeeks - buildWeeks - peakWeeks;
        taperWeeks = Math.Max(taperWeeks, 2);

        // Re-adjust if total doesn't match
        int assigned = baseWeeks + buildWeeks + peakWeeks + taperWeeks;
        if (assigned < totalWeeks) buildWeeks += totalWeeks - assigned;
        else if (assigned > totalWeeks) buildWeeks -= assigned - totalWeeks;

        var phases = new List<string>();
        phases.AddRange(Enumerable.Repeat("Base", baseWeeks));
        phases.AddRange(Enumerable.Repeat("Build", buildWeeks));
        phases.AddRange(Enumerable.Repeat("Peak", peakWeeks));
        phases.AddRange(Enumerable.Repeat("Taper", taperWeeks));
        return phases;
    }

    private static double DetermineStartingMileage(UserProfile profile, FitnessAssessment fitness)
    {
        if (profile.CurrentWeeklyMileage > 0)
            return profile.CurrentWeeklyMileage;

        return Math.Max(10, fitness.CurrentLongestRunMiles * 3);
    }

    private static double DeterminePeakMileage(RaceType raceType, RunningExperience experience)
    {
        return (raceType, experience) switch
        {
            (RaceType.HalfMarathon, RunningExperience.Beginner) => 35,
            (RaceType.HalfMarathon, RunningExperience.Intermediate) => 40,
            (RaceType.HalfMarathon, RunningExperience.Advanced) => 45,
            (RaceType.FullMarathon, RunningExperience.Beginner) => 45,
            (RaceType.FullMarathon, RunningExperience.Intermediate) => 55,
            (RaceType.FullMarathon, RunningExperience.Advanced) => 65,
            _ => 40
        };
    }

    private static double DetermineMaxLongRun(RaceType raceType)
    {
        return raceType switch
        {
            RaceType.HalfMarathon => 12,
            RaceType.FullMarathon => 22,
            _ => 12
        };
    }

    private static (double mileage, double longRun) CalculateTaperValues(
        double peakMileage, double maxLongRun, int taperWeekIndex, int taperWeeks)
    {
        // Progressive taper: 75%, 60%, 40%, then lighter if more weeks
        double[] reductions = taperWeeks switch
        {
            2 => [0.65, 0.40],
            3 => [0.75, 0.60, 0.40],
            _ => BuildTaperReductions(taperWeeks)
        };

        int idx = Math.Min(taperWeekIndex, reductions.Length - 1);
        double mileage = peakMileage * reductions[idx];
        double longRun = maxLongRun * reductions[idx];
        longRun = Math.Max(3, Math.Round(longRun, 1));
        return (Math.Round(mileage, 1), longRun);
    }

    private static double[] BuildTaperReductions(int taperWeeks)
    {
        var reductions = new double[taperWeeks];
        for (int i = 0; i < taperWeeks; i++)
        {
            double fraction = (double)(taperWeeks - i) / (taperWeeks + 1);
            reductions[i] = Math.Max(0.30, Math.Round(fraction, 2));
        }
        return reductions;
    }

    private static List<TrainingDay> BuildWeekDays(
        UserProfile profile,
        FitnessAssessment fitness,
        TrainingWeek week,
        double weeklyMileage,
        double longRunMiles,
        string phase)
    {
        var days = new List<TrainingDay>();
        int runDays = profile.PreferredRunDaysPerWeek;

        double easyPace = fitness.ComfortablePaceMinutesPerMile + 0.75;
        double tempoPace = fitness.ComfortablePaceMinutesPerMile - 0.5;
        double intervalPace = fitness.ComfortablePaceMinutesPerMile - 1.0;
        double longRunPace = fitness.ComfortablePaceMinutesPerMile + 0.5;
        double recoveryPace = fitness.ComfortablePaceMinutesPerMile + 1.5;

        longRunMiles = Math.Round(longRunMiles, 1);
        double remainingMileage = Math.Max(0, weeklyMileage - longRunMiles);

        // Determine workout types based on experience
        var workoutSlots = GetWorkoutSlots(profile.RunningExperience, phase, runDays);

        // Calculate distances for non-long-run days
        int nonLongRunDays = workoutSlots.Count(s => s != RunType.LongRun);
        var runDistances = DistributeRemainingMileage(remainingMileage, workoutSlots);

        // Assign run days across the week
        var scheduledDays = ScheduleRunDays(profile.LongRunDay, runDays);

        int slotIndex = 0;
        for (int d = 0; d < 7; d++)
        {
            var dayOfWeek = (DayOfWeek)d;
            var trainingDay = new TrainingDay { DayOfWeek = dayOfWeek };

            if (scheduledDays.Contains(dayOfWeek) && slotIndex < workoutSlots.Count)
            {
                var runType = workoutSlots[slotIndex];
                trainingDay.RunType = runType;

                if (runType == RunType.LongRun)
                {
                    trainingDay.DistanceMiles = longRunMiles;
                    trainingDay.TargetPaceMinPerMile = longRunPace;
                    trainingDay.TargetPaceMaxMinPerMile = longRunPace + 0.5;
                    trainingDay.Notes = phase == "Taper"
                        ? "Taper long run — keep easy and comfortable"
                        : "Long run — maintain steady conversational pace";
                }
                else
                {
                    trainingDay.DistanceMiles = Math.Round(runDistances[slotIndex], 1);
                    AssignPaceAndNotes(trainingDay, runType,
                        easyPace, tempoPace, intervalPace, recoveryPace, phase);
                }

                slotIndex++;
            }
            else
            {
                // Fill non-run days: prefer CrossTrain over pure Rest up to a point
                bool hasCrossTrainAlready = days.Any(dd => dd.RunType == RunType.CrossTrain);
                trainingDay.RunType = !hasCrossTrainAlready ? RunType.CrossTrain : RunType.Rest;
                trainingDay.DistanceMiles = 0;
                trainingDay.Notes = trainingDay.RunType == RunType.CrossTrain
                    ? "Cross-training: cycling, swimming, yoga, or strength work"
                    : "Rest day — focus on recovery and hydration";
            }

            days.Add(trainingDay);
        }

        return days;
    }

    private static List<RunType> GetWorkoutSlots(
        RunningExperience experience, string phase, int runDays)
    {
        var slots = new List<RunType>();

        switch (experience)
        {
            case RunningExperience.Beginner:
            {
                // 3-4 days: easy runs + long run
                int easyCount = Math.Min(runDays - 1, 3);
                for (int i = 0; i < easyCount; i++) slots.Add(RunType.Easy);
                slots.Add(RunType.LongRun);
                break;
            }
            case RunningExperience.Intermediate:
            {
                // 4-5 days: easy + tempo + long run
                int easyCount = Math.Max(2, runDays - 2);
                for (int i = 0; i < easyCount; i++) slots.Add(RunType.Easy);
                if (phase != "Base") slots.Add(RunType.Tempo);
                else slots.Add(RunType.Easy);
                slots.Add(RunType.LongRun);
                break;
            }
            case RunningExperience.Advanced:
            {
                // 5-6 days: easy + tempo + intervals + long run
                int easyCount = Math.Max(2, runDays - 3);
                for (int i = 0; i < easyCount; i++) slots.Add(RunType.Easy);
                if (phase is "Build" or "Peak")
                {
                    slots.Add(RunType.Tempo);
                    slots.Add(RunType.Intervals);
                }
                else if (phase == "Taper")
                {
                    slots.Add(RunType.Tempo);
                    slots.Add(RunType.Easy);
                }
                else
                {
                    slots.Add(RunType.Easy);
                    slots.Add(RunType.Easy);
                }
                slots.Add(RunType.LongRun);
                break;
            }
        }

        // Trim to match requested run days
        while (slots.Count > runDays) slots.RemoveAt(0);
        while (slots.Count < runDays) slots.Insert(0, RunType.Easy);

        return slots;
    }

    private static double[] DistributeRemainingMileage(
        double remainingMileage, List<RunType> workoutSlots)
    {
        var distances = new double[workoutSlots.Count];
        int nonLongCount = workoutSlots.Count(s => s != RunType.LongRun);
        if (nonLongCount == 0) return distances;

        // Weight distribution: tempo/intervals get slightly more than easy
        double totalWeight = 0;
        for (int i = 0; i < workoutSlots.Count; i++)
        {
            if (workoutSlots[i] == RunType.LongRun) continue;
            double weight = workoutSlots[i] switch
            {
                RunType.Tempo => 1.3,
                RunType.Intervals => 1.1,
                _ => 1.0
            };
            totalWeight += weight;
        }

        for (int i = 0; i < workoutSlots.Count; i++)
        {
            if (workoutSlots[i] == RunType.LongRun)
            {
                distances[i] = 0; // handled separately
                continue;
            }
            double weight = workoutSlots[i] switch
            {
                RunType.Tempo => 1.3,
                RunType.Intervals => 1.1,
                _ => 1.0
            };
            distances[i] = Math.Max(2, remainingMileage * weight / totalWeight);
        }

        return distances;
    }

    private static HashSet<DayOfWeek> ScheduleRunDays(DayOfWeek longRunDay, int runDays)
    {
        var scheduled = new HashSet<DayOfWeek> { longRunDay };

        // Spread runs as evenly as possible through the week, avoiding day-before long run for rest
        var allDays = Enum.GetValues<DayOfWeek>()
            .Where(d => d != longRunDay)
            .OrderBy(d => SpreadScore(d, longRunDay))
            .ToList();

        foreach (var day in allDays)
        {
            if (scheduled.Count >= runDays) break;
            scheduled.Add(day);
        }

        return scheduled;
    }

    private static int SpreadScore(DayOfWeek candidate, DayOfWeek longRunDay)
    {
        // Prefer days that maximize spacing from long run day
        int diff = Math.Abs((int)candidate - (int)longRunDay);
        int wrappedDiff = Math.Min(diff, 7 - diff);
        return -wrappedDiff; // negative so larger distances sort first
    }

    private static void AssignPaceAndNotes(
        TrainingDay day, RunType runType,
        double easyPace, double tempoPace, double intervalPace, double recoveryPace,
        string phase)
    {
        switch (runType)
        {
            case RunType.Easy:
                day.TargetPaceMinPerMile = easyPace;
                day.TargetPaceMaxMinPerMile = easyPace + 0.5;
                day.Notes = "Easy run — keep effort conversational";
                break;
            case RunType.Tempo:
                day.TargetPaceMinPerMile = tempoPace;
                day.TargetPaceMaxMinPerMile = tempoPace + 0.3;
                day.Notes = phase == "Taper"
                    ? "Short tempo — maintain turnover, don't push hard"
                    : "Tempo run — comfortably hard, sustained effort";
                break;
            case RunType.Intervals:
                day.TargetPaceMinPerMile = intervalPace;
                day.TargetPaceMaxMinPerMile = intervalPace + 0.3;
                day.Notes = "Interval session — warm up 1–2 miles, repeat hard efforts with recovery jogs, cool down";
                break;
            case RunType.Recovery:
                day.TargetPaceMinPerMile = recoveryPace;
                day.TargetPaceMaxMinPerMile = recoveryPace + 0.5;
                day.Notes = "Recovery run — very easy, focus on blood flow";
                break;
        }
    }
}

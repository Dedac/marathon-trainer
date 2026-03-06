using MarathonTrainer.Api.Models;

namespace MarathonTrainer.Api.Services;

public interface ITrainingPlanGenerator
{
    TrainingPlan GeneratePlan(UserProfile profile, RaceType raceType, DateTime raceDate);
}

using MarathonTrainer.Api.Models;

namespace MarathonTrainer.Api.Services;

public interface IMedicalAdjustmentService
{
    void AdjustPlan(TrainingPlan plan, MedicalInfo medicalInfo);
}

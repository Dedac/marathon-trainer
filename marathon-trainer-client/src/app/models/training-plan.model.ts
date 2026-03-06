export enum RunningExperience { Beginner = 'Beginner', Intermediate = 'Intermediate', Advanced = 'Advanced' }
export enum RaceType { HalfMarathon = 'HalfMarathon', FullMarathon = 'FullMarathon' }
export enum RunType { Easy = 'Easy', Tempo = 'Tempo', Intervals = 'Intervals', LongRun = 'LongRun', Rest = 'Rest', CrossTrain = 'CrossTrain', Recovery = 'Recovery' }

export interface MedicalInfo {
  id?: number;
  userProfileId?: number;
  hasKneeIssues: boolean;
  hasPlantarFasciitis: boolean;
  hasAsthma: boolean;
  hasHeartCondition: boolean;
  recentInjuries?: string;
  doctorClearance: boolean;
  medicationsAffectingHeartRate?: string;
}

export interface FitnessAssessment {
  id?: number;
  userProfileId?: number;
  currentLongestRunMiles: number;
  recentRaceTimeTicks?: number;
  recentRaceDistanceMiles?: number;
  restingHeartRate?: number;
  comfortablePaceMinutesPerMile: number;
  crossTrainingPreferences?: string;
}

export interface UserProfile {
  id?: number;
  name: string;
  age: number;
  weightLbs: number;
  heightInches: number;
  gender: string;
  currentWeeklyMileage: number;
  runningExperience: RunningExperience;
  preferredRunDaysPerWeek: number;
  longRunDay: number; // 0=Sunday, 6=Saturday
  medicalInfo?: MedicalInfo;
  fitnessAssessment?: FitnessAssessment;
}

export interface TrainingDay {
  id?: number;
  dayOfWeek: number;
  runType: RunType;
  distanceMiles: number;
  targetPaceMinPerMile?: number;
  targetPaceMaxMinPerMile?: number;
  notes?: string;
  medicalModifications?: string;
}

export interface TrainingWeek {
  id?: number;
  weekNumber: number;
  totalMileage: number;
  isStepBackWeek: boolean;
  phase: string;
  trainingDays: TrainingDay[];
}

export interface TrainingPlan {
  id?: number;
  userProfileId?: number;
  raceType: RaceType;
  raceDate: string;
  planStartDate: string;
  totalWeeks: number;
  createdAt?: string;
  trainingWeeks: TrainingWeek[];
}

export interface GeneratePlanRequest {
  userProfileId: number;
  raceType: RaceType;
  raceDate: string;
}

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';

import { ApiService } from '../../services/api.service';
import { RunningExperience, UserProfile } from '../../models/training-plan.model';

@Component({
  selector: 'app-user-profile-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDividerModule,
  ],
  templateUrl: './user-profile-form.html',
  styleUrl: './user-profile-form.css',
})
export class UserProfileForm implements OnInit {
  profileForm!: FormGroup;
  loading = true;
  saving = false;
  isEditing = false;
  private existingProfileId?: number;

  genderOptions = ['Male', 'Female', 'Other', 'Prefer not to say'];
  experienceOptions = Object.values(RunningExperience);
  runDaysOptions = [3, 4, 5, 6];
  daysOfWeek = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

  constructor(
    private fb: FormBuilder,
    private api: ApiService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.loadExistingProfile();
  }

  private buildForm(): void {
    this.profileForm = this.fb.group({
      // Personal info
      name: ['', Validators.required],
      age: [null, [Validators.required, Validators.min(13), Validators.max(100)]],
      weightLbs: [null, [Validators.required, Validators.min(0.1)]],
      heightInches: [null, [Validators.required, Validators.min(0.1)]],
      gender: ['', Validators.required],

      // Running info
      currentWeeklyMileage: [null, [Validators.required, Validators.min(0)]],
      runningExperience: [RunningExperience.Beginner, Validators.required],
      preferredRunDaysPerWeek: [4, [Validators.required, Validators.min(3), Validators.max(6)]],
      longRunDay: [0, Validators.required],

      // Fitness assessment
      currentLongestRunMiles: [null, [Validators.required, Validators.min(0)]],
      recentRaceDistanceMiles: [null],
      comfortablePaceMinutesPerMile: [null, [Validators.required, Validators.min(4.0), Validators.max(20.0)]],
      restingHeartRate: [null],
      crossTrainingPreferences: [''],

      // Medical info
      hasKneeIssues: [false],
      hasPlantarFasciitis: [false],
      hasAsthma: [false],
      hasHeartCondition: [false],
      recentInjuries: [''],
      doctorClearance: [false],
      medicationsAffectingHeartRate: [''],
    });
  }

  private loadExistingProfile(): void {
    this.api.getUserProfile().subscribe({
      next: (profile) => {
        this.isEditing = true;
        this.existingProfileId = profile.id;
        this.populateForm(profile);
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  private populateForm(profile: UserProfile): void {
    this.profileForm.patchValue({
      name: profile.name,
      age: profile.age,
      weightLbs: profile.weightLbs,
      heightInches: profile.heightInches,
      gender: profile.gender,
      currentWeeklyMileage: profile.currentWeeklyMileage,
      runningExperience: profile.runningExperience,
      preferredRunDaysPerWeek: profile.preferredRunDaysPerWeek,
      longRunDay: profile.longRunDay,
    });

    if (profile.fitnessAssessment) {
      this.profileForm.patchValue({
        currentLongestRunMiles: profile.fitnessAssessment.currentLongestRunMiles,
        recentRaceDistanceMiles: profile.fitnessAssessment.recentRaceDistanceMiles,
        comfortablePaceMinutesPerMile: profile.fitnessAssessment.comfortablePaceMinutesPerMile,
        restingHeartRate: profile.fitnessAssessment.restingHeartRate,
        crossTrainingPreferences: profile.fitnessAssessment.crossTrainingPreferences,
      });
    }

    if (profile.medicalInfo) {
      this.profileForm.patchValue({
        hasKneeIssues: profile.medicalInfo.hasKneeIssues,
        hasPlantarFasciitis: profile.medicalInfo.hasPlantarFasciitis,
        hasAsthma: profile.medicalInfo.hasAsthma,
        hasHeartCondition: profile.medicalInfo.hasHeartCondition,
        recentInjuries: profile.medicalInfo.recentInjuries,
        doctorClearance: profile.medicalInfo.doctorClearance,
        medicationsAffectingHeartRate: profile.medicalInfo.medicationsAffectingHeartRate,
      });
    }
  }

  onSubmit(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.saving = true;
    const formValue = this.profileForm.value;

    const profile: UserProfile = {
      id: this.existingProfileId,
      name: formValue.name,
      age: formValue.age,
      weightLbs: formValue.weightLbs,
      heightInches: formValue.heightInches,
      gender: formValue.gender,
      currentWeeklyMileage: formValue.currentWeeklyMileage,
      runningExperience: formValue.runningExperience,
      preferredRunDaysPerWeek: formValue.preferredRunDaysPerWeek,
      longRunDay: formValue.longRunDay,
      fitnessAssessment: {
        currentLongestRunMiles: formValue.currentLongestRunMiles,
        recentRaceDistanceMiles: formValue.recentRaceDistanceMiles || undefined,
        comfortablePaceMinutesPerMile: formValue.comfortablePaceMinutesPerMile,
        restingHeartRate: formValue.restingHeartRate || undefined,
        crossTrainingPreferences: formValue.crossTrainingPreferences || undefined,
      },
      medicalInfo: {
        hasKneeIssues: formValue.hasKneeIssues,
        hasPlantarFasciitis: formValue.hasPlantarFasciitis,
        hasAsthma: formValue.hasAsthma,
        hasHeartCondition: formValue.hasHeartCondition,
        recentInjuries: formValue.recentInjuries || undefined,
        doctorClearance: formValue.doctorClearance,
        medicationsAffectingHeartRate: formValue.medicationsAffectingHeartRate || undefined,
      },
    };

    const save$ = this.isEditing
      ? this.api.updateUserProfile(profile)
      : this.api.saveUserProfile(profile);

    save$.subscribe({
      next: () => {
        this.saving = false;
        this.router.navigate(['/generate']);
      },
      error: () => {
        this.saving = false;
      },
    });
  }
}

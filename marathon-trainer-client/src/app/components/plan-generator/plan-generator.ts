import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatListModule } from '@angular/material/list';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';

import { ApiService } from '../../services/api.service';
import {
  UserProfile,
  RaceType,
  GeneratePlanRequest,
} from '../../models/training-plan.model';

interface MedicalCondition {
  label: string;
  active: boolean;
}

@Component({
  selector: 'app-plan-generator',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatListModule,
    MatSnackBarModule,
    MatIconModule,
  ],
  providers: [provideNativeDateAdapter()],
  templateUrl: './plan-generator.html',
  styleUrl: './plan-generator.css',
})
export class PlanGenerator implements OnInit {
  readonly RaceType = RaceType;

  profile: UserProfile | null = null;
  loading = true;
  generatingPlan = false;
  errorMessage = '';
  activeConditions: MedicalCondition[] = [];
  minRaceDate: Date;
  planForm: FormGroup;

  private static readonly DAY_LABELS = [
    'Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday',
  ];

  constructor(
    private api: ApiService,
    private fb: FormBuilder,
    private router: Router,
    private snackBar: MatSnackBar,
  ) {
    const today = new Date();
    this.minRaceDate = new Date(today.getFullYear(), today.getMonth(), today.getDate() + 84); // 12 weeks

    this.planForm = this.fb.group({
      raceType: [RaceType.HalfMarathon, Validators.required],
      raceDate: [null, Validators.required],
    });
  }

  ngOnInit(): void {
    this.api.getUserProfile().subscribe({
      next: (profile) => {
        this.profile = profile;
        this.activeConditions = this.buildConditions(profile);
        this.loading = false;
      },
      error: () => {
        this.profile = null;
        this.loading = false;
      },
    });
  }

  dayOfWeekLabel(day: number): string {
    return PlanGenerator.DAY_LABELS[day] ?? 'Unknown';
  }

  onGenerate(): void {
    if (this.planForm.invalid || !this.profile?.id) return;

    this.generatingPlan = true;
    this.errorMessage = '';

    const raceDate: Date = this.planForm.value.raceDate;
    const request: GeneratePlanRequest = {
      userProfileId: this.profile.id,
      raceType: this.planForm.value.raceType,
      raceDate: raceDate.toISOString(),
    };

    this.api.generatePlan(request).subscribe({
      next: () => {
        this.generatingPlan = false;
        this.snackBar.open('Training plan generated!', 'OK', { duration: 3000 });
        this.router.navigate(['/plan']);
      },
      error: (err) => {
        this.generatingPlan = false;
        this.errorMessage =
          err?.error?.message ?? 'Failed to generate plan. Please try again.';
        this.snackBar.open(this.errorMessage, 'Dismiss', { duration: 5000 });
      },
    });
  }

  private buildConditions(profile: UserProfile): MedicalCondition[] {
    const medical = profile.medicalInfo;
    if (!medical) return [];

    const conditions: MedicalCondition[] = [];
    if (medical.hasKneeIssues) conditions.push({ label: 'Knee Issues', active: true });
    if (medical.hasPlantarFasciitis) conditions.push({ label: 'Plantar Fasciitis', active: true });
    if (medical.hasAsthma) conditions.push({ label: 'Asthma', active: true });
    if (medical.hasHeartCondition) conditions.push({ label: 'Heart Condition', active: true });
    if (medical.recentInjuries) conditions.push({ label: `Injury: ${medical.recentInjuries}`, active: true });

    return conditions;
  }
}

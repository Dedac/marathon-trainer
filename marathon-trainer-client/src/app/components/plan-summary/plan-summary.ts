import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { ApiService } from '../../services/api.service';
import {
  TrainingPlan,
  TrainingWeek,
  RaceType,
  RunType,
} from '../../models/training-plan.model';

interface WeekChartEntry {
  weekNumber: number;
  mileage: number;
  phase: string;
  barWidthPercent: number;
}

interface PhaseBreakdown {
  phase: string;
  weeks: number;
  percent: number;
}

interface RunTypeCount {
  runType: string;
  count: number;
}

interface LongRunEntry {
  weekNumber: number;
  distance: number;
}

@Component({
  selector: 'app-plan-summary',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatChipsModule,
    MatDividerModule,
    MatProgressBarModule,
    MatTableModule,
  ],
  templateUrl: './plan-summary.html',
  styleUrl: './plan-summary.css',
})
export class PlanSummary implements OnInit {
  plan: TrainingPlan | null = null;
  loading = true;
  error = false;

  // Stat cards
  totalMiles = 0;
  peakWeekNumber = 0;
  peakWeekMileage = 0;
  totalWeeks = 0;
  weeksUntilRace = 0;
  averageWeeklyMileage = 0;

  // Race info
  raceTypeLabel = '';
  raceDateFormatted = '';

  // Chart data
  weeklyChart: WeekChartEntry[] = [];

  // Phase breakdown
  phases: PhaseBreakdown[] = [];

  // Run type distribution
  runTypeCounts: RunTypeCount[] = [];

  // Long run progression
  longRuns: LongRunEntry[] = [];

  // Medical modifications
  medicalModifications: string[] = [];

  readonly phaseColors: Record<string, string> = {
    Base: '#1976d2',
    Build: '#f57c00',
    Peak: '#d32f2f',
    Taper: '#388e3c',
  };

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.apiService.getLatestPlan().subscribe({
      next: (plan) => {
        this.plan = plan;
        this.computeStats(plan);
        this.loading = false;
      },
      error: () => {
        this.plan = null;
        this.loading = false;
        this.error = true;
      },
    });
  }

  private computeStats(plan: TrainingPlan): void {
    const weeks = plan.trainingWeeks ?? [];

    // Race info
    this.raceTypeLabel =
      plan.raceType === RaceType.HalfMarathon ? 'Half Marathon' : 'Full Marathon';
    this.raceDateFormatted = new Date(plan.raceDate).toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
    this.totalWeeks = plan.totalWeeks;

    const now = new Date();
    const raceDate = new Date(plan.raceDate);
    const diffMs = raceDate.getTime() - now.getTime();
    this.weeksUntilRace = Math.max(0, Math.ceil(diffMs / (7 * 24 * 60 * 60 * 1000)));

    // Total miles
    this.totalMiles = weeks.reduce((sum, w) => sum + w.totalMileage, 0);
    this.totalMiles = Math.round(this.totalMiles * 10) / 10;

    // Average weekly mileage
    this.averageWeeklyMileage =
      weeks.length > 0
        ? Math.round((this.totalMiles / weeks.length) * 10) / 10
        : 0;

    // Peak week
    let peakWeek: TrainingWeek | null = null;
    for (const w of weeks) {
      if (!peakWeek || w.totalMileage > peakWeek.totalMileage) {
        peakWeek = w;
      }
    }
    this.peakWeekNumber = peakWeek?.weekNumber ?? 0;
    this.peakWeekMileage = peakWeek?.totalMileage ?? 0;

    // Weekly chart
    const maxMileage = this.peakWeekMileage || 1;
    this.weeklyChart = weeks.map((w) => ({
      weekNumber: w.weekNumber,
      mileage: Math.round(w.totalMileage * 10) / 10,
      phase: w.phase,
      barWidthPercent: (w.totalMileage / maxMileage) * 100,
    }));

    // Phase breakdown
    const phaseCounts = new Map<string, number>();
    for (const w of weeks) {
      phaseCounts.set(w.phase, (phaseCounts.get(w.phase) ?? 0) + 1);
    }
    const phaseOrder = ['Base', 'Build', 'Peak', 'Taper'];
    this.phases = phaseOrder
      .filter((p) => phaseCounts.has(p))
      .map((p) => ({
        phase: p,
        weeks: phaseCounts.get(p)!,
        percent:
          weeks.length > 0
            ? Math.round((phaseCounts.get(p)! / weeks.length) * 100)
            : 0,
      }));

    // Run type distribution
    const runCounts = new Map<string, number>();
    for (const w of weeks) {
      for (const d of w.trainingDays ?? []) {
        runCounts.set(d.runType, (runCounts.get(d.runType) ?? 0) + 1);
      }
    }
    this.runTypeCounts = Array.from(runCounts.entries())
      .map(([runType, count]) => ({ runType, count }))
      .sort((a, b) => b.count - a.count);

    // Long run progression
    this.longRuns = weeks
      .map((w) => {
        const longRun = (w.trainingDays ?? []).find((d) => d.runType === RunType.LongRun);
        return longRun
          ? { weekNumber: w.weekNumber, distance: longRun.distanceMiles }
          : null;
      })
      .filter((entry): entry is LongRunEntry => entry !== null);

    // Medical modifications
    const modsSet = new Set<string>();
    for (const w of weeks) {
      for (const d of w.trainingDays ?? []) {
        if (d.medicalModifications) {
          modsSet.add(d.medicalModifications);
        }
      }
    }
    this.medicalModifications = Array.from(modsSet);
  }

  getPhaseColor(phase: string): string {
    return this.phaseColors[phase] ?? '#9e9e9e';
  }

  getRunTypeColor(runType: string): string {
    const colors: Record<string, string> = {
      [RunType.Easy]: '#4caf50',
      [RunType.Tempo]: '#ff9800',
      [RunType.Intervals]: '#f44336',
      [RunType.LongRun]: '#2196f3',
      [RunType.Rest]: '#9e9e9e',
      [RunType.CrossTrain]: '#9c27b0',
      [RunType.Recovery]: '#00bcd4',
    };
    return colors[runType] ?? '#757575';
  }
}

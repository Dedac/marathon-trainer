import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatBadgeModule } from '@angular/material/badge';
import { MatTooltipModule } from '@angular/material/tooltip';

import { ApiService } from '../../services/api.service';
import {
  TrainingPlan,
  TrainingWeek,
  TrainingDay,
  RunType,
} from '../../models/training-plan.model';

const PAGE_SIZE = 4;

@Component({
  selector: 'app-training-calendar',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatExpansionModule,
    MatProgressSpinnerModule,
    MatBadgeModule,
    MatTooltipModule,
  ],
  templateUrl: './training-calendar.html',
  styleUrl: './training-calendar.css',
})
export class TrainingCalendar implements OnInit {
  plan: TrainingPlan | null = null;
  loading = true;
  errorMessage = '';

  pageIndex = 0;
  showAllWeeks = false;
  expandedDayId: string | null = null;

  private readonly phases = ['Base', 'Build', 'Peak', 'Taper'];

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.apiService.getLatestPlan().subscribe({
      next: (plan) => {
        this.plan = plan;
        this.loading = false;
        this.jumpToCurrentWeek();
      },
      error: () => {
        this.plan = null;
        this.loading = false;
      },
    });
  }

  // --- Pagination ---

  get totalPages(): number {
    if (!this.plan) return 0;
    return Math.ceil(this.plan.weeks.length / PAGE_SIZE);
  }

  get visibleWeeks(): TrainingWeek[] {
    if (!this.plan) return [];
    if (this.showAllWeeks) return this.plan.weeks;
    const start = this.pageIndex * PAGE_SIZE;
    return this.plan.weeks.slice(start, start + PAGE_SIZE);
  }

  get canGoPrev(): boolean {
    return this.pageIndex > 0;
  }

  get canGoNext(): boolean {
    return this.pageIndex < this.totalPages - 1;
  }

  prevPage(): void {
    if (this.canGoPrev) this.pageIndex--;
  }

  nextPage(): void {
    if (this.canGoNext) this.pageIndex++;
  }

  toggleShowAll(): void {
    this.showAllWeeks = !this.showAllWeeks;
  }

  // --- Phase progress ---

  get allPhases(): string[] {
    return this.phases;
  }

  get currentPhase(): string {
    if (!this.plan) return '';
    const today = new Date();
    const start = new Date(this.plan.planStartDate);
    const diffMs = today.getTime() - start.getTime();
    const currentWeekNum = Math.floor(diffMs / (7 * 24 * 60 * 60 * 1000)) + 1;
    const week = this.plan.weeks.find((w) => w.weekNumber === currentWeekNum);
    return week?.phase ?? this.plan.weeks[this.plan.weeks.length - 1]?.phase ?? '';
  }

  isPhaseActive(phase: string): boolean {
    return this.currentPhase.toLowerCase() === phase.toLowerCase();
  }

  isPhaseCompleted(phase: string): boolean {
    const currentIdx = this.phases.findIndex(
      (p) => p.toLowerCase() === this.currentPhase.toLowerCase(),
    );
    const phaseIdx = this.phases.findIndex(
      (p) => p.toLowerCase() === phase.toLowerCase(),
    );
    return currentIdx > phaseIdx;
  }

  // --- Week helpers ---

  isCurrentWeek(week: TrainingWeek): boolean {
    if (!this.plan) return false;
    const today = new Date();
    const start = new Date(this.plan.planStartDate);
    const diffMs = today.getTime() - start.getTime();
    const currentWeekNum = Math.floor(diffMs / (7 * 24 * 60 * 60 * 1000)) + 1;
    return week.weekNumber === currentWeekNum;
  }

  getWeekDays(week: TrainingWeek): TrainingDay[] {
    const days: TrainingDay[] = [];
    for (let d = 0; d < 7; d++) {
      const existing = week.days.find((day) => day.dayOfWeek === d);
      days.push(
        existing ?? {
          dayOfWeek: d,
          runType: RunType.Rest,
          distanceMiles: 0,
        },
      );
    }
    return days;
  }

  // --- Day helpers ---

  getDayName(dayOfWeek: number): string {
    const names = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
    return names[dayOfWeek] ?? '';
  }

  getDayCssClass(day: TrainingDay): string {
    const typeMap: Record<string, string> = {
      [RunType.Easy]: 'run-easy',
      [RunType.Tempo]: 'run-tempo',
      [RunType.Intervals]: 'run-intervals',
      [RunType.LongRun]: 'run-longrun',
      [RunType.Rest]: 'run-rest',
      [RunType.CrossTrain]: 'run-crosstrain',
      [RunType.Recovery]: 'run-recovery',
    };
    return typeMap[day.runType] ?? 'run-rest';
  }

  getRunTypeColor(runType: string): string {
    const colorMap: Record<string, string> = {
      [RunType.Easy]: '#4caf50',
      [RunType.Tempo]: '#ff9800',
      [RunType.Intervals]: '#f44336',
      [RunType.LongRun]: '#2196f3',
      [RunType.Rest]: '#9e9e9e',
      [RunType.CrossTrain]: '#9c27b0',
      [RunType.Recovery]: '#81c784',
    };
    return colorMap[runType] ?? '#9e9e9e';
  }

  getRunTypeIcon(runType: string): string {
    const iconMap: Record<string, string> = {
      [RunType.Easy]: 'directions_run',
      [RunType.Tempo]: 'speed',
      [RunType.Intervals]: 'timer',
      [RunType.LongRun]: 'hiking',
      [RunType.Rest]: 'hotel',
      [RunType.CrossTrain]: 'fitness_center',
      [RunType.Recovery]: 'self_improvement',
    };
    return iconMap[runType] ?? 'help_outline';
  }

  formatPace(minutesPerMile: number): string {
    const whole = Math.floor(minutesPerMile);
    const seconds = Math.round((minutesPerMile - whole) * 60);
    return `${whole}:${seconds.toString().padStart(2, '0')}`;
  }

  getPaceDisplay(day: TrainingDay): string {
    if (!day.targetPaceMinPerMile) return '';
    const min = this.formatPace(day.targetPaceMinPerMile);
    if (!day.targetPaceMaxMinPerMile) return min;
    return `${min} – ${this.formatPace(day.targetPaceMaxMinPerMile)}`;
  }

  hasDetails(day: TrainingDay): boolean {
    return !!(day.notes || day.medicalModifications);
  }

  dayTrackId(_index: number, day: TrainingDay): number {
    return day.dayOfWeek;
  }

  weekTrackId(_index: number, week: TrainingWeek): number {
    return week.weekNumber;
  }

  toggleDayExpansion(weekNumber: number, dayOfWeek: number): void {
    const id = `${weekNumber}-${dayOfWeek}`;
    this.expandedDayId = this.expandedDayId === id ? null : id;
  }

  isDayExpanded(weekNumber: number, dayOfWeek: number): boolean {
    return this.expandedDayId === `${weekNumber}-${dayOfWeek}`;
  }

  getPhaseColor(phase: string): string {
    const colorMap: Record<string, string> = {
      base: '#4caf50',
      build: '#ff9800',
      peak: '#f44336',
      taper: '#2196f3',
    };
    return colorMap[phase.toLowerCase()] ?? '#9e9e9e';
  }

  // --- Navigation helper ---

  private jumpToCurrentWeek(): void {
    if (!this.plan) return;
    const today = new Date();
    const start = new Date(this.plan.planStartDate);
    const diffMs = today.getTime() - start.getTime();
    const currentWeekNum = Math.floor(diffMs / (7 * 24 * 60 * 60 * 1000)) + 1;
    const weekIdx = this.plan.weeks.findIndex(
      (w) => w.weekNumber === currentWeekNum,
    );
    if (weekIdx >= 0) {
      this.pageIndex = Math.floor(weekIdx / PAGE_SIZE);
    }
  }
}

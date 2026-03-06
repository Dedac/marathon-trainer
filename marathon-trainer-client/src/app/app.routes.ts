import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: '/profile', pathMatch: 'full' },
  {
    path: 'profile',
    loadComponent: () => import('./components/user-profile-form/user-profile-form').then(m => m.UserProfileForm),
  },
  {
    path: 'generate',
    loadComponent: () => import('./components/plan-generator/plan-generator').then(m => m.PlanGenerator),
  },
  {
    path: 'plan',
    loadComponent: () => import('./components/training-calendar/training-calendar').then(m => m.TrainingCalendar),
  },
  {
    path: 'summary',
    loadComponent: () => import('./components/plan-summary/plan-summary').then(m => m.PlanSummary),
  },
];

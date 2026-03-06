import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  UserProfile,
  TrainingPlan,
  GeneratePlanRequest,
} from '../models/training-plan.model';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private baseUrl = '/api';

  constructor(private http: HttpClient) {}

  getUserProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.baseUrl}/userprofile`);
  }

  saveUserProfile(profile: UserProfile): Observable<UserProfile> {
    return this.http.post<UserProfile>(`${this.baseUrl}/userprofile`, profile);
  }

  updateUserProfile(profile: UserProfile): Observable<UserProfile> {
    return this.http.put<UserProfile>(`${this.baseUrl}/userprofile/${profile.id}`, profile);
  }

  generatePlan(request: GeneratePlanRequest): Observable<TrainingPlan> {
    return this.http.post<TrainingPlan>(`${this.baseUrl}/trainingplan/generate`, request);
  }

  getTrainingPlan(id: number): Observable<TrainingPlan> {
    return this.http.get<TrainingPlan>(`${this.baseUrl}/trainingplan/${id}`);
  }

  getLatestPlan(): Observable<TrainingPlan> {
    return this.http.get<TrainingPlan>(`${this.baseUrl}/trainingplan/latest`);
  }
}

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ScoreResponse, ScoredJob } from '../models/job.models';
import { AuthService } from './auth';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private readonly baseUrl = '/api/offers';

  getScoredJobs(): Observable<ScoreResponse> {
    return this.http.get<ScoreResponse>(`${this.baseUrl}/score`);
  }

  async *streamScoredJobs(categories: string[], experienceLevels: string[], minScore: number): AsyncGenerator<ScoredJob> {
    const token = this.auth.getToken();

    const savedProfile = localStorage.getItem('candidate_profile');
    const profile = savedProfile ? JSON.parse(savedProfile) : null;

    const body: any = {
      filters: { categories, experienceLevels },
      minScore: minScore,
      forceRescore: false,
    };

    if (profile) {
      body.profile = profile;
    }

    const response = await fetch(`${this.baseUrl}/score/stream`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify(body),
    });

    if (!response.ok) throw new Error(`HTTP ${response.status}`);

    const reader = response.body!.getReader();
    const decoder = new TextDecoder();
    let buffer = '';

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split('\n');
      buffer = lines.pop() ?? '';

      for (const line of lines) {
        const trimmed = line.trim();
        if (!trimmed) continue;
        try {
          yield JSON.parse(trimmed) as ScoredJob;
        } catch {
          console.warn('Не удалось распарсить строку:', trimmed);
        }
      }
    }
  }
}

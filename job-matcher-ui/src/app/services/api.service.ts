import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CandidateProfile, ScoreRequest, ScoredJob } from '../models/job.models';
import { AuthService } from './auth';
import { ApiError, errorMessage } from './api-error';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private readonly baseUrl = `${environment.apiUrl}/api`;

  uploadCv(file: File): Observable<CandidateProfile> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<CandidateProfile>(`${this.baseUrl}/cv/upload`, formData);
  }

  /**
   * Streams scored jobs from the NDJSON endpoint as they arrive.
   * HttpClient can't hand out the body line by line, so this uses fetch and
   * does what the auth interceptor would: adds the token and logs out on 401.
   */
  async *streamScoredJobs(request: ScoreRequest, signal: AbortSignal): AsyncGenerator<ScoredJob> {
    const token = this.auth.getToken();

    const response = await fetch(`${this.baseUrl}/offers/score/stream`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify(request),
      signal,
    });

    if (response.status === 401) {
      this.auth.logout();
      throw new ApiError(401, 'Your session has expired. Please log in again.');
    }

    if (!response.ok) {
      const problem = await response.json().catch(() => null);
      throw new ApiError(response.status, errorMessage(response.status, problem, 'Failed to load jobs.'));
    }

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
          console.warn('Could not parse line:', trimmed);
        }
      }
    }
  }
}

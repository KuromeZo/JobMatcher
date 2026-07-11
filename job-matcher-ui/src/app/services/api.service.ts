import { Injectable, inject} from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ScoreResponse } from '../models/job.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/offers';

  getScoredJobs(): Observable<ScoreResponse> {
      return this.http.get<ScoreResponse>(`${this.baseUrl}/score`);
  }
}

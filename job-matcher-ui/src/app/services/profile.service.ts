import { Injectable, inject } from '@angular/core';
import { CandidateProfile } from '../models/job.models';
import { AuthService } from './auth';

/** Candidate profile kept in localStorage, separately for each user of the browser. */
@Injectable({ providedIn: 'root' })
export class ProfileService {
  private auth = inject(AuthService);

  load(): CandidateProfile | null {
    const saved = localStorage.getItem(this.storageKey());
    if (!saved) return null;

    try {
      return JSON.parse(saved) as CandidateProfile;
    } catch {
      return null;
    }
  }

  save(profile: CandidateProfile): void {
    localStorage.setItem(this.storageKey(), JSON.stringify(profile));
  }

  private storageKey(): string {
    const userId = this.auth.getUserId();
    return userId ? `candidate_profile_${userId}` : 'candidate_profile_anonymous';
  }
}

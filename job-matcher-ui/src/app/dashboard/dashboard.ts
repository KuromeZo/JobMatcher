import { Component, DestroyRef, signal, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../services/api.service';
import { ProfileService } from '../services/profile.service';
import { toApiError } from '../services/api-error';
import { ScoredJob } from '../models/job.models';
import { JobCard } from '../job-card/job-card';
import { ButtonModule } from 'primeng/button';
import { SliderModule } from 'primeng/slider';
import { CheckboxModule } from 'primeng/checkbox';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { FormsModule } from '@angular/forms';

const ALL_CATEGORIES = [
  { value: 'net', label: '.NET' },
  { value: 'java', label: 'Java' },
  { value: 'javascript', label: 'JavaScript' },
  { value: 'data', label: 'Data' },
  { value: 'python', label: 'Python' },
  { value: 'devops', label: 'DevOps' },
];

const ALL_LEVELS = [
  { value: 'intern', label: 'Intern' },
  { value: 'junior', label: 'Junior' },
  { value: 'mid', label: 'Mid' },
];

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, JobCard, ButtonModule, SliderModule, CheckboxModule, ProgressSpinnerModule, FormsModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private api = inject(ApiService);
  private abortController: AbortController | null = null;

  readonly allCategories = ALL_CATEGORIES;
  readonly allLevels = ALL_LEVELS;
  readonly profile = inject(ProfileService).load();
  readonly hasProfile = (this.profile?.skills.length ?? 0) > 0;

  jobs = signal<ScoredJob[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  hasSearched = signal(false);

  selectedCategories = signal<string[]>(['net']);
  selectedLevels = signal<string[]>(['junior']);

  minScore = signal(6);
  remoteOnly = signal(false);

  // Cached jobs arrive first and new ones as they are scored, so the order is fixed up here
  filteredJobs = computed(() => {
    const remote = this.remoteOnly();
    return this.jobs()
      .filter(j => !remote || j.offer.workplaceType === 'remote')
      .sort((a, b) => b.score - a.score);
  });

  constructor() {
    // Leaving the page stops the request, and the backend stops scoring along with it
    inject(DestroyRef).onDestroy(() => this.abortController?.abort());
  }

  isCategorySelected(value: string): boolean {
    return this.selectedCategories().includes(value);
  }

  toggleCategory(value: string): void {
    this.selectedCategories.set(toggle(this.selectedCategories(), value));
  }

  isLevelSelected(value: string): boolean {
    return this.selectedLevels().includes(value);
  }

  toggleLevel(value: string): void {
    this.selectedLevels.set(toggle(this.selectedLevels(), value));
  }

  async loadJobs(forceRescore: boolean = false): Promise<void> {
    if (!this.profile || !this.hasProfile) return;

    this.abortController?.abort();
    const controller = new AbortController();
    this.abortController = controller;

    this.hasSearched.set(true);
    this.loading.set(true);
    this.error.set(null);
    this.jobs.set([]);

    try {
      const request = {
        profile: this.profile,
        filters: {
          categories: this.selectedCategories(),
          experienceLevels: this.selectedLevels(),
        },
        minScore: this.minScore(),
        forceRescore,
      };

      for await (const job of this.api.streamScoredJobs(request, controller.signal)) {
        this.jobs.update(list => [...list, job]);
      }
    } catch (err) {
      if (!controller.signal.aborted) {
        this.error.set(toApiError(err, 'Failed to load jobs.').message);
        console.error(err);
      }
    } finally {
      if (this.abortController === controller) {
        this.abortController = null;
        this.loading.set(false);
      }
    }
  }

  rescoreJobs(): void {
    this.loadJobs(true);
  }
}

/** Adds or removes a value, but never removes the last one. */
function toggle(values: string[], value: string): string[] {
  if (!values.includes(value)) return [...values, value];
  return values.length === 1 ? values : values.filter(v => v !== value);
}

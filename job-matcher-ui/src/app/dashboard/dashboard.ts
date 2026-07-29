import { Component, signal, computed, inject } from '@angular/core';
import { ApiService } from '../services/api.service';
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
  imports: [JobCard, ButtonModule, SliderModule, CheckboxModule, ProgressSpinnerModule, FormsModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private api = inject(ApiService);

  readonly allCategories = ALL_CATEGORIES;
  readonly allLevels = ALL_LEVELS;

  jobs = signal<ScoredJob[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  loadedCount = signal(0);
  hasSearched = signal(false);

  selectedCategories = signal<string[]>(['net']);
  selectedLevels = signal<string[]>(['junior']);

  minScore = signal(6);
  remoteOnly = signal(false);

  filteredJobs = computed(() => {
    const remote = this.remoteOnly();
    return this.jobs().filter((j) => {
      if (remote && j.offer.workplaceType !== 'remote') return false;
      return true;
    });
  });

  isCategorySelected(value: string): boolean {
    return this.selectedCategories().includes(value);
  }

  toggleCategory(value: string): void {
    const current = this.selectedCategories();
    if (current.includes(value)) {
      if (current.length === 1) return;
      this.selectedCategories.set(current.filter(c => c !== value));
    } else {
      this.selectedCategories.set([...current, value]);
    }
  }

  isLevelSelected(value: string): boolean {
    return this.selectedLevels().includes(value);
  }

  toggleLevel(value: string): void {
    const current = this.selectedLevels();
    if (current.includes(value)) {
      if (current.length === 1) return;
      this.selectedLevels.set(current.filter(l => l !== value));
    } else {
      this.selectedLevels.set([...current, value]);
    }
  }

  async loadJobs(forceRescore: boolean = false): Promise<void> {
    this.hasSearched.set(true);
    this.loading.set(true);
    this.error.set(null);
    this.jobs.set([]);
    this.loadedCount.set(0);

    try {
      for await (const job of this.api.streamScoredJobs(
        this.selectedCategories(),
        this.selectedLevels(),
        this.minScore(),
        forceRescore
      )) {
        this.jobs.update(list => [...list, job]);
        this.loadedCount.update(n => n + 1);
      }
    } catch (err) {
      this.error.set('Failed to load jobs. Is the backend running?');
      console.error(err);
    } finally {
      this.loading.set(false);
    }
  }

  rescoreJobs(): void {
    this.loadJobs(true);
  }
}

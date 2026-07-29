import { Component, input } from '@angular/core';
import { ScoredJob } from '../models/job.models';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-job-card',
  imports: [CardModule, TagModule, ButtonModule],
  templateUrl: './job-card.html',
  styleUrl: './job-card.scss',
})
export class JobCard {
  job = input.required<ScoredJob>();

  get scoreSeverity(): 'success' | 'warn' | 'danger' {
    const s = this.job().score;
    if (s >= 8) return 'success';
    if (s >= 6) return 'warn';
    return 'danger';
  }
}

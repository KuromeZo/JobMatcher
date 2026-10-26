import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { DividerModule } from 'primeng/divider';
import { ApiService } from '../services/api.service';
import { ProfileService } from '../services/profile.service';
import { toApiError } from '../services/api-error';
import { CandidateProfile } from '../models/job.models';

@Component({
  selector: 'app-profile-editor',
  imports: [
    ReactiveFormsModule,
    CardModule, InputTextModule, TextareaModule, SelectModule,
    ButtonModule, ProgressSpinnerModule, DividerModule
  ],
  templateUrl: './profile-editor.html',
  styleUrl: './profile-editor.scss',
})
export class ProfileEditor {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private profiles = inject(ProfileService);

  uploading = signal(false);
  uploadError = signal<string | null>(null);
  profileReady = signal(false);
  saved = signal(false);
  saveError = signal<string | null>(null);

  readonly levelOptions = [
    { label: 'Intern', value: 'intern' },
    { label: 'Junior', value: 'junior' },
    { label: 'Mid', value: 'mid' },
    { label: 'Senior', value: 'senior' },
  ];

  form: FormGroup = this.fb.group({
    level: ['junior', Validators.required],
    description: ['', Validators.required],
    skills: this.fb.array([]),
  });

  constructor() {
    const saved = this.profiles.load();
    if (saved) this.fillForm(saved);

    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.saved.set(false));
  }

  get skills(): FormArray {
    return this.form.get('skills') as FormArray;
  }

  get skillControls() {
    return this.skills.controls;
  }

  addSkill(): void {
    this.skills.push(this.fb.control(''));
  }

  removeSkill(i: number): void {
    this.skills.removeAt(i);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    this.uploadCv(input.files[0]);
    // Lets the user pick the same file again after an error
    input.value = '';
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    const file = event.dataTransfer?.files[0];
    if (file) this.uploadCv(file);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  async uploadCv(file: File): Promise<void> {
    if (!file.name.toLowerCase().endsWith('.docx')) {
      this.uploadError.set('Only .docx format is supported');
      return;
    }

    this.uploading.set(true);
    this.uploadError.set(null);
    this.profileReady.set(false);

    try {
      const profile = await firstValueFrom(this.api.uploadCv(file));
      this.fillForm(profile);
      this.profileReady.set(true);
    } catch (err) {
      this.uploadError.set(toApiError(err, 'Failed to upload CV. Please try again.').message);
      console.error(err);
    } finally {
      this.uploading.set(false);
    }
  }

  saveProfile(): void {
    const value = this.form.value as CandidateProfile;
    const skills = value.skills.map(s => s.trim()).filter(s => s.length > 0);

    if (skills.length === 0) {
      this.saveError.set('Add at least one skill');
      return;
    }

    this.profiles.save({ ...value, skills });
    this.saveError.set(null);
    this.saved.set(true);
  }

  private fillForm(profile: CandidateProfile): void {
    this.form.patchValue({
      level: profile.level,
      description: profile.description,
    });

    this.skills.clear();
    profile.skills.forEach(s => this.skills.push(this.fb.control(s)));
  }
}

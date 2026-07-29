import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { DividerModule } from 'primeng/divider';
import { AuthService } from '../services/auth';
import { environment } from '../../environments/environment';

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
  private auth = inject(AuthService);

  uploading = signal(false);
  uploadError = signal<string | null>(null);
  profileReady = signal(false);

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
    const saved = localStorage.getItem(this.auth.getProfileStorageKey());
    if (saved) {
      const profile = JSON.parse(saved);
      this.form.patchValue({
        level: profile.level,
        description: profile.description,
      });
      this.skills.clear();
      (profile.skills as string[]).forEach(s =>
        this.skills.push(this.fb.control(s))
      );
    }
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
    if (!file.name.endsWith('.docx')) {
      this.uploadError.set('Only .docx format is supported');
      return;
    }

    this.uploading.set(true);
    this.uploadError.set(null);
    this.profileReady.set(false);

    const formData = new FormData();
    formData.append('file', file);

    const token = this.auth.getToken();

    try {
      const response = await fetch(`${environment.apiUrl}/api/cv/upload`, {
        method: 'POST',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        body: formData,
      });

      if (!response.ok) throw new Error(`HTTP ${response.status}`);

      const profile = await response.json();

      this.form.patchValue({
        level: profile.level,
        description: profile.description,
      });

      this.skills.clear();
      (profile.skills as string[]).forEach(s =>
        this.skills.push(this.fb.control(s))
      );
      this.form.updateValueAndValidity();

      this.profileReady.set(true);
    } catch (err) {
      this.uploadError.set('Failed to upload CV. Please try again.');
      console.error(err);
    } finally {
      this.uploading.set(false);
    }
  }

  saveToStorage(): void {
    localStorage.setItem(this.auth.getProfileStorageKey(), JSON.stringify(this.form.value));
    alert('Profile saved!');
  }
}

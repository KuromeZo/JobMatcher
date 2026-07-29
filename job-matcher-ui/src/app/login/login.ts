import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { TabsModule } from 'primeng/tabs';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-login',
  imports: [CardModule, InputTextModule, PasswordModule, ButtonModule, TabsModule, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private auth = inject(AuthService);
  private router = inject(Router);

  login = signal('');
  password = signal('');
  error = signal<string | null>(null);
  loading = signal(false);

  submit(isRegister: boolean): void {
    if (!this.login() || !this.password()) {
      this.error.set('Please fill in all fields');
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const action = isRegister
      ? this.auth.register(this.login(), this.password())
      : this.auth.login(this.login(), this.password());

    action.subscribe({
      next: () => this.router.navigate(['/']),
      error: (err) => {
        this.error.set(err.status === 401 ? 'Invalid login or password' : 'Something went wrong');
        this.loading.set(false);
      }
    });
  }
}

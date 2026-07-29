import { Routes } from '@angular/router';
import { Dashboard } from './dashboard/dashboard';
import { ProfileEditor } from './profile-editor/profile-editor';
import { authGuard } from './services/auth-guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./login/login').then(m => m.Login) },
  { path: '', component: Dashboard, canActivate: [authGuard] },
  { path: 'profile', component: ProfileEditor, canActivate: [authGuard] },
  { path: '**', redirectTo: '' },
];

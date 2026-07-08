import { Routes } from '@angular/router';
import { loginGuard } from '../core/guards/login-guard';
import { globalGuard } from '../core/guards/global-guard';

export const routes: Routes = [
  {
    path: 'login',
    pathMatch: 'full',
    canMatch: [loginGuard],
    loadComponent: () => import('../features/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard',
  },
  {
    path: '',
    canMatch: [globalGuard],
    loadComponent: () =>
      import('../layout/main-layout/main-layout-component').then((m) => m.MainLayoutComponent),
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('../features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'org-tree',
        loadComponent: () =>
          import('../features/org-tree/org-tree.component').then((x) => x.OrgTreeComponent),
      },
    ],
  },
  {
    path: '**',
    redirectTo: 'dashboard',
  },
];

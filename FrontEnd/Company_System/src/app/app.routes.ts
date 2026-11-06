import { Routes } from '@angular/router';
import { loginGuard } from './core/guards/login-guard';
import { globalGuard } from './core/guards/global-guard';
import { adminGuard } from './core/guards/admin-guard';

export const routes: Routes = [
  {
    path: 'login',
    pathMatch: 'full',
    canMatch: [loginGuard],
    loadComponent: () => import('./features/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard',
  },
  {
    // add main layout
    path: '',
    loadComponent: () =>
      import('./layout/main-layout/main-layout-component').then((m) => m.MainLayoutComponent),
    children: [
      // with globalGuard -------------
      {
        path: '',
        canMatch: [globalGuard],
        children: [
          // main paths
          {
            path: 'dashboard',
            loadComponent: () =>
              import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
          },
          {
            path: 'my-portal',
            loadComponent: () =>
              import('./features/my-portal/my-portal.component').then((m) => m.MyPortalComponent),
          },
          {
            path: 'org-tree',
            loadComponent: () =>
              import('./features/org-tree/org-tree.component').then((x) => x.OrgTreeComponent),
          },
        ],
      },
      // ------------------------------

      // with admin guard ------------------------
      {
        path: 'admin',
        data: { isAdmin: true },
        canMatch: [adminGuard],
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'employees' },
          {
            path: 'attendance',
            loadComponent: () =>
              import('./features/admin/attendance/attendance.component').then(
                (m) => m.AttendanceComponent,
              ),
          },
          {
            path: 'payments',
            loadComponent: () =>
              import('./features/admin/payments/payments.component').then(
                (m) => m.PaymentsComponent,
              ),
          },
          {
            path: 'employees',
            loadComponent: () =>
              import('./features/admin/employees/employees.component').then(
                (x) => x.EmployeesComponent,
              ),
          },
          {
            path: '**',
            redirectTo: 'employees',
          },
        ],
      },
      // ----------------------------------------
    ],
  },

  {
    path: '**',
    redirectTo: 'dashboard',
  },
];

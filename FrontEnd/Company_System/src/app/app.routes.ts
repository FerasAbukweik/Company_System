import { Routes } from '@angular/router';
import { loginGuard } from '../core/guards/login-guard';
import { globalGuard } from '../core/guards/global-guard';
import { adminGuard } from '../core/guards/admin-guard';

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
    // add main layout
    path: '',
    loadComponent: () =>
      import('../layout/main-layout/main-layout-component').then((m) => m.MainLayoutComponent),
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
              import('../features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
          },
          {
            path: 'org-tree',
            loadComponent: () =>
              import('../features/org-tree/org-tree.component').then((x) => x.OrgTreeComponent),
          },
        ],
      },
      // ------------------------------

      // with admin guard ------------------------
      {
        path: '',
        canMatch: [adminGuard],
        children: [
          {
            path: 'add-employee',
            loadComponent: () =>
              import('../features/add-employee/add-employee.component').then(
                (x) => x.AddEmployeeComponent,
              ),
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

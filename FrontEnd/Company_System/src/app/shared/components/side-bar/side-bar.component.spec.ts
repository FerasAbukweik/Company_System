import { of } from 'rxjs';
import { Component, signal, WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthService } from '../../../core/services/api-services/auth-service';
import { SideBarService } from '../../../core/services/api-services/side-bar-service';
import { SideBarComponent } from './side-bar.component';

@Component({ template: '' })
class TestPage {}

describe('Sidebar route context', () => {
  const admin = signal(true);

  beforeEach(() => {
    admin.set(true);
    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthService,
          useValue: { isAdmin: admin, updateIsAdmin: vi.fn(() => of(true)) },
        },
        { provide: SideBarService, useValue: { isSideBarOpen: signal(true), close: vi.fn() } },
        provideRouter([
          { path: 'dashboard', component: TestPage },
          {
            path: 'admin',
            data: { isAdmin: true },
            children: [{ path: 'employees', component: TestPage }],
          },
          { path: 'management', data: { isAdmin: true }, component: TestPage },
        ]),
      ],
    });
  });

  it('refreshes admin status once when the sidebar initializes', async () => {
    const auth = TestBed.inject(AuthService);
    const fixture = TestBed.createComponent(SideBarComponent);
    await fixture.whenStable();
    expect(auth.updateIsAdmin).toHaveBeenCalledOnce();
    await TestBed.inject(Router).navigateByUrl('/dashboard');
    await fixture.whenStable();
    expect(auth.updateIsAdmin).toHaveBeenCalledOnce();
  });

  it('keeps a closed sidebar hidden from its first render', async () => {
    const open = TestBed.inject(SideBarService).isSideBarOpen as WritableSignal<boolean>;
    open.set(false);
    const fixture = TestBed.createComponent(SideBarComponent);
    await fixture.whenStable();
    const sidebar: HTMLElement = fixture.nativeElement;
    expect(sidebar.style.visibility).toBe('hidden');
    expect(sidebar.hasAttribute('inert')).toBe(true);
    open.set(true);
    await fixture.whenStable();
    expect(sidebar.style.visibility).toBe('visible');
    expect(sidebar.hasAttribute('inert')).toBe(false);
  });

  it('shows regular pages and an admin-only entry linking to /admin', async () => {
    await TestBed.inject(Router).navigateByUrl('/dashboard');
    const fixture = TestBed.createComponent(SideBarComponent);
    await fixture.whenStable();
    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(links.map((link) => link.textContent?.trim())).toEqual([
      'Dashboard',
      'Org Tree',
      'My Portal',
      'Admin',
    ]);
    expect(links[3].getAttribute('href')).toBe('/admin');

    admin.set(false);
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).not.toContain('Admin');
  });

  it('replaces regular links inside admin routes and restores them after leaving', async () => {
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/dashboard');
    const fixture = TestBed.createComponent(SideBarComponent);
    await fixture.whenStable();

    await router.navigateByUrl('/admin/employees');
    await fixture.whenStable();
    const labels = () =>
      Array.from(fixture.nativeElement.querySelectorAll('a')).map((link) =>
        (link as HTMLElement).textContent?.trim(),
      );
    expect(labels()).toEqual(['Employees', 'Payments', 'Attendance', 'Employee Pages']);

    await router.navigateByUrl('/dashboard');
    await fixture.whenStable();
    expect(labels()).toEqual(['Dashboard', 'Org Tree', 'My Portal', 'Admin']);
  });

  it('uses route metadata on an initial load, rather than checking the URL prefix', async () => {
    await TestBed.inject(Router).navigateByUrl('/management');
    const fixture = TestBed.createComponent(SideBarComponent);
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Employees');
    expect(fixture.nativeElement.querySelector('a[href="/dashboard"]')?.textContent).toContain(
      'Employee Pages',
    );
  });
});

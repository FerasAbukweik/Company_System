import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { DelegateTaskComponent } from './delegate-task.component';
import { DelegateTaskService } from './delegate-task.service';
import { ToastService } from '../../../../core/services/api-services/toast-service';
import { PositionsEnum } from '../../../../core/enum/positions-enum';

async function openForm() {
  const fixture = TestBed.createComponent(DelegateTaskComponent);
  await fixture.whenStable();
  const drawer = TestBed.inject(DelegateTaskService);
  drawer.open({
    id: 'node',
    userId: 'employee',
    fullName: 'Employee',
    userName: 'employee',
    position: PositionsEnum.Employee,
    userImageUrl: '',
    children: [],
  });
  await fixture.whenStable();
  fixture.componentInstance.form.setValue({
    title: 'Review payroll',
    description: 'Review this month’s records',
    deadline: '2026-12-01',
  });
  const submit = () =>
    fixture.nativeElement
      .querySelector('form')
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
  return { fixture, drawer, submit };
}

describe('Delegate task submission', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        DelegateTaskService,
        { provide: ToastService, useValue: { error: vi.fn(), success: vi.fn() } },
      ],
    }),
  );
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('sends the form to the backend once and closes only after success', async () => {
    const { drawer, submit } = await openForm();
    const http = TestBed.inject(HttpTestingController);
    submit();
    submit();
    const request = http.expectOne(
      (request) => request.method === 'POST' && request.url.endsWith('/Tasks/Add'),
    );
    expect(request.request.body).toMatchObject({
      userId: 'employee',
      title: 'Review payroll',
      priority: 0,
    });
    expect(drawer.isShowDelegateTask()).toBe(true);
    expect(TestBed.inject(ToastService).success).not.toHaveBeenCalled();
    request.flush(true);
    expect(TestBed.inject(ToastService).success).toHaveBeenCalledWith('Task added successfully.');
    expect(drawer.isShowDelegateTask()).toBe(false);
  });

  it('keeps the form and employee selection after failure and allows retry', async () => {
    const { fixture, drawer, submit } = await openForm();
    const http = TestBed.inject(HttpTestingController);
    submit();
    http
      .expectOne((request) => request.method === 'POST')
      .flush({}, { status: 500, statusText: 'Failed' });
    expect(drawer.isShowDelegateTask()).toBe(true);
    expect(fixture.componentInstance.form.controls.title.value).toBe('Review payroll');
    expect(TestBed.inject(ToastService).error).toHaveBeenCalledOnce();
    expect(TestBed.inject(ToastService).success).not.toHaveBeenCalled();
    submit();
    http.expectOne((request) => request.method === 'POST').flush(true);
    expect(drawer.isShowDelegateTask()).toBe(false);
  });
  it('submits a deadline at the end of the selected local day, including today', async () => {
    const { fixture, submit } = await openForm();
    fixture.componentInstance.form.controls.deadline.setValue(fixture.componentInstance.todayStr);
    submit();
    const request = TestBed.inject(HttpTestingController).expectOne(
      (request) => request.method === 'POST',
    );
    const deadline = request.request.body.deadline as Date;
    expect(deadline.getTime()).toBeGreaterThan(Date.now());
    expect(deadline.getHours()).toBe(23);
    expect(deadline.getMinutes()).toBe(59);
    expect(deadline.getDate()).toBe(new Date().getDate());
    request.flush(true);
  });
});

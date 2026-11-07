import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { PaymentsComponent } from './payments.component';
import { AdminPayment } from '../../../core/dto/payments/admin-payment-dto';
import { PaymentState } from '../../../core/enum/payment-state';

const records: AdminPayment[] = [0, 1, 2, 3].map((index) => ({
  id: `payment-${index}`,
  userId: `user-${index}`,
  employeeName: `Employee ${index}`,
  imageUrl: null,
  year: new Date().getFullYear(),
  month: 10,
  paidAmount: 100,
  paymentDate: index === 2 ? `${new Date().getFullYear()}-11-05` : null,
  state:
    index < 2 ? PaymentState.Pending : index === 2 ? PaymentState.Approved : PaymentState.Rejected,
  account: null,
}));

const summary = {
  totalCount: 4,
  pendingCount: 2,
  approvedCount: 1,
  rejectedCount: 1,
  pendingAmount: 200,
  approvedAmount: 100,
  rejectedAmount: 100,
};
const response = (items: AdminPayment[], totalCount = items.length) => ({
  items,
  totalCount,
});

describe('Payments backend integration', () => {
  beforeAll(() =>
    Object.defineProperties(HTMLDialogElement.prototype, {
      showModal: {
        configurable: true,
        value: function (this: HTMLDialogElement) {
          this.open = true;
        },
      },
      close: {
        configurable: true,
        value: function (this: HTMLDialogElement) {
          this.open = false;
        },
      },
    }),
  );

  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }),
  );
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  const setup = async () => {
    const fixture = TestBed.createComponent(PaymentsComponent);
    await fixture.whenStable();
    const http = TestBed.inject(HttpTestingController);
    const aggregate = http.expectOne((request) => request.url.endsWith('/Payments/GetSummary'));
    expect(aggregate.request.params.keys()).toEqual([]);
    aggregate.flush(summary);
    http
      .expectOne((request) => request.url.endsWith('/Payments/GetPayments'))
      .flush(response(records.filter((payment) => payment.state === PaymentState.Pending)));
    await fixture.whenStable();
    return { fixture, http, element: fixture.nativeElement as HTMLElement };
  };

  const button = (element: ParentNode, label: string) =>
    Array.from(element.querySelectorAll('button')).find((button) =>
      button.textContent?.trim().startsWith(label),
    )!;

  it('requests only the emitted search string after the debounce delay', async () => {
    const { fixture, http, element } = await setup();
    const input = element.querySelector('[aria-label="Search employees"]') as HTMLInputElement;
    input.value = 'Em';
    input.dispatchEvent(new Event('input'));
    input.value = 'Employee 1';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    http.expectNone((request) => request.url.endsWith('/Payments/GetPayments'));
    await new Promise((resolve) => setTimeout(resolve, 350));
    await fixture.whenStable();
    const filtered = http.expectOne((request) => request.url.endsWith('/Payments/GetPayments'));
    expect(filtered.request.params.get('fullName')).toBe('Employee 1');
    filtered.flush(response([]));
    await fixture.whenStable();
  });

  it('shows real pending records separately from history', async () => {
    const { fixture, element } = await setup();
    expect(element.querySelectorAll('tbody tr')).toHaveLength(2);
    expect(element.querySelector('tbody')?.textContent).not.toContain('Employee 2');
    button(element, 'Payment History').click();
    await fixture.whenStable();
    TestBed.inject(HttpTestingController)
      .expectOne((request) => request.url.endsWith('/Payments/GetPayments'))
      .flush(response(records.filter((payment) => payment.state !== PaymentState.Pending)));
    await fixture.whenStable();
    expect(element.querySelector('tbody')?.textContent).toContain('Employee 2');
    expect(element.querySelector('tbody')?.textContent).not.toContain('Employee 0');
  });

  it('waits for a successful backend decision before moving a record', async () => {
    const { fixture, http, element } = await setup();
    button(element.querySelector('tbody')!, 'Approve').click();
    await fixture.whenStable();
    button(element.querySelector('dialog')!, 'Confirm Approve').click();
    await fixture.whenStable();
    expect(element.querySelectorAll('tbody tr')).toHaveLength(2);
    const request = http.expectOne((request) => request.url.endsWith('/Payments/UpdateStates'));
    expect(request.request.body).toEqual(['payment-0']);
    expect(request.request.params.get('state')).toBe('1');
    request.flush([{ ...records[0], state: PaymentState.Approved }]);
    http.expectOne((request) => request.url.endsWith('/Payments/GetSummary')).flush(summary);
    http
      .expectOne((request) => request.url.endsWith('/Payments/GetPayments'))
      .flush(response([records[1]]));
    await fixture.whenStable();
    expect(element.querySelectorAll('tbody tr')).toHaveLength(1);
  });

  it('keeps the confirmation open and shows failed decisions without changing data', async () => {
    const { fixture, http, element } = await setup();
    button(element.querySelector('tbody')!, 'Reject').click();
    await fixture.whenStable();
    button(element.querySelector('dialog')!, 'Confirm Reject').click();
    http
      .expectOne((request) => request.url.endsWith('/Payments/UpdateStates'))
      .flush(
        { message: 'Payment changed; refresh and retry' },
        { status: 409, statusText: 'Conflict' },
      );
    await fixture.whenStable();
    expect(element.querySelector('dialog [role="alert"]')?.textContent).toContain(
      'Payment changed',
    );
    expect(element.querySelectorAll('tbody tr')).toHaveLength(2);
  });
});

import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { EmployeesAdminService } from './employees.service';
import { PaymentsService } from '../payments/payments.service';
import { MyPortalService } from '../../my-portal/my-portal.service';

const cases = [
  {
    name: 'employees',
    create: () => {
      const service = TestBed.inject(EmployeesAdminService);
      return {
        pages: service.employeePages,
        load: (page: number) => service.load({ taken: (page - 1) * 5, sectionSize: 5 }),
      };
    },
  },
  {
    name: 'admin payments',
    create: () => {
      const service = TestBed.inject(PaymentsService);
      return {
        pages: service.paymentPages,
        load: (page: number) => service.load({ year: 2026, taken: (page - 1) * 5, sectionSize: 5 }),
      };
    },
  },
  {
    name: 'portal payments',
    create: () => {
      const service = TestBed.inject(MyPortalService);
      return {
        pages: service.portalPaymentPages,
        load: (page: number) => service.loadPayments(2026, page, 5),
      };
    },
  },
];

describe.each(cases)('$name page signal cache', ({ create }) => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        EmployeesAdminService,
        PaymentsService,
        MyPortalService,
      ],
    }),
  );
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('stores pages only after responses and returns to a cached page without changing the signal', () => {
    const { pages, load } = create();
    const http = TestBed.inject(HttpTestingController);
    http
      .match(
        (request) =>
          request.url.endsWith('/GetEmployeesSummary') ||
          request.url.endsWith('/Payments/GetSummary'),
      )
      .forEach((request) => request.flush({}));
    const first = [{ id: 'first' }];
    const second = [{ id: 'second' }];
    load(1);
    expect(pages()[1]).toBeUndefined();
    http.expectOne(() => true).flush({ items: first, totalCount: 10 });
    expect(pages()[1]).toEqual(first);

    load(2);
    expect(pages()[2]).toBeUndefined();
    expect(pages()[1]).toEqual(first);
    http.expectOne(() => true).flush({ items: second, totalCount: 10 });
    const cachedPages = pages();
    load(1);
    http.expectNone(() => true);
    expect(pages()).toBe(cachedPages);
    expect(pages()[1]).toEqual(first);
    expect(pages()[2]).toEqual(second);
  });
});

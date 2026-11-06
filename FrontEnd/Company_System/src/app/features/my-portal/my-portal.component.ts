import {
  Component,
  DestroyRef,
  effect,
  inject,
  signal,
  linkedSignal,
  untracked,
} from '@angular/core';
import { MyPortalService } from './my-portal.service';
import { AdminPayment } from '../../core/dto/payments/admin-payment-dto';
import { PortalSummaryComponent } from './components/portal-summary/portal-summary.component';
import { PayslipLedgerComponent } from './components/payslip-ledger/payslip-ledger.component';
import { PayslipDrawerComponent } from './components/payslip-drawer/payslip-drawer.component';
import { EmployeePaginationComponent } from '../admin/employees/components/employee-pagination/employee-pagination.component';
import { AttendanceCalendarComponent } from '../../shared/components/attendance-calendar/attendance-calendar.component';

@Component({
  selector: 'app-my-portal',
  providers: [MyPortalService],
  imports: [
    PortalSummaryComponent,
    PayslipLedgerComponent,
    PayslipDrawerComponent,
    AttendanceCalendarComponent,
    EmployeePaginationComponent,
  ],
  templateUrl: './my-portal.component.html',
  host: { class: 'block p-4 text-text-primary' },
})
export class MyPortalComponent {
  protected readonly portal = inject(MyPortalService);
  protected readonly year = signal(new Date().getFullYear());
  protected readonly years = Array.from(
    { length: new Date().getFullYear() - 2023 + 1 },
    (_, i) => 2023 + i,
  );
  protected readonly calendarMonth = signal(
    new Date(new Date().getFullYear(), new Date().getMonth(), 1),
  );
  protected readonly pageSize = signal(5);
  protected readonly page = linkedSignal(() => {
    this.year();
    this.pageSize();
    return 1;
  });
  protected readonly selectedPayslip = signal<AdminPayment | null>(null);

  constructor() {
    effect(() => {
      const year = this.year();
      untracked(() => this.portal.loadDetails(year));
    });
    effect(() => {
      const year = this.year(),
        page = this.page(),
        size = this.pageSize();
      this.selectedPayslip.set(null);
      untracked(() => this.portal.loadPayments(year, page, size));
    });
    effect(() => {
      if (this.portal.paymentsLoading() || this.portal.paymentsError()) return;
      const last = Math.max(
        1,
        Math.ceil(this.portal.paymentSummary().totalCount / this.pageSize()),
      );
      if (this.page() > last) this.page.set(last);
    });
  }

  protected changeYear(value: string) {
    const year = Number(value);
    this.calendarMonth.update((month) => new Date(year, month.getMonth(), 1));
    this.year.set(year);
  }

  protected changeMonth(month: Date) {
    if (month.getFullYear() < 2023 || month.getFullYear() > new Date().getFullYear()) return;
    this.calendarMonth.set(month);
    this.year.set(month.getFullYear());
  }

  protected printPayslip() {
    window.print();
  }
}

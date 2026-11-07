import {
  Component,
  computed,
  effect,
  inject,
  signal,
  linkedSignal,
  untracked,
} from '@angular/core';
import { SelectedAttendanceDayComponent } from './components/selected-attendance-day/selected-attendance-day.component';
import { PaymentState } from '../../core/enum/payment-state';
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
    SelectedAttendanceDayComponent,
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
  protected readonly State = PaymentState;
  protected readonly paymentState = signal<PaymentState | null>(null);
  protected readonly year = signal(new Date().getFullYear());
  protected readonly years = Array.from(
    { length: new Date().getFullYear() - 2023 + 1 },
    (_, i) => 2023 + i,
  );
  protected readonly calendarMonth = signal(
    new Date(new Date().getFullYear(), new Date().getMonth(), 1),
  );
  protected readonly selectedDate = linkedSignal(() => {
    const month = this.calendarMonth();
    const today = new Date();
    const day =
      month.getFullYear() === today.getFullYear() && month.getMonth() === today.getMonth()
        ? today.getDate()
        : 1;
    return `${month.getFullYear()}-${String(month.getMonth() + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
  });
  protected readonly selectedAttendance = computed(
    () => this.portal.attendance().find((record) => record.date === this.selectedDate()) ?? null,
  );
  protected readonly pageSize = signal(5);
  protected readonly page = linkedSignal(() => {
    this.year();
    this.paymentState();
    this.pageSize();
    return 1;
  });
  protected readonly selectedPayslip = signal<AdminPayment | null>(null);

  constructor() {
    this.portal.loadOverview();
    effect(() => {
      const month = this.calendarMonth();
      untracked(() => this.portal.loadDetails(month));
    });
    effect(() => {
      const year = this.year(),
        page = this.page(),
        size = this.pageSize(),
        state = this.paymentState();
      this.selectedPayslip.set(null);
      untracked(() => this.portal.loadPayments(year, page, size, state));
    });
    effect(() => {
      if (this.portal.paymentsLoading() || this.portal.paymentsError()) return;
      const last = Math.max(1, Math.ceil(this.portal.paymentTotalCount() / this.pageSize()));
      if (this.page() > last) this.page.set(last);
    });
  }

  protected changePaymentState(value: string) {
    this.paymentState.set(value === '' ? null : (Number(value) as PaymentState));
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

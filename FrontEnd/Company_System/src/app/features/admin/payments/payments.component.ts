import { apiError } from '../../../core/utils/api-error';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  untracked,
  DestroyRef,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { ToastService } from '../../../core/services/api-services/toast-service';
import { PaymentToolbarComponent } from './components/payment-toolbar/payment-toolbar.component';
import { PaymentMetricsComponent } from './components/payment-metrics/payment-metrics.component';
import { PaymentTableComponent } from './components/payment-table/payment-table.component';
import { PaymentPaginationComponent } from './components/payment-pagination/payment-pagination.component';
import { PaymentDecisionComponent } from './components/payment-decision/payment-decision.component';
import { PaymentReceiptComponent } from './components/payment-receipt/payment-receipt.component';
import { AdminPayment } from '../../../core/dto/payments/admin-payment-dto';
import { PaymentState } from '../../../core/enum/payment-state';
import { HistoryFilter, PaymentDecision, PaymentView } from './payments.model';
import { PaymentsService } from './payments.service';
import { paymentsCsv } from './payments.utils';

@Component({
  selector: 'app-payments',
  imports: [
    PaymentToolbarComponent,
    PaymentMetricsComponent,
    PaymentTableComponent,
    PaymentPaginationComponent,
    PaymentDecisionComponent,
    PaymentReceiptComponent,
  ],
  providers: [PaymentsService],
  templateUrl: './payments.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block p-4 sm:p-6 lg:p-8 text-text-primary' },
})
export class PaymentsComponent {
  protected readonly paymentsService = inject(PaymentsService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly saving = signal(false);
  protected readonly decisionError = signal('');

  protected readonly search = signal('');
  protected readonly month = signal(0);
  protected readonly year = signal(new Date().getFullYear());
  protected readonly view = signal<PaymentView>('pending');
  protected readonly historyFilter = signal<HistoryFilter>('all');
  protected readonly pageSize = 5;

  protected readonly decision = signal<PaymentDecision | null>(null);
  protected readonly receipt = signal<AdminPayment | null>(null);
  protected readonly State = PaymentState;
  protected readonly views: { key: PaymentView; label: string }[] = [
    { key: 'pending', label: 'Pending Payments' },
    { key: 'history', label: 'Payment History' },
    { key: 'all', label: 'All Payments' },
  ];
  protected readonly historyFilters: { key: HistoryFilter; label: string }[] = [
    { key: 'all', label: 'All History' },
    { key: 'approved', label: 'Approved' },
    { key: 'rejected', label: 'Rejected' },
  ];

  protected readonly counts = computed(() => ({
    pending: this.paymentsService.summary()?.pendingCount ?? 0,
    history:
      (this.paymentsService.summary()?.approvedCount ?? 0) +
      (this.paymentsService.summary()?.rejectedCount ?? 0),
    all: this.paymentsService.summary()?.totalCount ?? 0,
  }));

  // Each view remembers its page; changing search or period starts a new result set.
  private readonly pagesByView = linkedSignal(() => {
    this.search();
    this.month();
    this.year();
    this.historyFilter();

    return { pending: 1, history: 1, all: 1 };
  });

  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.paymentsService.totalCount() / this.pageSize)),
  );
  protected readonly page = computed(() => this.pagesByView()[this.view()]);

  constructor() {
    effect(() => {
      const state =
        this.view() === 'pending'
          ? PaymentState.Pending
          : this.view() === 'history' && this.historyFilter() !== 'all'
            ? this.historyFilter() === 'approved'
              ? PaymentState.Approved
              : PaymentState.Rejected
            : null;
      const query = {
        year: this.year(),
        month: this.month() || undefined,
        state,
        historyOnly: this.view() === 'history',
        fullName: this.search(),
        taken: (this.page() - 1) * this.pageSize,
        sectionSize: this.pageSize,
      };
      untracked(() => this.paymentsService.load(query));
    });
    effect(() => {
      if (this.paymentsService.loading() || this.paymentsService.error()) return;
      if (this.page() > this.totalPages()) this.changePage(this.totalPages());
    });
  }

  protected readonly selected = linkedSignal(() => {
    this.search();
    this.month();
    this.year();
    this.view();

    return new Set<string>();
  });

  protected readonly selectedPayments = computed(() =>
    Object.values(this.paymentsService.paymentPages())
      .flat()
      .filter(
        (payment) => payment.state === PaymentState.Pending && this.selected().has(payment.id),
      ),
  );

  protected changePage(page: number) {
    this.pagesByView.update((pages) => ({
      ...pages,
      [this.view()]: Math.max(1, Math.min(page, this.totalPages())),
    }));
  }

  protected toggleSelected(id: string) {
    this.selected.update((current) => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  protected selectPage(checked: boolean) {
    this.selected.update((current) => {
      const next = new Set(current);
      for (const payment of this.paymentsService.paymentPages()[this.page()] ?? []) {
        if (payment.state !== PaymentState.Pending) continue;
        if (checked) next.add(payment.id);
        else next.delete(payment.id);
      }
      return next;
    });
  }

  protected openDecision(decision: PaymentDecision) {
    this.decisionError.set('');
    this.decision.set(decision);
  }

  protected approveSelected() {
    if (!this.selectedPayments().length) return;
    this.openDecision({ payments: this.selectedPayments(), state: PaymentState.Approved });
  }

  protected confirmDecision(decision: PaymentDecision) {
    if (this.saving()) return;
    this.saving.set(true);
    this.decisionError.set('');
    this.paymentsService
      .applyDecision(decision)
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.selected.set(new Set());
          this.decision.set(null);
          this.toast.success('Payment decisions saved.');
        },
        error: (error) => this.decisionError.set(apiError(error)),
      });
  }

  protected exportReport() {
    const file = new Blob([paymentsCsv(this.paymentsService.paymentPages()[this.page()] ?? [])], {
      type: 'text/csv;charset=utf-8;',
    });
    const url = URL.createObjectURL(file);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `payments-${this.year()}-${this.view()}-page-${this.page()}.csv`;
    anchor.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
}

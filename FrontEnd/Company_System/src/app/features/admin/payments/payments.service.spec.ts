import { PaymentState } from '../../../core/enum/payment-state';
import { paymentsCsv } from './payments.utils';

describe('Payment CSV export', () => {
  it('quotes CSV fields and blocks spreadsheet formulas', () => {
    const csv = paymentsCsv([
      {
        id: 'id',
        userId: 'user',
        employeeName: '=HYPERLINK("example")',
        year: 2026,
        month: 1,
        paidAmount: 100,
        paymentDate: '2026-01-05',
        state: PaymentState.Pending,
        imageUrl: null,
        account: null,
      },
    ]);
    expect(csv).toContain('"\'=HYPERLINK(""example"")"');
  });
});

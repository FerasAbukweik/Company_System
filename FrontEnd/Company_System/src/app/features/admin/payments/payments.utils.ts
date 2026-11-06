import { AdminPayment } from '../../../core/dto/payments/admin-payment-dto';
import { PaymentState } from '../../../core/enum/payment-state';

/** Escape CSV fields and prevent employee-supplied text being interpreted as a formula. */
export function paymentsCsv(payments: readonly AdminPayment[]): string {
  const field = (value: string | number) => {
    const text = String(value);
    const safe = /^[\s]*[=+@-]/.test(text) ? `'${text}` : text;
    return `"${safe.replaceAll('"', '""')}"`;
  };
  const header = [
    'Payment ID',
    'Employee ID',
    'Employee',
    'Payroll Year',
    'Payroll Month',
    'Payment Date',
    'Paid Amount (USD)',
    'State',
  ];
  const rows = payments.map((payment) => [
    payment.id,
    payment.userId,
    payment.employeeName,
    payment.year,
    payment.month,
    payment.paymentDate ?? '',
    payment.paidAmount.toFixed(2),
    PaymentState[payment.state],
  ]);

  return [header, ...rows].map((row) => row.map(field).join(',')).join('\r\n');
}

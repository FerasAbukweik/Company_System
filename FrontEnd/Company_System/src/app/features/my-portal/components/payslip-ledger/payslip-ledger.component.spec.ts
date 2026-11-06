import { TestBed } from '@angular/core/testing';
import { PayslipLedgerComponent } from './payslip-ledger.component';

describe('PayslipLedgerComponent', () => {
  it('shows an empty state instead of sample records', async () => {
    const fixture = TestBed.createComponent(PayslipLedgerComponent);
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('tbody').textContent).toContain(
      'No payments recorded',
    );
  });

  it('emits the supplied backend payment when viewed', async () => {
    const fixture = TestBed.createComponent(PayslipLedgerComponent);
    const record = {
      id: 'payment',
      userId: 'user',
      year: 2026,
      month: 10,
      paidAmount: 100,
      paymentDate: '2026-11-05',
      state: 1,
    };
    fixture.componentRef.setInput('records', [record]);
    const viewed = vi.fn();
    fixture.componentInstance.viewed.subscribe(viewed);
    await fixture.whenStable();
    fixture.nativeElement.querySelector('tbody button').click();
    expect(viewed).toHaveBeenCalledWith(record);
  });
});

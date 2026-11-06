import { Component, inject } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApprovalService } from '../../../../core/services/api-services/approval-service';
import { ToastService } from '../../../../core/services/api-services/toast-service';
import { NeedsApprovalCardComponent } from './needs-approval-card.component';

@Component({
  imports: [NeedsApprovalCardComponent],
  template: `@for (approval of approvals.toApprove(); track approval.id) {
    <app-needs-approval-card [approval]="approval" />
  }`,
})
class ApprovalListHost {
  readonly approvals = inject(ApprovalService);

  refresh() {
    this.approvals.resetToApprova();
    this.approvals.loadMoreToApprove().subscribe();
  }
}

const approval = {
  id: 'approval',
  requesterName: 'Employee',
  body: 'Holiday',
  createdOn: '2026-11-05T10:00:00Z',
};
const isList = (request: { url: string }) => request.url.endsWith('/GetNeedsApproval');

describe('Approval card request ownership', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ToastService, useValue: { success: vi.fn(), error: vi.fn() } },
      ],
    }),
  );
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  const setup = async () => {
    const fixture = TestBed.createComponent(ApprovalListHost);
    const http = TestBed.inject(HttpTestingController);
    fixture.componentInstance.refresh();
    http.expectOne(isList).flush([approval]);
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelectorAll('app-needs-approval-card')).toHaveLength(1);
    return { fixture, http };
  };

  it('keeps the replacement refresh request alive when old cards are destroyed', async () => {
    const { fixture, http } = await setup();
    fixture.componentInstance.refresh();
    const refresh = http.expectOne(isList);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelectorAll('app-needs-approval-card')).toHaveLength(0);
    expect(refresh.cancelled).toBe(false);
    refresh.flush([{ ...approval, id: 'replacement' }]);
    await fixture.whenStable();
    expect(fixture.componentInstance.approvals.toApprove()[0].id).toBe('replacement');
    expect(fixture.componentInstance.approvals.isLoadingToApprove()).toBe(false);
  });

  it('keeps a decision request alive when optimistic removal destroys its card', async () => {
    const { fixture, http } = await setup();
    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    buttons[1].click();
    const decision = http.expectOne((request) => request.method === 'PUT');
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelectorAll('app-needs-approval-card')).toHaveLength(0);
    expect(decision.cancelled).toBe(false);
    decision.flush({});
    expect(TestBed.inject(ToastService).success).toHaveBeenCalledWith(
      'updated approval successfully',
    );
    expect(fixture.componentInstance.approvals.isLoadingToApprove()).toBe(false);
  });

  it('still cancels an obsolete read on explicit refresh without cancelling its replacement', async () => {
    const { fixture, http } = await setup();
    fixture.componentInstance.approvals.loadMoreToApprove().subscribe();
    const obsolete = http.expectOne(isList);
    fixture.componentInstance.refresh();
    const replacement = http.expectOne(isList);
    await fixture.whenStable();

    expect(obsolete.cancelled).toBe(true);
    expect(replacement.cancelled).toBe(false);
    replacement.flush([]);
    expect(fixture.componentInstance.approvals.isLoadingToApprove()).toBe(false);
  });
});

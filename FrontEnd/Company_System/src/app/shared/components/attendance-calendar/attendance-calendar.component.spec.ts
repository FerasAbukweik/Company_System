import { TestBed } from '@angular/core/testing';
import { AttendanceCalendarComponent } from './attendance-calendar.component';

describe('AttendanceCalendarComponent', () => {
  it('selects and emits the local date today on initialization only once', async () => {
    const fixture = TestBed.createComponent(AttendanceCalendarComponent);
    const selected = vi.fn();
    fixture.componentInstance.dateSelected.subscribe(selected);
    const today = new Date();
    const date = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;

    await fixture.whenStable();
    expect(fixture.componentInstance.selectedDate()).toBe(date);
    expect(selected).toHaveBeenCalledExactlyOnceWith(date);
    expect(
      fixture.nativeElement.querySelector('[aria-pressed="true"]').getAttribute('aria-label'),
    ).toContain(date);
    fixture.detectChanges();
    expect(selected).toHaveBeenCalledTimes(1);
  });

  it('marks only stored days and displays their actual state and worked hours', async () => {
    const fixture = TestBed.createComponent(AttendanceCalendarComponent);
    fixture.componentRef.setInput('month', new Date(2026, 10, 1));
    fixture.componentRef.setInput('records', [
      { id: 'day', userId: 'user', date: '2026-11-05', minutesWorked: 450, state: 0 },
    ]);
    await fixture.whenStable();
    expect(
      fixture.nativeElement.querySelector('[title="Worked · 7.5 hours"]')?.textContent.trim(),
    ).toBe('5');
    expect(fixture.nativeElement.querySelectorAll('[title="No attendance data"]')).toHaveLength(29);
  });

  it('emits navigation across years without inventing records', async () => {
    const fixture = TestBed.createComponent(AttendanceCalendarComponent);
    fixture.componentRef.setInput('month', new Date(2026, 11, 1));
    const changed = vi.fn();
    fixture.componentInstance.monthChanged.subscribe(changed);
    await fixture.whenStable();
    (fixture.nativeElement.querySelector('[aria-label="Next month"]') as HTMLButtonElement).click();
    expect(changed).toHaveBeenCalledWith(new Date(2027, 0, 1));
  });
  it('emits the local date for a selected day and disables days outside the month', async () => {
    const fixture = TestBed.createComponent(AttendanceCalendarComponent);
    fixture.componentRef.setInput('month', new Date(2026, 10, 1));
    fixture.componentRef.setInput('selectedDate', '2026-11-05');
    const selected = vi.fn();
    fixture.componentInstance.dateSelected.subscribe(selected);
    await fixture.whenStable();

    const day = fixture.nativeElement.querySelector(
      '[aria-label^="2026-11-05 ·"]',
    ) as HTMLButtonElement;
    day.click();
    expect(selected).toHaveBeenCalledWith('2026-11-05');
    expect(day.getAttribute('aria-pressed')).toBe('true');
    const outside = fixture.nativeElement.querySelector(
      '[title="Outside current month"]',
    ) as HTMLButtonElement;
    expect(outside.disabled).toBe(true);
    outside.click();
    expect(selected).toHaveBeenCalledTimes(1);

    fixture.componentRef.setInput('disabled', true);
    await fixture.whenStable();
    day.click();
    expect(selected).toHaveBeenCalledTimes(1);
  });
});
